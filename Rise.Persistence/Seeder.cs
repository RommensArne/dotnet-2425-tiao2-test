using Rise.Domain.Addresses;
using Rise.Domain.Batteries;
using Rise.Domain.Boats;
using Rise.Domain.Prices;
using Rise.Domain.Users;

namespace Rise.Persistence;

public class Seeder
{
    private readonly ApplicationDbContext dbContext;

    public Seeder(ApplicationDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public void Seed()
    {
        if (!dbContext.Addresses.Any())
            SeedAddresses();

        if (!dbContext.Users.Any())
            SeedUsers();

        if (!dbContext.Boats.Any())
            SeedBoats();

        if (!dbContext.Batteries.Any())
            SeedBatteries();
        if (!dbContext.Prices.Any())
            SeedPrices();
    }

    private void SeedBoats()
    {
        if (!dbContext.Boats.Any())
        {
            var boats = new List<Boat>
            {
                new Boat("LIMBA", Boat.BoatStatus.Available),
                new Boat("LEITH", Boat.BoatStatus.Available),
                new Boat("LUBECK", Boat.BoatStatus.InRepair),
            };

            dbContext.Boats.AddRange(boats);
            dbContext.SaveChanges();
        }
    }

    private void SeedAddresses()
    {
        var addresses = new List<Address>
        {
            new Address("Korenmarkt", "12A", "Ghent", "9000"),
            new Address("Vrijdagmarkt", "34B", "Ghent", "9000"),
            new Address("Sint-Baafsplein", "56C", "Ghent", "9000"),
            new Address("Graslei", "10", "Ghent", "9000"),
            new Address("straat", "1", "Gent", "9000"),
        };

        foreach (var address in addresses)
        {
            if (string.IsNullOrWhiteSpace(address.Street))
            {
                Console.WriteLine(
                    $"Seeding Address: {address.Street}, {address.HouseNumber}, {address.City}, {address.PostalCode}"
                );

                throw new Exception($"Street is null or empty for address: {address.Street}");
            }
        }

        dbContext.Addresses.AddRange(addresses);
        dbContext.SaveChanges();
    }

    private void SeedUsers()
    {
        // Fetch addresses after saving
        var address1 = dbContext.Addresses.FirstOrDefault(a => a.Street == "Korenmarkt");
        if (address1 == null)
            throw new Exception("Address not found: Korenmarkt");

        var address2 = dbContext.Addresses.FirstOrDefault(a => a.Street == "Vrijdagmarkt");
        if (address2 == null)
            throw new Exception("Address not found: Vrijdagmarkt");

        var address3 = dbContext.Addresses.FirstOrDefault(a => a.Street == "straat");
        if (address3 == null)
            throw new Exception("Address not found: straat");

        var users = new List<User>
        {
            new User("auth0|123456", "John.Doe@gmail.com")
            {
                Firstname = "John",
                Lastname = "Doe",
                BirthDay = new DateTime(1985, 5, 10),
                PhoneNumber = "0491234567",
                Address = address1, // Ensure this is not null
                IsRegistrationComplete = true,
            },
            new User("auth0|123456", "Jane.Smith@gmail.com")
            {
                Auth0UserId = "auth0|654321",
                Firstname = "Jane",
                Lastname = "Smith",
                BirthDay = new DateTime(1990, 7, 15),
                PhoneNumber = "0491234565",
                Address = address2, // Ensure this is not null
                IsRegistrationComplete = true,
            },
            new User("auth0|67335cf5486ab548a84cfb9b", "testuser@mail.be")
            {
                Firstname = "Simon",
                Lastname = "Peeter",
                BirthDay = new DateTime(1988, 11, 24),
                PhoneNumber = "0498123456",
                Address = address3,
                IsRegistrationComplete = true,
            },
        };

        dbContext.Users.AddRange(users);
        dbContext.SaveChanges();
    }

    private void SeedBatteries()
    {
        var user = dbContext.Users.FirstOrDefault(u =>
            u.Firstname == "John" && u.Lastname == "Doe"
        );
        if (user == null)
        {
            throw new Exception("User not found: John Doe");
        }

        var batteries = new List<Battery>
        {
            new Battery("BATTERY1", Battery.BatteryStatus.Available, user),
            new Battery("BATTERY2", Battery.BatteryStatus.Available, user),
            new Battery("BATTERY3", Battery.BatteryStatus.Available, user),
        };

        dbContext.Batteries.AddRange(batteries);
        dbContext.SaveChanges();
    }

    private void SeedPrices()
    {
        var prices = new List<Price>
        {
            new Price(30m),
        };
        dbContext.Prices.AddRange(prices);
        dbContext.SaveChanges();
    }
}
