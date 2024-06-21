using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AspnetWeb.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUIDToBigInt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
			// 기존 기본 키 삭제
			migrationBuilder.DropPrimaryKey(
				name: "PK_Users",
				table: "Users");

			// UID 열 수정 (예: 타입 변경)
			migrationBuilder.AlterColumn<long>(
                name: "UID",
                table: "Users",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("SqlServer:Identity", "1, 1")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<long>(
                name: "MUID",
                table: "OAuthUsers",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<long>(
                name: "MUID",
                table: "AspnetUsers",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

			// 기본 키 다시 추가
			migrationBuilder.AddPrimaryKey(
				name: "PK_Users",
				table: "Users",
				column: "UID");
		}

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
			// 기본 키 삭제
			migrationBuilder.DropPrimaryKey(
				name: "PK_Users",
				table: "Users");

			// UID 열 원래대로 복원
			migrationBuilder.AlterColumn<int>(
                name: "UID",
                table: "Users",
                type: "int",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .Annotation("SqlServer:Identity", "1, 1")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<int>(
                name: "MUID",
                table: "OAuthUsers",
                type: "int",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<int>(
                name: "MUID",
                table: "AspnetUsers",
                type: "int",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

			// 기본 키 다시 추가
			migrationBuilder.AddPrimaryKey(
				name: "PK_Users",
				table: "Users",
				column: "UID");
		}
    }
}
