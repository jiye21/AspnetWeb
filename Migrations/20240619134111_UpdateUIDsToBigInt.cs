using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AspnetWeb.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUIDsToBigInt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
			// 기존 기본 키 삭제
			migrationBuilder.DropPrimaryKey(
				name: "PK_OAuthUsers",
				table: "OAuthUsers");

			migrationBuilder.DropPrimaryKey(
				name: "PK_AspnetUsers",
				table: "AspnetUsers");

			migrationBuilder.AlterColumn<long>(
                name: "UID",
                table: "OAuthUsers",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("SqlServer:Identity", "1, 1")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<long>(
                name: "UID",
                table: "AspnetUsers",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("SqlServer:Identity", "1, 1")
                .OldAnnotation("SqlServer:Identity", "1, 1");

			// 기본 키 다시 추가
			migrationBuilder.AddPrimaryKey(
				name: "PK_OAuthUsers",
				table: "OAuthUsers",
				column: "UID");

			migrationBuilder.AddPrimaryKey(
				name: "PK_AspnetUsers",
				table: "AspnetUsers",
				column: "UID");
		}

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
			migrationBuilder.DropPrimaryKey(
				name: "PK_OAuthUsers",
				table: "OAuthUsers");

			migrationBuilder.DropPrimaryKey(
				name: "PK_AspnetUsers",
				table: "AspnetUsers");

			migrationBuilder.AlterColumn<int>(
                name: "UID",
                table: "OAuthUsers",
                type: "int",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .Annotation("SqlServer:Identity", "1, 1")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<int>(
                name: "UID",
                table: "AspnetUsers",
                type: "int",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .Annotation("SqlServer:Identity", "1, 1")
                .OldAnnotation("SqlServer:Identity", "1, 1");

			migrationBuilder.AddPrimaryKey(
				name: "PK_OAuthUsers",
				table: "OAuthUsers",
				column: "UID");

			migrationBuilder.AddPrimaryKey(
				name: "PK_AspnetUsers",
				table: "AspnetUsers",
				column: "UID");
		}
    }
}
