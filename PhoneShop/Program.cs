using Microsoft.EntityFrameworkCore;
using PhoneShop.Configuration;
using PhoneShop.DB;
using PhoneShop.Services;

var builder = WebApplication.CreateBuilder(args);

// Add db to the container.
builder.Services.AddDbContext<PhoneShopDbContext>(o => o.UseSqlServer(builder.Configuration.GetConnectionString("PhoneShopConnection")));
builder.Services.Configure<PayPalOptions>(
    builder.Configuration.GetSection(PayPalOptions.SectionName));
builder.Services.AddHttpClient<IPayPalService, PayPalService>();

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}"
    );

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
