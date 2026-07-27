using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClaudyGod.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveAccidentalShadowForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BlogPostTags_BlogTags_BlogTagId1",
                table: "BlogPostTags");

            migrationBuilder.DropForeignKey(
                name: "FK_TicketReservations_Events_EventId1",
                table: "TicketReservations");

            migrationBuilder.DropIndex(
                name: "IX_TicketReservations_EventId1",
                table: "TicketReservations");

            migrationBuilder.DropIndex(
                name: "IX_BlogPostTags_BlogTagId1",
                table: "BlogPostTags");

            migrationBuilder.DropColumn(
                name: "EventId1",
                table: "TicketReservations");

            migrationBuilder.DropColumn(
                name: "BlogTagId1",
                table: "BlogPostTags");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "EventId1",
                table: "TicketReservations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BlogTagId1",
                table: "BlogPostTags",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TicketReservations_EventId1",
                table: "TicketReservations",
                column: "EventId1");

            migrationBuilder.CreateIndex(
                name: "IX_BlogPostTags_BlogTagId1",
                table: "BlogPostTags",
                column: "BlogTagId1");

            migrationBuilder.AddForeignKey(
                name: "FK_BlogPostTags_BlogTags_BlogTagId1",
                table: "BlogPostTags",
                column: "BlogTagId1",
                principalTable: "BlogTags",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TicketReservations_Events_EventId1",
                table: "TicketReservations",
                column: "EventId1",
                principalTable: "Events",
                principalColumn: "Id");
        }
    }
}
