using Microsoft.EntityFrameworkCore.Migrations;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
public partial class CreateInitialSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Address Table
        migrationBuilder.CreateTable(
            name: "Address",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                Street = table.Column<string>(maxLength: 100, nullable: false),
                HouseNumber = table.Column<string>(maxLength: 10, nullable: false),
                UnitNumber = table.Column<string>(maxLength: 10, nullable: true),
                City = table.Column<string>(maxLength: 50, nullable: false),
                PostalCode = table.Column<string>(maxLength: 10, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Address", x => x.Id);
            });

        // User Table
        migrationBuilder.CreateTable(
            name: "User",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                Auth0UserId = table.Column<string>(maxLength: 100, nullable: false),
                FirstName = table.Column<string>(maxLength: 50, nullable: false),
                LastName = table.Column<string>(maxLength: 50, nullable: false),
                BirthDay = table.Column<DateTime>(nullable: false),
                Email = table.Column<string>(maxLength: 100, nullable: false),
                PhoneNumber = table.Column<string>(maxLength: 15, nullable: true),
                AddressId = table.Column<int>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_User", x => x.Id);
                table.ForeignKey(
                    name: "FK_User_Address_AddressId",
                    column: x => x.AddressId,
                    principalTable: "Address",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        // Battery Table
        migrationBuilder.CreateTable(
            name: "Battery",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                Name = table.Column<string>(maxLength: 50, nullable: false),
                Status = table.Column<string>(maxLength: 20, nullable: false),
                AddressId = table.Column<int>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Battery", x => x.Id);
                table.ForeignKey(
                    name: "FK_Battery_Address_AddressId",
                    column: x => x.AddressId,
                    principalTable: "Address",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        // Boat Table
        migrationBuilder.CreateTable(
            name: "Boat",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                Name = table.Column<string>(maxLength: 50, nullable: false),
                Status = table.Column<string>(maxLength: 20, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Boat", x => x.Id);
            });

        // Timeslot Table
        migrationBuilder.CreateTable(
            name: "Timeslot",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                Date = table.Column<DateTime>(nullable: false),
                Type = table.Column<string>(maxLength: 20, nullable: false),
                Reason = table.Column<string>(maxLength: 100, nullable: true),
                CreatedByUserId = table.Column<int>(nullable: false),
                CreatedAt = table.Column<DateTime>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Timeslot", x => x.Id);
                table.ForeignKey(
                    name: "FK_Timeslot_User_CreatedByUserId",
                    column: x => x.CreatedByUserId,
                    principalTable: "User",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        // Booking Table
        migrationBuilder.CreateTable(
            name: "Booking",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                BoatId = table.Column<int>(nullable: false),
                BatteryId = table.Column<int>(nullable: false),
                UserId = table.Column<int>(nullable: false),
                DateTime = table.Column<DateTime>(nullable: false),
                Status = table.Column<string>(maxLength: 20, nullable: false),
                Remark = table.Column<string>(maxLength: 200, nullable: true),
                PriceId = table.Column<int>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Booking", x => x.Id);
                table.ForeignKey(
                    name: "FK_Booking_Boat_BoatId",
                    column: x => x.BoatId,
                    principalTable: "Boat",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_Booking_Battery_BatteryId",
                    column: x => x.BatteryId,
                    principalTable: "Battery",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_Booking_User_UserId",
                    column: x => x.UserId,
                    principalTable: "User",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        // Price Table
        migrationBuilder.CreateTable(
            name: "Price",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                CreatedAt = table.Column<DateTime>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Price", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_User_AddressId",
            table: "User",
            column: "AddressId");

        migrationBuilder.CreateIndex(
            name: "IX_Battery_AddressId",
            table: "Battery",
            column: "AddressId");

        migrationBuilder.CreateIndex(
            name: "IX_Timeslot_CreatedByUserId",
            table: "Timeslot",
            column: "CreatedByUserId");

        migrationBuilder.CreateIndex(
            name: "IX_Booking_BoatId",
            table: "Booking",
            column: "BoatId");

        migrationBuilder.CreateIndex(
            name: "IX_Booking_BatteryId",
            table: "Booking",
            column: "BatteryId");

        migrationBuilder.CreateIndex(
            name: "IX_Booking_UserId",
            table: "Booking",
            column: "UserId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Booking");
        migrationBuilder.DropTable(name: "Timeslot");
        migrationBuilder.DropTable(name: "Price");
        migrationBuilder.DropTable(name: "Boat");
        migrationBuilder.DropTable(name: "Battery");
        migrationBuilder.DropTable(name: "User");
        migrationBuilder.DropTable(name: "Address");
    }
}
