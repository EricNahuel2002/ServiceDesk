using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceDesk.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UseBlobStorageForAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Content",
                table: "TicketAttachments");

            migrationBuilder.RenameColumn(
                name: "BlobUrl",
                table: "TicketAttachments",
                newName: "BlobName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "BlobName",
                table: "TicketAttachments",
                newName: "BlobUrl");

            migrationBuilder.AddColumn<byte[]>(
                name: "Content",
                table: "TicketAttachments",
                type: "varbinary(max)",
                nullable: false,
                defaultValue: new byte[0]);
        }
    }
}
