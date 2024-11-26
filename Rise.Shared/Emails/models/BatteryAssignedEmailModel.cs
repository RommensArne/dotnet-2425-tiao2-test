namespace Rise.Shared.Emails.Models;

public class BatteryAssignedEmailModel
{
    public string? FirstName { get; set; }
    public DateTime RentalDate { get; set; }
    public string? BoatName { get; set; }
    public string? BatteryName { get; set; }
    public string UserId { get; set; }
}
