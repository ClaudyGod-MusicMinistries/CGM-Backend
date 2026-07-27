using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClaudyGod.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnforceEventCapacityConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Events",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Events_ReservedCount_WithinCapacity",
                table: "Events",
                sql: "\"ReservedCount\" >= 0 AND \"ReservedCount\" <= \"TotalCapacity\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Events_TotalCapacity_NonNegative",
                table: "Events",
                sql: "\"TotalCapacity\" >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Events_ReservedCount_WithinCapacity",
                table: "Events");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Events_TotalCapacity_NonNegative",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Events");
        }
    }
}
