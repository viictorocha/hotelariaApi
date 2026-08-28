using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace hotelariaApi.Migrations
{
    /// <inheritdoc />
    public partial class MultiPousada : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Usuarios_Perfis_PerfilId",
                table: "Usuarios");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Pousada",
                table: "Pousada");

            migrationBuilder.RenameTable(
                name: "Pousada",
                newName: "Pousadas");

            migrationBuilder.RenameColumn(
                name: "PerfilId",
                table: "Usuarios",
                newName: "ContaId");

            migrationBuilder.RenameIndex(
                name: "IX_Usuarios_PerfilId",
                table: "Usuarios",
                newName: "IX_Usuarios_ContaId");

            migrationBuilder.AddColumn<bool>(
                name: "EhDono",
                table: "Usuarios",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "PousadaId",
                table: "Quartos",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ContaId",
                table: "Pousadas",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Pousadas",
                table: "Pousadas",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "Contas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UsuariosPousadas",
                columns: table => new
                {
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    PousadaId = table.Column<int>(type: "integer", nullable: false),
                    PerfilId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuariosPousadas", x => new { x.UsuarioId, x.PousadaId });
                    table.ForeignKey(
                        name: "FK_UsuariosPousadas_Perfis_PerfilId",
                        column: x => x.PerfilId,
                        principalTable: "Perfis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UsuariosPousadas_Pousadas_PousadaId",
                        column: x => x.PousadaId,
                        principalTable: "Pousadas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UsuariosPousadas_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Quartos_PousadaId",
                table: "Quartos",
                column: "PousadaId");

            migrationBuilder.CreateIndex(
                name: "IX_Pousadas_ContaId",
                table: "Pousadas",
                column: "ContaId");

            migrationBuilder.CreateIndex(
                name: "IX_UsuariosPousadas_PerfilId",
                table: "UsuariosPousadas",
                column: "PerfilId");

            migrationBuilder.CreateIndex(
                name: "IX_UsuariosPousadas_PousadaId",
                table: "UsuariosPousadas",
                column: "PousadaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Pousadas_Contas_ContaId",
                table: "Pousadas",
                column: "ContaId",
                principalTable: "Contas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Quartos_Pousadas_PousadaId",
                table: "Quartos",
                column: "PousadaId",
                principalTable: "Pousadas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Usuarios_Contas_ContaId",
                table: "Usuarios",
                column: "ContaId",
                principalTable: "Contas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Pousadas_Contas_ContaId",
                table: "Pousadas");

            migrationBuilder.DropForeignKey(
                name: "FK_Quartos_Pousadas_PousadaId",
                table: "Quartos");

            migrationBuilder.DropForeignKey(
                name: "FK_Usuarios_Contas_ContaId",
                table: "Usuarios");

            migrationBuilder.DropTable(
                name: "Contas");

            migrationBuilder.DropTable(
                name: "UsuariosPousadas");

            migrationBuilder.DropIndex(
                name: "IX_Quartos_PousadaId",
                table: "Quartos");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Pousadas",
                table: "Pousadas");

            migrationBuilder.DropIndex(
                name: "IX_Pousadas_ContaId",
                table: "Pousadas");

            migrationBuilder.DropColumn(
                name: "EhDono",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "PousadaId",
                table: "Quartos");

            migrationBuilder.DropColumn(
                name: "ContaId",
                table: "Pousadas");

            migrationBuilder.RenameTable(
                name: "Pousadas",
                newName: "Pousada");

            migrationBuilder.RenameColumn(
                name: "ContaId",
                table: "Usuarios",
                newName: "PerfilId");

            migrationBuilder.RenameIndex(
                name: "IX_Usuarios_ContaId",
                table: "Usuarios",
                newName: "IX_Usuarios_PerfilId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Pousada",
                table: "Pousada",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Usuarios_Perfis_PerfilId",
                table: "Usuarios",
                column: "PerfilId",
                principalTable: "Perfis",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
