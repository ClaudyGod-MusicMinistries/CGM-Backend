using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClaudyGod.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeBookingCountryCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The former enum used UK, but ISO 3166-1 alpha-2 uses GB.
            migrationBuilder.Sql("UPDATE \"Bookings\" SET \"CountryCode\" = 'GB' WHERE \"CountryCode\" = 'UK';");

            migrationBuilder.AlterColumn<string>(
                name: "CountryCode",
                table: "Bookings",
                type: "character varying(2)",
                maxLength: 2,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE \"Bookings\" SET \"CountryCode\" = 'UK' WHERE \"CountryCode\" = 'GB';");

            migrationBuilder.AlterColumn<string>(
                name: "CountryCode",
                table: "Bookings",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(2)",
                oldMaxLength: 2);
        }
    }
}
