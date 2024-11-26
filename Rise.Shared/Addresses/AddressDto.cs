namespace Rise.Shared.Addresses
{
    public class AddressDto
    {
        public int Id { get; set; }
        public string? Street { get; set; }
        public string? HouseNumber { get; set; }
        public string? UnitNumber { get; set; }
        public string? City { get; set; }
        public string? PostalCode { get; set; }
        public string FullAddress =>
            $"{Street} {HouseNumber} "
            + (string.IsNullOrEmpty(UnitNumber) ? "" : $"bus {UnitNumber} ")
            + $"{PostalCode} {City}";
    }
}
