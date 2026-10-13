using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AppParque.Api.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCatalogoZonas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ZonasCorporales",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    Corto = table.Column<string>(type: "text", nullable: false),
                    Nota = table.Column<string>(type: "text", nullable: false),
                    ResumenSinNovedad = table.Column<string>(type: "text", nullable: false),
                    EnCuerpo = table.Column<bool>(type: "boolean", nullable: false),
                    PermiteSinNovedad = table.Column<bool>(type: "boolean", nullable: false),
                    Orden = table.Column<int>(type: "integer", nullable: false),
                    Activa = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ZonasCorporales", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PreguntasCapacidad",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ZonaId = table.Column<int>(type: "integer", nullable: false),
                    Clave = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Titulo = table.Column<string>(type: "text", nullable: false),
                    Ayuda = table.Column<string>(type: "text", nullable: true),
                    Eje = table.Column<int>(type: "integer", nullable: false),
                    Tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Unidad = table.Column<string>(type: "text", nullable: true),
                    Orden = table.Column<int>(type: "integer", nullable: false),
                    DependeDeClave = table.Column<string>(type: "text", nullable: true),
                    ActivaCuandoCsv = table.Column<string>(type: "text", nullable: true),
                    ValorSinNovedad = table.Column<string>(type: "text", nullable: true),
                    Activa = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreguntasCapacidad", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PreguntasCapacidad_ZonasCorporales_ZonaId",
                        column: x => x.ZonaId,
                        principalTable: "ZonasCorporales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OpcionesCapacidad",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PreguntaId = table.Column<int>(type: "integer", nullable: false),
                    Texto = table.Column<string>(type: "text", nullable: false),
                    Valor = table.Column<string>(type: "text", nullable: false),
                    Orden = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpcionesCapacidad", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OpcionesCapacidad_PreguntasCapacidad_PreguntaId",
                        column: x => x.PreguntaId,
                        principalTable: "PreguntasCapacidad",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OpcionesCapacidad_PreguntaId_Valor",
                table: "OpcionesCapacidad",
                columns: new[] { "PreguntaId", "Valor" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PreguntasCapacidad_ZonaId_Clave",
                table: "PreguntasCapacidad",
                columns: new[] { "ZonaId", "Clave" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ZonasCorporales_Codigo",
                table: "ZonasCorporales",
                column: "Codigo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OpcionesCapacidad");

            migrationBuilder.DropTable(
                name: "PreguntasCapacidad");

            migrationBuilder.DropTable(
                name: "ZonasCorporales");
        }
    }
}
