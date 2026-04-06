namespace ERPApp.Models
{
    public class Request
    {
        public int Id { get; set; }

        public required string Requestor { get; set; }
        public required string Item { get; set; }
        public double Price { get; set; }

        public string? Description { get; set; } = "NA";

        public string? Status { get; set; } = "PENDING";
    }
}