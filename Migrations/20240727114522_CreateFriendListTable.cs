using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AspnetWeb.Migrations
{
    /// <inheritdoc />
    public partial class CreateFriendListTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FriendList",
                columns: table => new
                {
                    UID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FriendName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FriendMUID = table.Column<long>(type: "bigint", nullable: false),
                    HeartCount = table.Column<int>(type: "int", nullable: false),
                    MUID = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FriendList", x => x.UID);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FriendList");
        }
    }
}
