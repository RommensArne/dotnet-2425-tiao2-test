using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Rise.Shared.Addresses;
using Rise.Shared.Users;

namespace Rise.Client.Pages;

public partial class AdditionalRegistration
{
    private RegisterAccountForm model = new RegisterAccountForm
    {
        Firstname = string.Empty,
        Lastname = string.Empty,
        Street = string.Empty,
        HouseNumber = string.Empty,
        PostalCode = string.Empty,
        City = string.Empty,
        PhoneNumber = string.Empty,
        BirthDay = null,
    };
    private bool success;

    private class RegisterAccountForm
    {
        [Required(ErrorMessage = "Voornaam is verplicht.")]
        [StringLength(50, ErrorMessage = "Voornaam mag max {1} karakters bevatten.")]
        [MinLength(2, ErrorMessage = "Voornaam moet minstens {1} karakters bevatten.")]
        public required string Firstname { get; set; }

        [Required(ErrorMessage = "Achternaam is verplicht.")]
        [StringLength(50, ErrorMessage = "Achternaam mag max {1} karakters bevatten.")]
        [MinLength(2, ErrorMessage = "Achternaam moet minstens {1} karakters bevatten.")]
        public required string Lastname { get; set; }

        [Required(ErrorMessage = "Straatnaam is verplicht.")]
        [StringLength(100, ErrorMessage = "Straat mag max {1} karakters bevatten.")]
        [MinLength(2, ErrorMessage = "Straat moet minstens {1} karakters bevatten.")]
        public required string Street { get; set; }

        [Required(ErrorMessage = "Huisnummer is verplicht.")]
        [StringLength(6, ErrorMessage = "Huisnummer mag max {1} karakters bevatten.")]
        [RegularExpression(
            @"^[1-9]\w*",
            ErrorMessage = "Huisnummer moet beginnen met een cijfer van 1 tot 9."
        )]
        public required string HouseNumber { get; set; }

        [StringLength(10, ErrorMessage = "Bus mag max {1} karakters bevatten.")]
        public string? UnitNumber { get; set; }

        [Required(ErrorMessage = "Postcode is verplicht.")]
        [RegularExpression(
            @"^[1-9]\d{3}$",
            ErrorMessage = "Postcode moet exact 4 cijfers bevatten, niet starten met 0."
        )]
        public required string PostalCode { get; set; }

        [Required(ErrorMessage = "Plaats is verplicht.")]
        [StringLength(50, ErrorMessage = "Plaats mag max {1} karakters bevatten.")]
        [MinLength(2, ErrorMessage = "Plaats moet minstens {1} karakters bevatten.")]
        public required string City { get; set; }

        [Required(ErrorMessage = "Telefoonnummer is verplicht.")]
        [BelgianPhoneNumber]
        public required string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Geboortedatum is verplicht.")]
        [MinimumAge(18)]
        public required DateTime? BirthDay { get; set; }
    }

    private class BelgianPhoneNumberAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(
            object? value,
            ValidationContext validationContext
        )
        {
            string phoneNumber = value as string ?? string.Empty;

            // Validatie voor mobiel nummer dat begint met 04 en 10 cijfers bevat
            if (Regex.IsMatch(phoneNumber, @"^04\d{8}$"))
            {
                return ValidationResult.Success!;
            }

            // Validatie voor mobiel nummer dat begint met +32 en 11 cijfers bevat
            if (Regex.IsMatch(phoneNumber, @"^\+32\d{9}$"))
            {
                return ValidationResult.Success!;
            }

            // Als geen van de regels voldoet, dan een foutmelding geven
            return new ValidationResult(
                "Ongeldig Belgisch telefoonnummer. Het moet beginnen met 04 (10 cijfers) of +32 (11 cijfers)."
            );
        }
    }

    private class MinimumAgeAttribute : ValidationAttribute
    {
        private readonly int _minimumAge;

        public MinimumAgeAttribute(int minimumAge)
        {
            _minimumAge = minimumAge;
            ErrorMessage = $"Leeftijd moet minstens {_minimumAge} jaar zijn.";
        }

        protected override ValidationResult? IsValid(
            object? value,
            ValidationContext validationContext
        )
        {
            if (value is DateTime birthDate)
            {
                var today = DateTime.Today;
                var age = today.Year - birthDate.Year;
                if (birthDate.Date > today.AddYears(-age))
                    age--;

                if (age < _minimumAge)
                {
                    return new ValidationResult(ErrorMessage);
                }
            }
            return ValidationResult.Success;
        }
    }

    private async Task OnValidSubmit(EditContext context)
    {
        success = true;

        //auth0UserId
        AuthenticationState authState =
            await AuthenticationStateProvider.GetAuthenticationStateAsync();
        string auth0UserId = authState.User.FindFirst("sub")?.Value!;

        UserDto.Create userDto =
            new()
            {
                Auth0UserId = auth0UserId,
                Firstname = model.Firstname,
                Lastname = model.Lastname,
                PhoneNumber = model.PhoneNumber,
                BirthDay = model.BirthDay,
                Address = new AddressDto
                {
                    Street = model.Street,
                    HouseNumber = model.HouseNumber,
                    UnitNumber = model.UnitNumber,
                    PostalCode = model.PostalCode,
                    City = model.City,
                },
            };

        await UserService.CompleteUserRegistrationAsync(userDto);

        NavigationManager.NavigateTo("/bookings");
    }
}
