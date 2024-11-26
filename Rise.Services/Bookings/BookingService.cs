using Microsoft.EntityFrameworkCore;
using Rise.Domain.Batteries;
using Rise.Domain.Boats;
using Rise.Domain.Bookings;
using Rise.Domain.Prices;
using Rise.Persistence;
using Rise.Shared.Addresses;
using Rise.Shared.Batteries;
using Rise.Shared.Boats;
using Rise.Shared.Bookings;
using Rise.Shared.Emails;
using Rise.Shared.Emails.Models;
using Rise.Shared.Prices;
using Rise.Shared.Users;

namespace Rise.Services.Bookings;

public class BookingService(
    ApplicationDbContext dbContext,
    IBoatService boatService,
    IEmailTemplateService templateService,
    IEmailService emailService
) : IBookingService
{
    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly IBoatService _boatService = boatService;

    private readonly IEmailTemplateService _templateService = templateService;

    private readonly IEmailService _emailService = emailService;

    public async Task<IEnumerable<BookingDto.Index>?> GetAllBookingsAsync()
    {
        IQueryable<BookingDto.Index> query = _dbContext
            .Bookings.Where(b => !b.IsDeleted)
            .OrderBy(x => x.RentalDateTime)
            .Select(x => new BookingDto.Index
            {
                Id = x.Id,
                RentalDateTime = x.RentalDateTime,
                Boat =
                    x.Boat == null
                        ? null
                        : new BoatDto.BoatIndex { Id = x.Boat.Id, Name = x.Boat.Name },
                Battery =
                    x.Battery == null
                        ? null
                        : new BatteryDto.BatteryIndex
                        {
                            Id = x.Battery.Id,
                            Name = x.Battery.Name,
                            Status = x.Battery.Status.ToString(),
                        },
                Status = (BookingDto.BookingStatus)x.Status,
                User = new UserDto.Index
                {
                    Id = x.User.Id,
                    Firstname = x.User.Firstname ?? null,
                    Lastname = x.User.Lastname ?? null,
                },
            });

        var bookings = await query.ToListAsync();

        return bookings;
    }

    public async Task<IEnumerable<BookingDto.Index>?> GetAllCurrentBookingsAsync()
    {
        IQueryable<BookingDto.Index> query = _dbContext
            .Bookings.Where(x =>
                !x.IsDeleted
                && x.RentalDateTime >= DateTime.Today
                && x.Status == Booking.BookingStatus.Active
            )
            .Select(x => new BookingDto.Index
            {
                //No User!!
                Id = x.Id,
                RentalDateTime = x.RentalDateTime,
                Status = (BookingDto.BookingStatus)x.Status,
                User = new UserDto.Index { Id = x.User.Id },
            });

        var bookings = await query.ToListAsync();

        return bookings;
    }

    public async Task<BookingDto.Detail?> GetBookingByIdAsync(int bookingId)
    {
        BookingDto.Detail? booking =
            await _dbContext
                .Bookings.Where(b => !b.IsDeleted)
                .Select(b => new BookingDto.Detail
                {
                    Id = b.Id,
                    RentalDateTime = b.RentalDateTime,
                    Remark = b.Remark,
                    Boat =
                        b.Boat == null
                            ? null
                            : new BoatDto.BoatIndex { Id = b.Boat.Id, Name = b.Boat.Name },

                    Battery =
                        b.Battery == null
                            ? null
                            : new BatteryDto.BatteryDetail
                            {
                                Id = b.Battery.Id,
                                Name = b.Battery.Name,
                                Status = b.Battery.Status.ToString(),
                                User = new UserDto.Index
                                {
                                    Id = b.Battery.Id,
                                    Auth0UserId = b.Battery.User.Auth0UserId,
                                    Email = b.Battery.User.Email,
                                    Firstname = b.Battery.User.Firstname ?? null,
                                    Lastname = b.Battery.User.Lastname ?? null,
                                    PhoneNumber = b.Battery.User.PhoneNumber ?? null,
                                },
                            },
                    User = new UserDto.Index
                    {
                        Id = b.User.Id,
                        Firstname = b.User.Firstname,
                        Lastname = b.User.Lastname,
                        Auth0UserId = b.User.Auth0UserId,
                        Email = b.User.Email,
                        PhoneNumber = b.User.PhoneNumber,
                        BirthDay = b.User.BirthDay,
                        Address =
                            b.User.Address != null
                                ? new AddressDto
                                {
                                    Id = b.User.Address.Id,
                                    Street = b.User.Address.Street,
                                    HouseNumber = b.User.Address.HouseNumber,
                                    UnitNumber = b.User.Address.UnitNumber,
                                    City = b.User.Address.City,
                                    PostalCode = b.User.Address.PostalCode,
                                }
                                : null,
                        IsRegistrationComplete = b.User.IsRegistrationComplete,
                    },
                    Status = (BookingDto.BookingStatus)b.Status,
                    Price =
                        b.Price != null
                            ? new PriceDto.Index { Id = b.Price.Id, Amount = b.Price.Amount }
                            : null,
                })
                .SingleOrDefaultAsync(b => b.Id == bookingId)
            ?? throw new KeyNotFoundException($"Booking with ID {bookingId} was not found.");
        return booking;
    }

    public async Task<IEnumerable<BookingDto.Index>?> GetBookingsByUserIdAsync(int userId)
    {
        IQueryable<BookingDto.Index> query = _dbContext
            .Bookings.Where(b => b.User.Id == userId && !b.IsDeleted)
            .OrderBy(x => x.RentalDateTime)
            .Select(x => new BookingDto.Index
            {
                Id = x.Id,
                RentalDateTime = x.RentalDateTime,
                Boat =
                    x.Boat == null
                        ? null
                        : new BoatDto.BoatIndex { Id = x.Boat.Id, Name = x.Boat.Name },
                Battery =
                    x.Battery == null
                        ? null
                        : new BatteryDto.BatteryIndex
                        {
                            Id = x.Battery.Id,
                            Name = x.Battery.Name,
                            Status = x.Battery.Status.ToString(),
                        },
                Status = (BookingDto.BookingStatus)x.Status,
                User = new UserDto.Index
                {
                    Id = x.User.Id,
                    Firstname = x.User.Firstname ?? null,
                    Lastname = x.User.Lastname ?? null,
                },
            });

        var bookings = await query.ToListAsync();

        return bookings;
    }

    public async Task<int> CreateBookingAsync(BookingDto.Mutate model)
    {
        await ValidateNoExistingUserBooking(model.UserId, model.RentalDateTime);

        int existingBookingsCount = await _dbContext.Bookings.CountAsync(x =>
            !x.IsDeleted
            && x.Status == Booking.BookingStatus.Active
            && x.RentalDateTime == model.RentalDateTime
        );

        var blockedTimeSlot = await dbContext.TimeSlots.FirstOrDefaultAsync(x =>
            x.Date == model.RentalDateTime
        );

        if (blockedTimeSlot != null)
        {
            throw new InvalidOperationException("The specified rental date and time are blocked.");
        }

        if (existingBookingsCount >= await _boatService.GetAvailableBoatsCountAsync())
        {
            throw new InvalidOperationException(
                "The specified rental date and time are fully booked."
            );
        }
        Boat? boat = null;

        if (model.BoatId != null)
        {
            boat =
                await _dbContext.Boats.FirstOrDefaultAsync(b =>
                    !b.IsDeleted && b.Id == model.BoatId
                ) ?? throw new KeyNotFoundException($"Boat with ID {model.BoatId} not found.");

            if (boat.Status != Boat.BoatStatus.Available)
            {
                throw new InvalidOperationException("The specified boat is not available.");
            }
            int existingBookingsCountForBoat = await _dbContext.Bookings.CountAsync(x =>
                !x.IsDeleted
                && x.Status != Booking.BookingStatus.Canceled
                && x.RentalDateTime == model.RentalDateTime
                && x.BoatId == model.BoatId
            );
            if (existingBookingsCountForBoat >= 1)
            {
                throw new InvalidOperationException(
                    "The specified boat is already booked for the specified rental date and time."
                );
            }
        }
        Battery? battery = null;
        if (model.BatteryId != null)
        {
            battery =
                await _dbContext.Batteries.FirstOrDefaultAsync(b =>
                    !b.IsDeleted && b.Id == model.BatteryId
                )
                ?? throw new KeyNotFoundException($"Battery with ID {model.BatteryId} not found.");

            if (battery.Status != Battery.BatteryStatus.Available)
            {
                throw new InvalidOperationException("The specified battery is not available.");
            }
            int existingBookingsCountForBatteryOnSameDay = await _dbContext.Bookings.CountAsync(x =>
                !x.IsDeleted
                && x.Status != Booking.BookingStatus.Canceled
                && x.RentalDateTime.Date == model.RentalDateTime.Date
                && x.BatteryId == model.BatteryId
            );
            if (existingBookingsCountForBatteryOnSameDay >= 1)
            {
                throw new InvalidOperationException(
                    "The specified battery is already booked for the specified rental date."
                );
            }
        }

        var user =
            await _dbContext.Users.FirstOrDefaultAsync(u => !u.IsDeleted && u.Id == model.UserId)
            ?? throw new KeyNotFoundException($"User with ID {model.UserId} not found.");

        var price =
            await _dbContext.Prices.FirstOrDefaultAsync(p => !p.IsDeleted && p.Id == model.PriceId)
            ?? throw new KeyNotFoundException($"Price with ID {model.PriceId} not found.");

        Booking booking =
            new(boat, battery, model.RentalDateTime, Booking.BookingStatus.Active, user, price)
            {
                BoatId = boat?.Id,
                BatteryId = battery?.Id,
                Remark = model.Remark,
            };

        _dbContext.Bookings.Add(booking);
        await _dbContext.SaveChangesAsync();

        await SendBookingConfirmedEmailAsync(booking);

        return booking.Id;
    }

    public async Task<bool> UpdateBookingAsync(int bookingId, BookingDto.Mutate model)
    {
        if ((Booking.BookingStatus)model.Status == Booking.BookingStatus.Canceled)
        {
            return await CancelBookingAsync(bookingId);
        }

        var booking = await _dbContext.Bookings.FindAsync(bookingId);
        if (booking is null || booking.IsDeleted)
        {
            return false;
        }
        if (model.BoatId is not null)
        {
            var boat = await _dbContext.Boats.FindAsync(model.BoatId);
            if (boat is null || booking.IsDeleted)
            {
                throw new KeyNotFoundException($"Boat with ID {model.BoatId} not found.");
            }
            booking.BoatId = model.BoatId;
        }
        else
        {
            booking.BoatId = null;
        }

        if (model.BatteryId is not null)
        {
            var battery = await _dbContext.Batteries.FindAsync(model.BatteryId);
            if (battery is null || battery.IsDeleted)
            {
                throw new KeyNotFoundException($"Battery with ID {model.BatteryId} not found.");
            }
            booking.BatteryId = model.BatteryId;
        }
        else
        {
            booking.BatteryId = null;
        }

        booking.PriceId = model.PriceId;
        booking.RentalDateTime = model.RentalDateTime;
        booking.Status = (Booking.BookingStatus)model.Status;
        booking.Remark = model.Remark;

        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> CancelBookingAsync(int bookingId)
    {
        var booking =
            await _dbContext.Bookings.SingleOrDefaultAsync(b => b.Id == bookingId)
            ?? throw new KeyNotFoundException($"Booking with ID {bookingId} was not found.");

        if (booking.Status == Booking.BookingStatus.Canceled)
        {
            throw new InvalidOperationException("The booking has already been canceled.");
        }
        booking.Status = Booking.BookingStatus.Canceled;

        await _dbContext.SaveChangesAsync();
        //TODO: Send email to user
        return true;
    }

    public async Task<bool> DeleteBookingAsync(int bookingId)
    {
        var booking = await _dbContext.Bookings.FindAsync(bookingId);
        if (booking is null)
        {
            return false;
        }

        booking.IsDeleted = true;
        await _dbContext.SaveChangesAsync();
        return true;
    }

    //private helper functions

    private async Task ValidateNoExistingUserBooking(int userId, DateTime rentalDateTime)
    {
        var existingUserBooking = await _dbContext.Bookings.FirstOrDefaultAsync(x =>
            !x.IsDeleted
            && x.Status != Booking.BookingStatus.Canceled
            && x.RentalDateTime == rentalDateTime
            && x.UserId == userId
        );

        if (existingUserBooking != null)
        {
            throw new InvalidOperationException(
                "You already have a booking for the specified rental date and time."
            );
        }
    }

    public async Task SendBookingConfirmedEmailAsync(Booking booking)
    {
        var model = new BookingConfirmedEmailModel
        {
            FirstName = booking.User?.Firstname,
            RentalDate = booking.RentalDateTime,
            BookingId = booking.Id.ToString(),
        };

        string emailContent = await _templateService.RenderTemplateAsync("BookingConfirmed", model);

        await _emailService.SendEmailAsync(booking.User?.Email, "Boeking bevestigd", emailContent);
    }
}
