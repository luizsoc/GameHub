using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDirectMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DirectMessageKey",
                table: "Channels",
                type: "character varying(65)",
                maxLength: 65,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Channels_DirectMessageKey",
                table: "Channels",
                column: "DirectMessageKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Channels_DirectMessageKey",
                table: "Channels");

            migrationBuilder.DropColumn(
                name: "DirectMessageKey",
                table: "Channels");
        }
    }
}
