using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace hotelariaApi.Migrations
{
    /// <inheritdoc />
    public partial class AjusteNparaN : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Funcionalidades_Perfis_PerfilId",
                table: "Funcionalidades");

            migrationBuilder.DropIndex(
                name: "IX_Funcionalidades_PerfilId",
                table: "Funcionalidades");

            migrationBuilder.DropColumn(
                name: "PerfilId",
                table: "Funcionalidades");

            migrationBuilder.CreateTable(
                name: "FuncionalidadePerfil",
                columns: table => new
                {
                    FuncionalidadesId = table.Column<int>(type: "integer", nullable: false),
                    PerfisId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FuncionalidadePerfil", x => new { x.FuncionalidadesId, x.PerfisId });
                    table.ForeignKey(
                        name: "FK_FuncionalidadePerfil_Funcionalidades_FuncionalidadesId",
                        column: x => x.FuncionalidadesId,
                        principalTable: "Funcionalidades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FuncionalidadePerfil_Perfis_PerfisId",
                        column: x => x.PerfisId,
                        principalTable: "Perfis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "FuncionalidadePerfil",
                columns: new[] { "FuncionalidadesId", "PerfisId" },
                values: new object[,]
                {
                    { 1, 1 },
                    { 2, 1 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_FuncionalidadePerfil_PerfisId",
                table: "FuncionalidadePerfil",
                column: "PerfisId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FuncionalidadePerfil");

            migrationBuilder.AddColumn<int>(
                name: "PerfilId",
                table: "Funcionalidades",
                type: "integer",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Funcionalidades",
                keyColumn: "Id",
                keyValue: 1,
                column: "PerfilId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Funcionalidades",
                keyColumn: "Id",
                keyValue: 2,
                column: "PerfilId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Funcionalidades",
                keyColumn: "Id",
                keyValue: 3,
                column: "PerfilId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Funcionalidades",
                keyColumn: "Id",
                keyValue: 4,
                column: "PerfilId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Funcionalidades",
                keyColumn: "Id",
                keyValue: 5,
                column: "PerfilId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Funcionalidades",
                keyColumn: "Id",
                keyValue: 6,
                column: "PerfilId",
                value: null);

            migrationBuilder.CreateIndex(
                name: "IX_Funcionalidades_PerfilId",
                table: "Funcionalidades",
                column: "PerfilId");

            migrationBuilder.AddForeignKey(
                name: "FK_Funcionalidades_Perfis_PerfilId",
                table: "Funcionalidades",
                column: "PerfilId",
                principalTable: "Perfis",
                principalColumn: "Id");
        }
    }
}
