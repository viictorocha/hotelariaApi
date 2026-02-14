using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace hotelariaApi.Migrations
{
    /// <inheritdoc />
    public partial class FixStaticSeedHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Nome",
                table: "Usuarios",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Nome", "SenhaHash" },
                values: new object[] { "", "$2a$11$ev6SvHDrS6vTTe9BUn7m9.SXYpT.pOfm4YshO.S.zJv8.M9m7UuO2" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Nome",
                table: "Usuarios");

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 1,
                column: "SenhaHash",
                value: "123456");
        }
    }
}
