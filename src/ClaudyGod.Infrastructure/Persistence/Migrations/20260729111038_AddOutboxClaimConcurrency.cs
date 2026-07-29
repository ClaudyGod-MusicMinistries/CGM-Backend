using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClaudyGod.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboxClaimConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "OutboxMessages",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "xmin",
                table: "OutboxMessages");
        }
    }
}
