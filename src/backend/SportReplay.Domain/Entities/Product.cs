using SportReplay.Domain.Common;
using SportReplay.Domain.Enums;

namespace SportReplay.Domain.Entities;

public class Product : EntityBase
{
    public Guid ClubId { get; set; }
    public ProductType Type { get; set; } = ProductType.Clip;
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = "ARS";
    public bool IsActive { get; set; } = true;

    public Club Club { get; set; } = null!;
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
