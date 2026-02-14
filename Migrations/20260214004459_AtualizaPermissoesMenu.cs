using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace hotelariaApi.Migrations
{
    /// <inheritdoc />
    public partial class AtualizaPermissoesMenu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Funcionalidades",
                columns: new[] { "Id", "Descricao", "Nome", "PerfilId" },
                values: new object[,]
                {
                    { 1, "Visualizar indicadores e métricas", "DASHBOARD", null },
                    { 2, "Visualizar status e gerenciar quartos", "MAPA_QUARTOS", null },
                    { 3, "Gerenciar lista e detalhes de reservas", "RESERVAS", null },
                    { 4, "Lançar e gerenciar itens de consumo", "CONSUMO", null },
                    { 5, "Acesso a contas e faturamento", "FINANCEIRO", null },
                    { 6, "Acesso a configurações do sistema e perfis", "CONFIGURACOES", null }
                });

            migrationBuilder.InsertData(
                table: "Perfis",
                columns: new[] { "Id", "Nome" },
                values: new object[] { 1, "Admin" });

            migrationBuilder.InsertData(
                table: "Usuarios",
                columns: new[] { "Id", "Email", "PerfilId", "SenhaHash" },
                values: new object[] { 1, "admin@hotel.com", 1, "123456" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Funcionalidades",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Funcionalidades",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Funcionalidades",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Funcionalidades",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Funcionalidades",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Funcionalidades",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Perfis",
                keyColumn: "Id",
                keyValue: 1);
        }
    }
}
