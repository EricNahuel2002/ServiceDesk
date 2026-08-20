using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceDesk.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketSlaRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "AuthorId",
                table: "TicketComments",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.CreateTable(
                name: "TicketSlaRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TicketId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TechnicianId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    SlaLimitHours = table.Column<int>(type: "int", nullable: false),
                    ResponseDeadlineAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BreachedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GraceDeadlineUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CanceledAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpiringNotifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BreachedNotifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CanceledNotifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsCurrent = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketSlaRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketSlaRecords_Tickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "Tickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TicketSlaRecords_IsCurrent",
                table: "TicketSlaRecords",
                column: "IsCurrent");

            migrationBuilder.CreateIndex(
                name: "IX_TicketSlaRecords_TechnicianId",
                table: "TicketSlaRecords",
                column: "TechnicianId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketSlaRecords_TicketId",
                table: "TicketSlaRecords",
                column: "TicketId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TicketSlaRecords");

            migrationBuilder.AlterColumn<Guid>(
                name: "AuthorId",
                table: "TicketComments",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);
        }
    }
}
