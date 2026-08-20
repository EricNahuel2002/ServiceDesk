using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceDesk.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAssignmentStartGraceRule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AdminReassignmentNotifiedAtUtc",
                table: "TicketSlaRecords",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CanceledReason",
                table: "TicketSlaRecords",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TicketSlaRecords_CanceledReason",
                table: "TicketSlaRecords",
                column: "CanceledReason");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TicketSlaRecords_CanceledReason",
                table: "TicketSlaRecords");

            migrationBuilder.DropColumn(
                name: "AdminReassignmentNotifiedAtUtc",
                table: "TicketSlaRecords");

            migrationBuilder.DropColumn(
                name: "CanceledReason",
                table: "TicketSlaRecords");
        }
    }
}
