using PhoneStore.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhoneStore.Dtos.Product
{
    public class CreateProductRequest
    {
        [Required(ErrorMessage ="Please input Product Nane")]

        public string? Name { get; set; }
        public string? Description { get; set; }
        [Range(0, double.MaxValue, ErrorMessage = "Price must be a positive value")]

        public decimal? Price { get; set; }

        public decimal? PriceSale { get; set; }


        public IFormFile? Photo { get; set; }


        public int? CategoryId { get; set; }

    }
}