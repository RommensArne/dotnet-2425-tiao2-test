using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Auth0.AuthenticationApi;
using Auth0.AuthenticationApi.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Rise.Domain.Boats;
using Rise.Persistence;
using Rise.Server.IntegrationTests.Utils;
using Rise.Shared.Boats;
using Rise.Shared.Users;
using Shouldly;
using Xunit;

namespace Rise.Server.IntegrationTests;

[Collection("Sequential Test Collection")]
public class BoatControllersTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private static HttpClient _client;
    private static string _adminToken;
    private static string _userToken;
    private readonly IConfigurationSection _auth0Settings;
    private readonly CustomWebApplicationFactory<Program> _factory;

    public BoatControllersTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        ResetDatabaseWithSeed();

        if (_client == null)
        {
            factory.ClientOptions.BaseAddress = new Uri("https://localhost:5001/api/");
            _client = factory.CreateClient();
        }
        _auth0Settings = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build()
            .GetSection("Auth0");
    }

    private async Task<string> GetTokenAsync(Role role)
    {
        //tijdelijke zelgemaakte tokens tijdens schrijven van testen
        // _userToken =
        //      "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCIsImtpZCI6IjBVekktMHIzZk1KT3VGT2ctNU5YVCJ9.eyJodHRwOi8vc2NoZW1hcy5taWNyb3NvZnQuY29tL3dzLzIwMDgvMDYvaWRlbnRpdHkvY2xhaW1zL3JvbGUiOlsiVXNlciJdLCJpc3MiOiJodHRwczovL2J1dXQuZXUuYXV0aDAuY29tLyIsInN1YiI6ImF1dGgwfDY3MjExZmQzNzlhNzZjMjdjOTUxYzcwMSIsImF1ZCI6WyJodHRwczovL2FwaS5idXV0LmJlIiwiaHR0cHM6Ly9idXV0LmV1LmF1dGgwLmNvbS91c2VyaW5mbyJdLCJpYXQiOjE3MzE4ODU3NjIsImV4cCI6MTczMTk3MjE2Miwic2NvcGUiOiJvcGVuaWQgcHJvZmlsZSIsImF6cCI6InViOWdUOVprTDVsbVloVDJNQ1ZkVHFHS1pXRjR6ZXA5In0.bocqR3w0p0dctR5j-bl1GrpD6HEhoovf2_DubhbqkqdzwktP52gmleaqAK3McuEihnappTgCL65VUoJlQwQ7DVhbXdSjT9EdKX8c625Ho0Kj60FgCILQCH2oiucossZ6xb1kQKaiuWDLYYC-2OR1QbatVRkA2os4RScW5_cUmUMegqrV5E2MeVrp5SHMqSrTbRSoWfyKBwAYSFqREJTJ4h-CV6NVpjxACWrmz1d1BCmsvAUmc_co9u6el9oSdesasCcrWnmW3R3ljFLwtfgYqyPB2I2ebfsr6TCWwPRD9EA1XQ_q45afbvqEptL_6J3vNhS2Vyo8IdFAEaineKA_mQ";
        // _adminToken =
        //     "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCIsImtpZCI6IjBVekktMHIzZk1KT3VGT2ctNU5YVCJ9.eyJodHRwOi8vc2NoZW1hcy5taWNyb3NvZnQuY29tL3dzLzIwMDgvMDYvaWRlbnRpdHkvY2xhaW1zL3JvbGUiOlsiQWRtaW5pc3RyYXRvciJdLCJpc3MiOiJodHRwczovL2J1dXQuZXUuYXV0aDAuY29tLyIsInN1YiI6ImF1dGgwfDY3MjExZjY3NWNlZTMxNTNkZjU1OGRlZiIsImF1ZCI6WyJodHRwczovL2FwaS5idXV0LmJlIiwiaHR0cHM6Ly9idXV0LmV1LmF1dGgwLmNvbS91c2VyaW5mbyJdLCJpYXQiOjE3MzE4ODU2OTcsImV4cCI6MTczMTk3MjA5Nywic2NvcGUiOiJvcGVuaWQgcHJvZmlsZSIsImF6cCI6InViOWdUOVprTDVsbVloVDJNQ1ZkVHFHS1pXRjR6ZXA5In0.g0nSHLKNbOwYQspvUcA_jLR1UKL4RGy1MMjQdky1hHXNmGBX38cYKimmoobZskBWbg0V5r9zDNdzFSUVbJaFyHYndUym4uxv_h-rNStZ5-D2cs1CaQ0XtX7NE9c_XRg98_4ubw328FFfreiGFnKCfDMWSxxBq39eQ4uCOnbU4_g_ac-N5g0OFNT4Bf0kKEjOheGRBDtf5m5Wz9EpXbSQKOuVeS4ElA2vU7NfHAXLsPtleTOD7Hg8hxdBVRoXSi8-yRwkwuVCxVfZcDNN8IIL0CTSwrvEy1Pfgr1G9jfzPhK7RIJ1LNuDvZf0ExeV6CrjwaOjc7QSJtXs2xsfYPLe_A";
        // Check if the token is already retrieved and cached
        if (role == Role.ADMIN && !string.IsNullOrEmpty(_adminToken))
        {
            return _adminToken;
        }
        else if (role == Role.USER && !string.IsNullOrEmpty(_userToken))
        {
            return _userToken;
        }

        // Otherwise, fetch a new token and cache it
        var auth0Client = new AuthenticationApiClient(_auth0Settings["Domain"]);
        var tokenRequest = new ResourceOwnerTokenRequest
        {
            ClientId = _auth0Settings["ClientId"],
            ClientSecret = _auth0Settings["ClientSecret"],
            Audience = _auth0Settings["Audience"],
            Username = role == Role.ADMIN ? TestData.AdminEmail : TestData.UserEmail,
            Password = role == Role.ADMIN ? TestData.AdminPassword : TestData.UserPassword,
            Scope = "openid profile email roles",
        };

        var tokenResponse = await auth0Client.GetTokenAsync(tokenRequest);
        string token = tokenResponse.AccessToken;

        // Cache the token for other tests, so we don't have to request a new token for each test
        if (role == Role.ADMIN)
        {
            _adminToken = token;
        }
        else
        {
            _userToken = token;
        }

        return token;
    }

    private async Task SetAuthorizationHeader(Role role)
    {
        string token = await GetTokenAsync(role);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            token
        );
    }

    private void ResetDatabaseWithSeed()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Verwijder de database
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            // Voeg de seeddata toe
            var seeder = new Seeder(db);
            seeder.Seed();
        }
    }

    private void ResetDatabaseWithoutSeed()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Verwijder de database
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();
        }
    }

    [Fact]
    public async Task GetAllBoatsAsAdmin_ReturnsAllBoats()
    {
        //Arrange
        await SetAuthorizationHeader(Role.ADMIN);

        //Act
        var response = await _client.GetAsync("boat");
        var boatList = await response.Content.ReadFromJsonAsync<IEnumerable<BoatDto.BoatIndex>>();

        //Assert
        response.EnsureSuccessStatusCode();
        Assert.NotNull(response);
        Assert.NotEmpty(boatList);
        boatList.Should().HaveCount(3);
        boatList.Should().Contain(b => b.Name == TestData.BoatName1);
        boatList.Should().Contain(b => b.Name == TestData.BoatName2);
        boatList.Should().Contain(b => b.Name == TestData.BoatName3);
    }

    [Fact]
    public async Task GetAllBoatsAsUser_ReturnsAllBoats()
    {
        //Arrange
        await SetAuthorizationHeader(Role.USER);

        //Act
        var response = await _client.GetAsync("boat");
        var boatList = await response.Content.ReadFromJsonAsync<IEnumerable<BoatDto.BoatIndex>>();

        //Assert
        response.EnsureSuccessStatusCode();
        Assert.NotNull(response);
        Assert.NotEmpty(boatList);
        boatList.Should().HaveCount(3);
        boatList.Should().Contain(b => b.Name == TestData.BoatName1);
        boatList.Should().Contain(b => b.Name == TestData.BoatName2);
        boatList.Should().Contain(b => b.Name == TestData.BoatName3);
    }

    [Fact]
    public async Task GetAllBoatsAsUser_NoBoats_ReturnsNoFound()
    {
        //Arrange
        await SetAuthorizationHeader(Role.USER);
        ResetDatabaseWithoutSeed();

        //Act
        var response = await _client.GetAsync("boat");
        //Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateNewBoatAsAdmin_ValidBoat_ReturnsCreatedBoat()
    {
        //Arrange
        await SetAuthorizationHeader(Role.ADMIN);
        var createDto = new BoatDto.CreateBoatDto
        {
            Name = "New Boat",
            Status = BoatDto.BoatStatus.Available,
        };

        //Act
        var response = await _client.PostAsJsonAsync("boat", createDto);
        var createdBoat = await response.Content.ReadFromJsonAsync<BoatDto.BoatIndex>();

        //Assert
        response.EnsureSuccessStatusCode();
        Assert.NotNull(createdBoat);
        Assert.Equal(createDto.Name, createdBoat.Name);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var newBoat = db.Boats.Find(createdBoat.Id);
            newBoat.Name.ShouldBe(createDto.Name);
            newBoat.Status.ShouldBe(Boat.BoatStatus.Available);
        }
    }

    [Fact]
    public async Task CreateNewBoatAsAdmin_DuplicateName_BadRequest()
    {
        //Arrange
        await SetAuthorizationHeader(Role.ADMIN);
        var createDto = new BoatDto.CreateBoatDto
        {
            Name = TestData.BoatName1,
            Status = BoatDto.BoatStatus.Available,
        };

        //Act
        var response = await _client.PostAsJsonAsync("boat", createDto);

        //Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateNewBoatAsUser_ValidBoat_Forbidden()
    {
        //Arrange
        await SetAuthorizationHeader(Role.USER);
        var createDto = new BoatDto.CreateBoatDto
        {
            Name = "New Boat",
            Status = BoatDto.BoatStatus.Available,
        };

        //Act
        var response = await _client.PostAsJsonAsync("boat", createDto);

        //Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetAvailableBoatsCountAsyncAsAdmin_ReturnsAvailableBoatsCount()
    {
        //Arrange
        await SetAuthorizationHeader(Role.ADMIN);

        //Act
        var response = await _client.GetAsync("boat/available/count");
        var count = await response.Content.ReadFromJsonAsync<int>();

        //Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task GetAvailableBoatsCountAsyncAsUser_ReturnsAvailableBoatsCount()
    {
        //Arrange
        await SetAuthorizationHeader(Role.USER);

        //Act
        var response = await _client.GetAsync("boat/available/count");
        var count = await response.Content.ReadFromJsonAsync<int>();

        //Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task GetBoatByIdAsyncAsAdmin_ExistingBoatId_ReturnsBoat()
    {
        //Arrange
        await SetAuthorizationHeader(Role.ADMIN);

        //Act
        var response = await _client.GetAsync($"boat/{Seeder.BoatId1}");
        var result = await response.Content.ReadFromJsonAsync<BoatDto.BoatIndex>();

        //Assert
        response.EnsureSuccessStatusCode();
        Assert.NotNull(result);
        Assert.Equal(Seeder.BoatId1, result.Id);
        Assert.Equal(TestData.BoatName1, result.Name);
    }

    [Fact]
    public async Task GetBoatByIdAsyncAsAdmin_NonExistingBoatId_NotFound()
    {
        //Arrange
        await SetAuthorizationHeader(Role.ADMIN);

        //Act
        var response = await _client.GetAsync($"boat/{999}");

        //Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetBoatByIdAsyncAsUser_ExistingBoatId_ReturnsBoat()
    {
        //Arrange
        await SetAuthorizationHeader(Role.USER);

        //Act
        var response = await _client.GetAsync($"boat/{Seeder.BoatId1}");
        var result = await response.Content.ReadFromJsonAsync<BoatDto.BoatIndex>();

        //Assert
        response.EnsureSuccessStatusCode();
        Assert.NotNull(result);
        Assert.Equal(Seeder.BoatId1, result.Id);
        Assert.Equal(TestData.BoatName1, result.Name);
    }

    [Fact]
    public async Task UpdateBoatStatusAsyncAsAdmin_ExistingId_ReturnsBoat()
    {
        //Arrange
        await SetAuthorizationHeader(Role.ADMIN);
        var status = BoatDto.BoatStatus.InRepair;

        //Act
        var response = await _client.PutAsJsonAsync($"boat/{Seeder.BoatId1}", status);
        var result = await response.Content.ReadFromJsonAsync<BoatDto.BoatIndex>();

        //Assert
        response.EnsureSuccessStatusCode();
        Assert.NotNull(result);
        Assert.Equal(Seeder.BoatId1, result.Id);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var updatedBoat = db.Boats.Find(Seeder.BoatId1);
            updatedBoat.Status.ShouldBe(Boat.BoatStatus.InRepair);
        }
    }

    [Fact]
    public async Task UpdateBoatStatusAsyncAsAdmin_NonExistingId_NotFound()
    {
        //Arrange
        await SetAuthorizationHeader(Role.ADMIN);
        var status = BoatDto.BoatStatus.InRepair;

        //Act
        var response = await _client.PutAsJsonAsync($"boat/{999}", status);

        //Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateBoatStatusAsyncAsUser_ExistingId_Forbidden()
    {
        //Arrange
        await SetAuthorizationHeader(Role.USER);
        var status = BoatDto.BoatStatus.InRepair;

        //Act
        var response = await _client.PutAsJsonAsync($"boat/{Seeder.BoatId1}", status);

        //Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteBoatAsAdmin_ExistingId_ReturnsNoContent()
    {
        //Arrange
        await SetAuthorizationHeader(Role.ADMIN);

        //Act
        var response = await _client.DeleteAsync($"boat/{Seeder.BoatId1}");

        //Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var deletedBoat = db.Boats.Find(Seeder.BoatId1);
            Assert.True(deletedBoat.IsDeleted);
        }
    }

    [Fact]
    public async Task DeleteBoatAsAdmin_NonExistingId_NotFound()
    {
        //Arrange
        await SetAuthorizationHeader(Role.ADMIN);

        //Act
        var response = await _client.DeleteAsync($"boat/{9999}");

        //Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteBoatAsUser_ExistingId_Forbidden()
    {
        //Arrange
        await SetAuthorizationHeader(Role.USER);

        //Act
        var response = await _client.DeleteAsync($"boat/{Seeder.BoatId1}");

        //Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
