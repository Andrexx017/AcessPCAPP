using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AppParque.Api.Migrations
{
    /// <inheritdoc />
    public partial class AgregarDiagnosticosGuardados : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DiagnosticosGuardados",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VisitanteId = table.Column<int>(type: "integer", nullable: false),
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PerfilJson = table.Column<string>(type: "text", nullable: false),
                    CondicionAdicional = table.Column<string>(type: "text", nullable: true),
                    ResultadosJson = table.Column<string>(type: "text", nullable: false),
                    Permitidas = table.Column<int>(type: "integer", nullable: false),
                    ConMitigacion = table.Column<int>(type: "integer", nullable: false),
                    PorVerificar = table.Column<int>(type: "integer", nullable: false),
                    Restringidas = table.Column<int>(type: "integer", nullable: false),
                    RespuestaIATexto = table.Column<string>(type: "text", nullable: true),
                    RespuestaIAModelo = table.Column<string>(type: "text", nullable: true),
                    RespuestaIASesionId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiagnosticosGuardados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiagnosticosGuardados_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DiagnosticosGuardados_Visitantes_VisitanteId",
                        column: x => x.VisitanteId,
                        principalTable: "Visitantes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DiagnosticosGuardados_Fecha",
                table: "DiagnosticosGuardados",
                column: "Fecha");

            migrationBuilder.CreateIndex(
                name: "IX_DiagnosticosGuardados_UsuarioId",
                table: "DiagnosticosGuardados",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_DiagnosticosGuardados_VisitanteId",
                table: "DiagnosticosGuardados",
                column: "VisitanteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DiagnosticosGuardados");
        }
    }
}
