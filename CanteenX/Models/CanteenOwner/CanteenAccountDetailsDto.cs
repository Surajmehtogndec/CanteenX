namespace CanteenX.Models.CanteenOwner
{
    public class CanteenAccountDetailsDto
    {
     
        public int CanteenId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string? AccountHolderName { get; set; }
        public string? AccountNumber { get; set; }
        public string? IFSC { get; set; }
        public string? BankName { get; set; }
      }
      

    
}
