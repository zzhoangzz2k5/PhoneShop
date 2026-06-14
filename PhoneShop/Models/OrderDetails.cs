using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhoneShop.Models;

[Table("OrdersDetails")]
public class OrdersDetails
{
    [Key]
    public int Id { get; set; }

    [Column(TypeName = "money")]
    public decimal Price { get; set; }

    public int Quantity { get; set; }

    public int OrdersId { get; set; }
    public Orders? Orders { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }
}
