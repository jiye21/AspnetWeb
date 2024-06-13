using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AspnetWeb.Migrations
{
    /// <inheritdoc />
    public partial class ModifyOAuthTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GoogleEmail",
                table: "OAuthUsers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GoogleEmail",
                table: "OAuthUsers");
        }
    }
}
