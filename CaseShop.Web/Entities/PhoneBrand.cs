namespace CaseShop.Web.Entities;

public class PhoneBrand
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public ICollection<PhoneModel> PhoneModels { get; set; } = new List<PhoneModel>();
}
