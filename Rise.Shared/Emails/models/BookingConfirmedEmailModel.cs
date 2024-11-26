namespace Rise.Shared.Emails.Models;

public class BookingConfirmedEmailModel
{
    public string? FirstName { get; set; }
    public DateTime RentalDate { get; set; }
    public string BookingId { get; set; }
}
