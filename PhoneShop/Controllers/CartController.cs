using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PhoneShop.Configuration;
using PhoneShop.DB;
using PhoneShop.Models;
using PhoneShop.Services;

namespace PhoneShop.Controllers
{
    public class CartController : Controller
    {
        private readonly PhoneShopDbContext _context;
        private readonly IPayPalService _payPalService;
        private readonly PayPalOptions _payPalOptions;
        private readonly IDataProtector _checkoutProtector;
        private readonly ILogger<CartController> _logger;
        private const string CART_COOKIE_KEY = "PhoneShopCart";

        public CartController(
            PhoneShopDbContext context,
            IPayPalService payPalService,
            IOptions<PayPalOptions> payPalOptions,
            IDataProtectionProvider dataProtectionProvider,
            ILogger<CartController> logger)
        {
            _context = context;
            _payPalService = payPalService;
            _payPalOptions = payPalOptions.Value;
            _checkoutProtector = dataProtectionProvider.CreateProtector(
                "PhoneShop.PayPalCheckoutDraft.v1");
            _logger = logger;
        }

        private List<CartItem> GetCartFromCookie()
        {
            var cookie = Request.Cookies[CART_COOKIE_KEY];
            if (string.IsNullOrEmpty(cookie))
            {
                return new List<CartItem>();
            }

            try
            {
                return JsonSerializer.Deserialize<List<CartItem>>(cookie) ?? new List<CartItem>();
            }
            catch
            {
                return new List<CartItem>();
            }
        }

        private void SaveCartToCookie(List<CartItem> cart)
        {
            var options = new CookieOptions
            {
                Expires = DateTime.Now.AddDays(7),
                HttpOnly = true,
                Path = "/"
            };
            var json = JsonSerializer.Serialize(cart);
            Response.Cookies.Append(CART_COOKIE_KEY, json, options);
        }

        public IActionResult Index()
        {
            var cart = GetCartFromCookie();
            return View(cart);
        }

        public async Task<IActionResult> AddToCart(
            int id,
            int quantity = 1,
            string? returnUrl = null)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            var cart = GetCartFromCookie();
            var item = cart.FirstOrDefault(i => i.ProductId == id);
            if (item != null)
            {
                item.Quantity += quantity;
            }
            else
            {
                cart.Add(new CartItem
                {
                    ProductId = product.Id,
                    Name = product.Name ?? "",
                    Photo = product.Photo ?? "",
                    Price = product.PriceSale ?? product.Price ?? 0,
                    Quantity = quantity
                });
            }

            SaveCartToCookie(cart);
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult UpdateQuantity(int id, int quantity)
        {
            var cart = GetCartFromCookie();
            var item = cart.FirstOrDefault(i => i.ProductId == id);
            if (item != null)
            {
                item.Quantity = quantity;
                if (item.Quantity <= 0)
                {
                    cart.Remove(item);
                }
            }
            SaveCartToCookie(cart);

            var itemSubtotal = item != null ? item.SubTotal : 0;
            var subtotal = cart.Sum(i => i.SubTotal);

            return Json(new { success = true, itemSubtotal = itemSubtotal, subtotal = subtotal });
        }

        public IActionResult Remove(int id)
        {
            var cart = GetCartFromCookie();
            var item = cart.FirstOrDefault(i => i.ProductId == id);
            if (item != null)
            {
                cart.Remove(item);
            }
            SaveCartToCookie(cart);
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Checkout()
        {
            var cart = await GetVerifiedCartAsync();
            if (cart.Count == 0)
            {
                return RedirectToAction("Index");
            }

            return View(new CheckoutViewModel
            {
                CartItems = cart,
                PayPalClientId = _payPalOptions.ClientId,
                Currency = _payPalOptions.Currency.ToUpperInvariant(),
                IsPayPalConfigured = _payPalOptions.IsConfigured
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePayPalOrder(
            [FromForm] CheckoutCustomerInput customer,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Please complete all required billing details." });
            }

            if (!_payPalOptions.IsConfigured)
            {
                return StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    new { message = "PayPal has not been configured for this store." });
            }

            var cart = await GetVerifiedCartAsync();
            if (cart.Count == 0)
            {
                return BadRequest(new { message = "Your cart is empty." });
            }

            try
            {
                var total = cart.Sum(item => item.SubTotal);
                var currency = _payPalOptions.Currency.ToUpperInvariant();
                var orderId = await _payPalService.CreateOrderAsync(
                    total,
                    currency,
                    $"PhoneShop order ({cart.Sum(item => item.Quantity)} items)",
                    cancellationToken);
                var draft = new PayPalCheckoutDraft(
                    orderId,
                    customer.FirstName.Trim(),
                    customer.LastName.Trim(),
                    customer.Address.Trim(),
                    customer.Email.Trim(),
                    customer.Phone.Trim(),
                    customer.Message?.Trim(),
                    total,
                    currency,
                    DateTimeOffset.UtcNow);
                var checkoutToken = _checkoutProtector.Protect(
                    JsonSerializer.Serialize(draft));

                return Json(new { id = orderId, checkoutToken });
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Could not create PayPal order.");
                return StatusCode(
                    StatusCodes.Status502BadGateway,
                    new { message = "PayPal could not start the payment. Please try again." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CapturePayPalOrder(
            [FromForm] string payPalOrderId,
            [FromForm] string checkoutToken,
            CancellationToken cancellationToken)
        {
            PayPalCheckoutDraft draft;
            try
            {
                var json = _checkoutProtector.Unprotect(checkoutToken);
                draft = JsonSerializer.Deserialize<PayPalCheckoutDraft>(json)
                    ?? throw new InvalidOperationException("Checkout data is missing.");
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Invalid PayPal checkout token.");
                return BadRequest(new { message = "This checkout session is invalid." });
            }

            if (!string.Equals(
                    draft.PayPalOrderId,
                    payPalOrderId,
                    StringComparison.Ordinal) ||
                DateTimeOffset.UtcNow - draft.CreatedAt > TimeSpan.FromMinutes(30))
            {
                return BadRequest(new { message = "This checkout session has expired." });
            }

            try
            {
                var capture = await _payPalService.CaptureOrderAsync(
                    payPalOrderId,
                    cancellationToken);
                var paymentMatches = string.Equals(
                                         capture.Status,
                                         "COMPLETED",
                                         StringComparison.OrdinalIgnoreCase) &&
                                     string.Equals(
                                         capture.Currency,
                                         draft.Currency,
                                         StringComparison.OrdinalIgnoreCase) &&
                                     capture.Amount == draft.Total;

                if (!paymentMatches)
                {
                    _logger.LogError(
                        "PayPal capture mismatch for order {OrderId}. " +
                        "Status: {Status}, Amount: {Amount} {Currency}, expected: {ExpectedAmount} {ExpectedCurrency}",
                        payPalOrderId,
                        capture.Status,
                        capture.Amount,
                        capture.Currency,
                        draft.Total,
                        draft.Currency);
                    return StatusCode(
                        StatusCodes.Status502BadGateway,
                        new { message = "PayPal did not confirm the expected payment amount." });
                }

                Response.Cookies.Delete(CART_COOKIE_KEY);
                TempData["OrderName"] = $"{draft.FirstName} {draft.LastName}";
                TempData["OrderPhone"] = draft.Phone;
                TempData["OrderAddress"] = draft.Address;
                TempData["OrderEmail"] = draft.Email;
                TempData["OrderTotal"] = draft.Total.ToString(
                    CultureInfo.InvariantCulture);
                TempData["OrderCurrency"] = draft.Currency;
                TempData["PayPalOrderId"] = payPalOrderId;

                return Json(new { successUrl = Url.Action(nameof(Success)) });
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Could not capture PayPal order {OrderId}.",
                    payPalOrderId);
                return StatusCode(
                    StatusCodes.Status502BadGateway,
                    new { message = "PayPal could not complete the payment. Please try again." });
            }
        }

        public IActionResult Success()
        {
            if (TempData["OrderTotal"] is not string totalText ||
                !decimal.TryParse(
                    totalText,
                    CultureInfo.InvariantCulture,
                    out var total))
            {
                return RedirectToAction("Index");
            }

            ViewBag.OrderName = TempData["OrderName"];
            ViewBag.OrderPhone = TempData["OrderPhone"];
            ViewBag.OrderAddress = TempData["OrderAddress"];
            ViewBag.OrderEmail = TempData["OrderEmail"];
            ViewBag.OrderTotal = total;
            ViewBag.OrderCurrency = TempData["OrderCurrency"];
            ViewBag.PayPalOrderId = TempData["PayPalOrderId"];

            return View();
        }

        private async Task<List<CartItem>> GetVerifiedCartAsync()
        {
            var cookieCart = GetCartFromCookie()
                .Where(item => item.ProductId > 0 && item.Quantity > 0)
                .ToList();
            if (cookieCart.Count == 0)
            {
                return [];
            }

            var productIds = cookieCart.Select(item => item.ProductId).Distinct().ToList();
            var products = await _context.Products
                .AsNoTracking()
                .Where(product => productIds.Contains(product.Id))
                .ToDictionaryAsync(product => product.Id);
            var verifiedCart = new List<CartItem>();

            foreach (var cookieItem in cookieCart)
            {
                if (!products.TryGetValue(cookieItem.ProductId, out var product))
                {
                    continue;
                }

                verifiedCart.Add(new CartItem
                {
                    ProductId = product.Id,
                    Name = product.Name ?? string.Empty,
                    Photo = product.Photo ?? string.Empty,
                    Price = product.PriceSale ?? product.Price ?? 0,
                    Quantity = Math.Min(cookieItem.Quantity, 99)
                });
            }

            SaveCartToCookie(verifiedCart);
            return verifiedCart;
        }
    }
}
