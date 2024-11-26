using System.Reflection;
using System.Security.Claims;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Rise.Persistence;
using Rise.Persistence.Triggers;
using Rise.Server.Middleware;
using Rise.Services.Batteries;
using Rise.Services.Batteries.Services;
using Rise.Services.Boats;
using Rise.Services.Bookings;
using Rise.Services.Emails;
using Rise.Services.Prices;
using Rise.Services.TimeSlots;
using Rise.Services.Users;
using Rise.Shared.Batteries;
using Rise.Shared.Boats;
using Rise.Shared.Bookings;
using Rise.Shared.Emails;
using Rise.Shared.Prices;
using Rise.Shared.TimeSlots;
using Rise.Shared.Users;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5000); // Bind to 5000
    options.ListenAnyIP(5001); // Bind to 5001
    options.ListenAnyIP(5002); // Bind to 5002
    options.ListenAnyIP(5003);
});

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Load environment variables from a .env file if it exists
Env.Load();

// Add environment variables to the configuration
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddControllers();
builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
    // Aangepaste schema ID configuratie om unieke namen te genereren
    c.CustomSchemaIds(type => type.FullName.Replace("+", "."));
});

//exceptionHandlers
builder.Services.AddExceptionHandler<UnauthorizedAccessExceptionHandler>();
builder.Services.AddExceptionHandler<BadRequestExceptionHandler>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder
    .Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Auth0:Authority"];
        options.Audience = builder.Configuration["Auth0:Audience"];
        options.TokenValidationParameters = new TokenValidationParameters
        {
            NameClaimType = ClaimTypes.NameIdentifier,
        };
    });

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer"));
    options.EnableDetailedErrors();
    options.EnableSensitiveDataLogging();
    options.UseTriggers(options => options.AddTrigger<EntityBeforeSaveTrigger>());
});

builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IBoatService, BoatService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITimeSlotService, TimeSlotService>();
builder.Services.AddScoped<IPriceService, PriceService>();

// custom authorization handler
builder.Services.AddSingleton<IAuthorizationHandler, OwnDataOrAdminHandler>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(
        "OwnDataOrAdmin",
        policy => policy.Requirements.Add(new OwnDataOrAdminRequirement())
    );
});
builder.Services.AddScoped<IBatteryService, BatteryService>();

builder.Services.AddScoped<IBatteryAndBoatAssignmentProcessor, BatteryAndBoatAssignmentProcessor>();
builder.Services.AddHostedService<BatteryAndBoatAssignmentService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddRazorPages();
builder.Services.AddControllersWithViews();

builder.Services.AddScoped<IEmailTemplateService, EmailTemplateService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.MapControllers();
app.MapFallbackToFile("index.html");

app.UseExceptionHandler();

/*Seeder*/
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    using (var scope = app.Services.CreateScope())
    { // Require a DbContext from the service provider and seed the database.
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Seeder seeder = new(dbContext);
        seeder.Seed();
    }
}

// This enables all authorization on every endpoint, so no more use without authorization
app.MapControllers().RequireAuthorization();

app.MapHealthChecks("/healthcheck");
app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.Logger.LogInformation("Starting application");
await app.RunAsync(); //  SonarFeedback csharpsquid:S6966

// for Integration tests
public partial class Program { }
