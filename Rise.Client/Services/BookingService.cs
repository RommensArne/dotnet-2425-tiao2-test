using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Rise.Shared.Bookings;

namespace Rise.Client.Bookings;

public class BookingService(HttpClient httpClient) : IBookingService
{
    private readonly HttpClient _httpClient = httpClient;
    private const string endpoint = "booking";

    public async Task<IEnumerable<BookingDto.Index>?> GetAllBookingsAsync()
    {
        var response = await _httpClient.GetAsync("booking");

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<IEnumerable<BookingDto.Index>>();
    }

    public async Task<IEnumerable<BookingDto.Index>?> GetBookingsByUserIdAsync(int userId)
    {
        var response = await _httpClient.GetAsync($"booking/user/{userId}");
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<IEnumerable<BookingDto.Index>>();
    }

    public async Task<IEnumerable<BookingDto.Index>?> GetAllCurrentBookingsAsync()
    {
        var response = await _httpClient.GetAsync($"{endpoint}/current");
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<IEnumerable<BookingDto.Index>>();
    }

    public async Task<int> CreateBookingAsync(BookingDto.Mutate model)
    {
        model.Status = BookingDto.BookingStatus.Active;

        var response = await _httpClient.PostAsJsonAsync(endpoint, model);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<int>();
    }

    public async Task<BookingDto.Detail?> GetBookingByIdAsync(int bookingId)
    {
        var response = await _httpClient.GetAsync($"{endpoint}/{bookingId}");
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<BookingDto.Detail>();
    }

    public async Task<bool> UpdateBookingAsync(int bookingId, BookingDto.Mutate model)
    {
        var response = await _httpClient.PutAsJsonAsync($"{endpoint}/{bookingId}", model);
        response.EnsureSuccessStatusCode();
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteBookingAsync(int bookingId)
    {
        var response = await _httpClient.DeleteAsync($"{endpoint}/{bookingId}");
        response.EnsureSuccessStatusCode();
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> CancelBookingAsync(int bookingId)
    {
        var response = await _httpClient.PutAsync($"{endpoint}/cancel/{bookingId}", null);
        response.EnsureSuccessStatusCode();
        return response.IsSuccessStatusCode;
    }
}
