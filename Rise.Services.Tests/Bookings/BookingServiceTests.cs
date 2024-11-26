using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using Rise.Domain.Batteries;
using Rise.Domain.Boats;
using Rise.Domain.Bookings;
using Rise.Domain.Prices;
using Rise.Domain.Users;
using Rise.Persistence;
using Rise.Services.Bookings;
using Rise.Shared.Boats;
using Rise.Shared.Bookings;
using Rise.Shared.Prices;
using Xunit;
using Xunit.Abstractions;

namespace Rise.Services.Tests
{
    public class BookingServiceTests : IDisposable
    {
        private readonly Mock<IBoatService> _mockBoatService;
        private readonly ApplicationDbContext _dbContext;
        private readonly BookingService _bookingService;
        private readonly ITestOutputHelper _output;

        private string auth0UserId = "auth0|123";
        private string email = "test@example.com";

        public BookingServiceTests(ITestOutputHelper output)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "BookingTestDb")
                .Options;
            _output = output;
            _dbContext = new ApplicationDbContext(options);
            _mockBoatService = new Mock<IBoatService>();
            _bookingService = new BookingService(_dbContext, _mockBoatService.Object);
        }

        private ApplicationDbContext CreateNewContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        public void Dispose()
        {
            _dbContext.Database.EnsureDeleted();
        }

        [Fact]
        public async Task CreateBookingAsync_ValidModelWithBoat_ReturnsBookingId()
        {
            Boat boat = new Boat("boat1", Boat.BoatStatus.Available);
            await _dbContext.Boats.AddAsync(boat);
            await _dbContext.SaveChangesAsync();
            User user = new User(auth0UserId, email);
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();
            Price price = new Price(30m);
            _dbContext.Prices.Add(price);
            await _dbContext.SaveChangesAsync();

            _mockBoatService
                .Setup(service => service.GetAvailableBoatsCountAsync())
                .ReturnsAsync(1);

            var model = new BookingDto.Mutate
            {
                RentalDateTime = DateTime.Now.AddDays(5),
                BoatId = boat.Id,
                UserId = user.Id,
                Status = BookingDto.BookingStatus.Active,
                PriceId = price.Id,
            };

            //Act
            var bookingId = await _bookingService.CreateBookingAsync(model);

            //Assert
            Assert.True(bookingId > 0);
            var createdBooking = await _dbContext.Bookings.FindAsync(bookingId);
            Assert.NotNull(createdBooking);
            Assert.Equal(model.RentalDateTime, createdBooking.RentalDateTime);
            Assert.Equal(boat.Id, createdBooking.Boat.Id);
            Assert.Null(createdBooking.Battery);
            Assert.Equal(Booking.BookingStatus.Active, createdBooking.Status);
            Assert.Equal(price.Id, createdBooking.Price.Id);
        }

        [Fact]
        public async Task CreateBookingAsync_ValidModelWithBattery_ReturnsBookingId()
        {
            User user = new User(auth0UserId, email);
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();
            Price price = new Price(30m);
            _dbContext.Prices.Add(price);
            await _dbContext.SaveChangesAsync();

            Battery battery = new("Battery 100", Battery.BatteryStatus.Available, user);
            _dbContext.Batteries.AddAsync(battery);
            await _dbContext.SaveChangesAsync();

            _mockBoatService
                .Setup(service => service.GetAvailableBoatsCountAsync())
                .ReturnsAsync(1);

            var model = new BookingDto.Mutate
            {
                RentalDateTime = DateTime.Now.AddDays(3),
                BatteryId = battery.Id,
                UserId = user.Id,
                Status = BookingDto.BookingStatus.Active,
                PriceId = price.Id,
            };

            //Act
            var bookingId = await _bookingService.CreateBookingAsync(model);

            //Assert
            Assert.True(bookingId > 0);
            var createdBooking = await _dbContext.Bookings.FindAsync(bookingId);
            Assert.NotNull(createdBooking);
            Assert.Equal(model.RentalDateTime, createdBooking.RentalDateTime);
            Assert.Equal(battery.Id, createdBooking.Battery.Id);
            Assert.Null(createdBooking.Boat);
            Assert.Equal(Booking.BookingStatus.Active, createdBooking.Status);
        }

        [Fact]
        public async Task CreateBookingAsync_ValidModelWithoutBatteryAndBoat_ReturnsBookingId()
        {
            User user = new User(auth0UserId, email);
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();
            Price price = new Price(30m);
            _dbContext.Prices.Add(price);
            await _dbContext.SaveChangesAsync();
            _mockBoatService
                .Setup(service => service.GetAvailableBoatsCountAsync())
                .ReturnsAsync(1);

            var model = new BookingDto.Mutate
            {
                RentalDateTime = DateTime.Now.AddDays(3),
                UserId = user.Id,
                Status = BookingDto.BookingStatus.Active,
                PriceId = price.Id,
            };

            //Act
            var bookingId = await _bookingService.CreateBookingAsync(model);

            //Assert
            Assert.True(bookingId > 0);
            var createdBooking = await _dbContext.Bookings.FindAsync(bookingId);
            Assert.NotNull(createdBooking);
            Assert.Equal(model.RentalDateTime, createdBooking.RentalDateTime);
            Assert.Null(createdBooking.Battery);
            Assert.Null(createdBooking.Boat);
            Assert.Equal(Booking.BookingStatus.Active, createdBooking.Status);
        }

        [Fact]
        public async Task CreateBookingAsync_TimeSlotIsFullyBooked_ThrowsInvalidOperationException()
        {
            // Arrange
            Boat boat = new Boat("boat1", Boat.BoatStatus.Available);
            await _dbContext.Boats.AddRangeAsync(boat);
            await _dbContext.SaveChangesAsync();
            User user = new User(auth0UserId, email);
            User user2 = new User("auth|123", "user2@test.com");
            _dbContext.Users.AddRange(user, user2);
            await _dbContext.SaveChangesAsync();
            Price price = new Price(30m);
            _dbContext.Prices.Add(price);
            await _dbContext.SaveChangesAsync();
            DateTime rentalDateTime = DateTime.Now.AddDays(5);
            _mockBoatService
                .Setup(service => service.GetAvailableBoatsCountAsync())
                .ReturnsAsync(1);

            var existingBooking = new Booking(
                boat,
                null,
                rentalDateTime,
                Booking.BookingStatus.Active,
                user2,
                price
            );
            await _dbContext.Bookings.AddAsync(existingBooking);
            await _dbContext.SaveChangesAsync();

            var model = new BookingDto.Mutate
            {
                RentalDateTime = rentalDateTime,
                UserId = user.Id,
                Status = BookingDto.BookingStatus.Active,
                PriceId = price.Id,
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _bookingService.CreateBookingAsync(model)
            );
            Assert.Equal("The specified rental date and time are fully booked.", exception.Message);
        }

        [Fact]
        public async Task CreateBookingAsync_WithBoatThatsAlreadyBookedOnSameTimeSlot_ThrowsInvalidOperationException()
        {
            // Arrange
            Boat boat = new Boat("boat1", Boat.BoatStatus.Available);
            await _dbContext.Boats.AddAsync(boat);
            await _dbContext.SaveChangesAsync();
            User user = new User(auth0UserId, email);
            User user2 = new User("auth|123", "user2@test.com");
            _dbContext.Users.AddRange(user, user2);
            await _dbContext.SaveChangesAsync();
            Price price = new Price(30m);
            _dbContext.Prices.Add(price);
            await _dbContext.SaveChangesAsync();
            DateTime rentalDateTime = DateTime.Now.AddDays(5);

            _mockBoatService
                .Setup(service => service.GetAvailableBoatsCountAsync())
                .ReturnsAsync(2);

            var existingBooking = new Booking(
                boat,
                null,
                rentalDateTime,
                Booking.BookingStatus.Active,
                user2,
                price
            );
            await _dbContext.Bookings.AddAsync(existingBooking);
            await _dbContext.SaveChangesAsync();

            var model = new BookingDto.Mutate
            {
                BoatId = boat.Id,
                RentalDateTime = rentalDateTime,
                UserId = user.Id,
                Status = BookingDto.BookingStatus.Active,
                PriceId = price.Id,
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _bookingService.CreateBookingAsync(model)
            );
            Assert.Equal(
                "The specified boat is already booked for the specified rental date and time.",
                exception.Message
            );
        }

        [Fact]
        public async Task CreateBookingAsync_WithBatteryThatsAlreadyBookedOnSameDay_ThrowsInvalidOperationException()
        {
            // Arrange
            Boat boat = new Boat("boat1", Boat.BoatStatus.Available);
            await _dbContext.Boats.AddAsync(boat);
            await _dbContext.SaveChangesAsync();
            User user = new User(auth0UserId, email);
            User user2 = new User("auth|123", "user2@test.com");
            _dbContext.Users.AddRange(user, user2);
            await _dbContext.SaveChangesAsync();
            Battery battery = new Battery("Battery 100", Battery.BatteryStatus.Available, user);
            _dbContext.Batteries.Add(battery);
            await _dbContext.SaveChangesAsync();
            Price price = new Price(30m);
            _dbContext.Prices.Add(price);
            await _dbContext.SaveChangesAsync();
            DateTime rentalDateTime = DateTime.Today.AddDays(5);

            _mockBoatService
                .Setup(service => service.GetAvailableBoatsCountAsync())
                .ReturnsAsync(2);

            var existingBooking = new Booking(
                null,
                battery,
                rentalDateTime.AddHours(10),
                Booking.BookingStatus.Active,
                user2,
                price
            );
            await _dbContext.Bookings.AddAsync(existingBooking);
            await _dbContext.SaveChangesAsync();

            var model = new BookingDto.Mutate
            {
                BatteryId = battery.Id,
                RentalDateTime = rentalDateTime.AddHours(15),
                UserId = user.Id,
                Status = BookingDto.BookingStatus.Active,
                PriceId = price.Id,
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _bookingService.CreateBookingAsync(model)
            );
            Assert.Equal(
                "The specified battery is already booked for the specified rental date.",
                exception.Message
            );
        }

        [Fact]
        public async Task GetBookingByIdAsync_ValidId_ReturnsBookingDetail()
        {
            //Arrange
            User user = new User(auth0UserId, email);
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();
            var boat = new Boat("Test Boat", Boat.BoatStatus.Available);
            await _dbContext.Boats.AddAsync(boat);
            await _dbContext.SaveChangesAsync();
            Price price = new Price(30m);
            _dbContext.Prices.Add(price);
            await _dbContext.SaveChangesAsync();

            var booking = new Booking(
                boat,
                null,
                DateTime.Now.AddDays(5),
                Booking.BookingStatus.Active,
                user,
                price
            );
            await _dbContext.Bookings.AddAsync(booking);
            await _dbContext.SaveChangesAsync();

            //Act
            var bookingDetail = await _bookingService.GetBookingByIdAsync(booking.Id);

            //Assert
            Assert.NotNull(bookingDetail);
            Assert.Equal(booking.Id, bookingDetail.Id);
            Assert.Equal(booking.RentalDateTime, bookingDetail.RentalDateTime);
            Assert.Equal(boat.Id, bookingDetail.Boat.Id);
            Assert.Equal(boat.Name, bookingDetail.Boat.Name);
        }

        [Fact]
        public async Task GetBookingByIdAsync_InvalidId_ThrowsKeyNotFoundException()
        {
            //Arrange
            var invalidId = 999;

            //Assert
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _bookingService.GetBookingByIdAsync(invalidId)
            );

            //Assert
            Assert.Equal($"Booking with ID {invalidId} was not found.", exception.Message);
        }

        [Fact]
        public async Task CancelBookingByIdAsync_ValidId()
        {
            //Arrange
            User user = new User(auth0UserId, email);
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();
            var boat = new Boat("Test Boat", Boat.BoatStatus.Available);
            await _dbContext.Boats.AddAsync(boat);
            await _dbContext.SaveChangesAsync();
            Price price = new Price(30m);
            _dbContext.Prices.Add(price);
            await _dbContext.SaveChangesAsync();

            var booking = new Booking(
                boat,
                null,
                DateTime.Now.AddDays(5),
                Booking.BookingStatus.Active,
                user,
                price
            );
            await _dbContext.Bookings.AddAsync(booking);
            await _dbContext.SaveChangesAsync();

            //Act
            await _bookingService.CancelBookingAsync(booking.Id);
            var canceledBooking = await _dbContext.Bookings.FindAsync(booking.Id);

            //Assert
            Assert.NotNull(canceledBooking);
            Assert.Equal(Booking.BookingStatus.Canceled, canceledBooking.Status);
        }

        [Fact]
        public async Task CancelBookingByIdAsync_InValidId_KeyNotFoundException()
        {
            //Arrange
            User user = new User(auth0UserId, email);
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();
            var boat = new Boat("Test Boat", Boat.BoatStatus.Available);
            await _dbContext.Boats.AddAsync(boat);
            await _dbContext.SaveChangesAsync();
            Price price = new Price(30m);
            _dbContext.Prices.Add(price);
            await _dbContext.SaveChangesAsync();

            var booking = new Booking(
                boat,
                null,
                DateTime.Now.AddDays(5),
                Booking.BookingStatus.Active,
                user,
                price
            );
            await _dbContext.Bookings.AddAsync(booking);
            await _dbContext.SaveChangesAsync();

            //Act && Assert
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _bookingService.CancelBookingAsync(999)
            );
        }

        [Fact]
        public async Task CancelBookingByIdAsync_AlreadyCanceled_ThrowsInvalidOperationException()
        {
            // Arrange
            User user = new User(auth0UserId, email);
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();
            Price price = new Price(30m);
            _dbContext.Prices.Add(price);
            await _dbContext.SaveChangesAsync();

            var booking = new Booking(
                null,
                null,
                DateTime.Now.AddDays(5),
                Booking.BookingStatus.Canceled,
                user,
                price
            );
            await _dbContext.Bookings.AddAsync(booking);
            await _dbContext.SaveChangesAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _bookingService.CancelBookingAsync(booking.Id)
            );

            Assert.Equal("The booking has already been canceled.", exception.Message);
        }

        [Fact]
        public async Task CancelBookingByIdAsync_InvalidId_ThrowsKeyNotFoundException()
        {
            // Arrange
            var invalidId = 999; // Assume this ID does not exist

            // Act & Assert
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _bookingService.CancelBookingAsync(invalidId)
            );

            Assert.Equal($"Booking with ID {invalidId} was not found.", exception.Message);
        }

        [Fact]
        public async Task GetAllBookingsAsync_ReturnsAllBookings()
        {
            // Arrange
            User user = new User(auth0UserId, email);
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();
            Price price = new Price(30m);
            _dbContext.Prices.Add(price);
            await _dbContext.SaveChangesAsync();

            var activeBooking = new Booking(
                null,
                null,
                DateTime.Now.AddDays(5),
                Booking.BookingStatus.Active,
                user,
                price
            );
            var canceledBooking = new Booking(
                null,
                null,
                DateTime.Now.AddDays(7),
                Booking.BookingStatus.Canceled,
                user,
                price
            );

            await _dbContext.Bookings.AddRangeAsync(activeBooking, canceledBooking);
            await _dbContext.SaveChangesAsync();

            // Act
            var bookings = await _bookingService.GetAllBookingsAsync();

            // Assert
            Assert.Equal(2, bookings.Count());
        }

        [Fact]
        public async Task GetAllBookingsByUserId_ReturnsAllBookingsByUserId()
        {
            // Arrange
            User user = new User(auth0UserId, email);
            User user2 = new User("auth|123", "user2@test.com");
            _dbContext.Users.AddRange(user, user2);
            await _dbContext.SaveChangesAsync();
            Price price = new Price(30m);
            _dbContext.Prices.Add(price);
            await _dbContext.SaveChangesAsync();

            var activeBooking = new Booking(
                null,
                null,
                DateTime.Now.AddDays(5),
                Booking.BookingStatus.Active,
                user,
                price
            );
            var canceledBooking = new Booking(
                null,
                null,
                DateTime.Now.AddDays(7),
                Booking.BookingStatus.Canceled,
                user,
                price
            );
            var activeBooking2 = new Booking(
                null,
                null,
                DateTime.Now.AddDays(10),
                Booking.BookingStatus.Active,
                user2,
                price
            );
            var activeBooking3 = new Booking(
                null,
                null,
                DateTime.Now.AddDays(11),
                Booking.BookingStatus.Active,
                user2,
                price
            );

            await _dbContext.Bookings.AddRangeAsync(
                activeBooking,
                canceledBooking,
                activeBooking2,
                activeBooking3
            );
            await _dbContext.SaveChangesAsync();

            // Act
            var bookings = await _bookingService.GetBookingsByUserIdAsync(user.Id);

            // Assert
            Assert.Equal(2, bookings.Count());
            Assert.All(bookings, booking => Assert.Equal(user.Id, booking.User!.Id));
        }

        [Fact]
        public async Task GetAllCurrentBookingsAsync_ReturnsAllCurrentBookings()
        {
            // Arrange
            User user = new User(auth0UserId, email);
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();
            Price price = new Price(30m);
            _dbContext.Prices.Add(price);
            await _dbContext.SaveChangesAsync();

            var activeBooking = new Booking(
                null,
                null,
                DateTime.Now.AddDays(5),
                Booking.BookingStatus.Active,
                user,
                price
            );
            var canceledBooking = new Booking(
                null,
                null,
                DateTime.Now.AddDays(7),
                Booking.BookingStatus.Canceled,
                user,
                price
            );
            var activeBooking2 = new Booking(
                null,
                null,
                DateTime.Now.AddDays(10),
                Booking.BookingStatus.Active,
                user,
                price
            );

            await _dbContext.Bookings.AddRangeAsync(activeBooking, canceledBooking, activeBooking2);
            await _dbContext.SaveChangesAsync();

            // Act
            var bookings = await _bookingService.GetAllCurrentBookingsAsync();

            // Assert
            Assert.Equal(2, bookings.Count());
            Assert.All(bookings, booking => Assert.True(booking.RentalDateTime >= DateTime.Now));
        }

        [Fact]
        public async Task DeleteBookingAsync_Valid_ReturnsTrue()
        {
            // Arrange
            User user = new User(auth0UserId, email);
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();
            Price price = new Price(30m);
            _dbContext.Prices.Add(price);
            await _dbContext.SaveChangesAsync();

            var booking = new Booking(
                null,
                null,
                DateTime.Now.AddDays(5),
                Booking.BookingStatus.Active,
                user,
                price
            );
            await _dbContext.Bookings.AddAsync(booking);
            await _dbContext.SaveChangesAsync();

            // Act
            var isDeleted = await _bookingService.DeleteBookingAsync(booking.Id);

            // Assert
            Assert.True(isDeleted);
            Assert.True(booking.IsDeleted);
        }

        [Fact]
        public async Task DeleteBookingAsync_InValid_ReturnsFalse()
        {
            // Arrange
            User user = new User(auth0UserId, email);
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();
            Price price = new Price(30m);
            _dbContext.Prices.Add(price);
            await _dbContext.SaveChangesAsync();

            var booking = new Booking(
                null,
                null,
                DateTime.Now.AddDays(5),
                Booking.BookingStatus.Active,
                user,
                price
            );
            await _dbContext.Bookings.AddAsync(booking);
            await _dbContext.SaveChangesAsync();

            // Act
            var isDeleted = await _bookingService.DeleteBookingAsync(999);

            // Assert
            Assert.False(isDeleted);
        }

        [Fact]
        public async Task UpdateBookingAsync_ValidModel_ReturnsTrue()
        {
            // Arrange
            User user = new User(auth0UserId, email);
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();
            Price price = new Price(30m);
            _dbContext.Prices.Add(price);
            await _dbContext.SaveChangesAsync();

            var booking = new Booking(
                null,
                null,
                DateTime.Now.AddDays(5),
                Booking.BookingStatus.Active,
                user,
                price
            );
            await _dbContext.Bookings.AddAsync(booking);
            await _dbContext.SaveChangesAsync();

            var model = new BookingDto.Mutate
            {
                RentalDateTime = DateTime.Now.AddDays(10),
                UserId = user.Id,
                Status = BookingDto.BookingStatus.Active,
                Remark = "Updated Remark",
                PriceId = price.Id,
            };

            // Act
            var isUpdated = await _bookingService.UpdateBookingAsync(booking.Id, model);

            // Assert
            Assert.True(isUpdated);
            Assert.Equal(model.RentalDateTime, booking.RentalDateTime);
            Assert.Equal(model.Remark, booking.Remark);
        }

        [Fact]
        public async Task UpdateBookingAsync_ValidModelCancel_ReturnsTrue()
        {
            // Arrange
            User user = new User(auth0UserId, email);
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();
            Price price = new Price(30m);
            _dbContext.Prices.Add(price);
            await _dbContext.SaveChangesAsync();
            DateTime rentalDateTime = DateTime.Now.AddDays(5);
            var booking = new Booking(
                null,
                null,
                rentalDateTime,
                Booking.BookingStatus.Active,
                user,
                price
            );
            await _dbContext.Bookings.AddAsync(booking);
            await _dbContext.SaveChangesAsync();

            var model = new BookingDto.Mutate
            {
                RentalDateTime = rentalDateTime,
                UserId = user.Id,
                Status = BookingDto.BookingStatus.Canceled,
                PriceId = price.Id,
            };

            // Act
            var isUpdated = await _bookingService.UpdateBookingAsync(booking.Id, model);

            // Assert
            Assert.True(isUpdated);
            Assert.Equal(Booking.BookingStatus.Canceled, booking.Status);
        }

        [Fact]
        public async Task UpdateBookingAsync_InValidBattery_KeyNotFoundException()
        {
            // Arrange
            User user = new User(auth0UserId, email);
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();
            Price price = new Price(30m);
            _dbContext.Prices.Add(price);
            await _dbContext.SaveChangesAsync();

            var booking = new Booking(
                null,
                null,
                DateTime.Now.AddDays(5),
                Booking.BookingStatus.Active,
                user,
                price
            );
            await _dbContext.Bookings.AddAsync(booking);
            await _dbContext.SaveChangesAsync();

            var model = new BookingDto.Mutate
            {
                BatteryId = 999,
                RentalDateTime = DateTime.Now.AddDays(10),
                UserId = user.Id,
                Status = BookingDto.BookingStatus.Active,
                PriceId = price.Id,
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _bookingService.UpdateBookingAsync(booking.Id, model)
            );
        }

        [Fact]
        public async Task UpdateBookingAsync_InValidBoat_KeyNotFoundException()
        {
            // Arrange
            User user = new User(auth0UserId, email);
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();
            Price price = new Price(30m);
            _dbContext.Prices.Add(price);
            await _dbContext.SaveChangesAsync();

            var booking = new Booking(
                null,
                null,
                DateTime.Now.AddDays(5),
                Booking.BookingStatus.Active,
                user,
                price
            );
            await _dbContext.Bookings.AddAsync(booking);
            await _dbContext.SaveChangesAsync();

            var model = new BookingDto.Mutate
            {
                BoatId = 999,
                RentalDateTime = DateTime.Now.AddDays(10),
                UserId = user.Id,
                Status = BookingDto.BookingStatus.Active,
                PriceId = price.Id,
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _bookingService.UpdateBookingAsync(booking.Id, model)
            );
        }
    }
}
