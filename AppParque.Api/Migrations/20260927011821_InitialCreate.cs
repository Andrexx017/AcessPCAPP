using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AppParque.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Atracciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    Descripcion = table.Column<string>(type: "text", nullable: true),
                    ImagenUrl = table.Column<string>(type: "text", nullable: true),
                    AlturaMinima = table.Column<int>(type: "integer", nullable: true),
                    AlturaMaxima = table.Column<int>(type: "integer", nullable: true),
                    Activa = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Atracciones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Condiciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "text", nullable: false),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    Descripcion = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Condiciones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Grupos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "text", nullable: false),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    Descripcion = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Grupos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Preguntas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Texto = table.Column<string>(type: "text", nullable: false),
                    Orden = table.Column<int>(type: "integer", nullable: false),
                    Tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Activa = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Preguntas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NombreCompleto = table.Column<string>(type: "text", nullable: false),
                    Username = table.Column<string>(type: "text", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    Rol = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Visitantes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TipoDocumento = table.Column<string>(type: "text", nullable: false),
                    NumeroDocumento = table.Column<string>(type: "text", nullable: false),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Visitantes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AtraccionCondicionRestricciones",
                columns: table => new
                {
                    AtraccionId = table.Column<int>(type: "integer", nullable: false),
                    CondicionId = table.Column<int>(type: "integer", nullable: false),
                    Permitido = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AtraccionCondicionRestricciones", x => new { x.AtraccionId, x.CondicionId });
                    table.ForeignKey(
                        name: "FK_AtraccionCondicionRestricciones_Atracciones_AtraccionId",
                        column: x => x.AtraccionId,
                        principalTable: "Atracciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AtraccionCondicionRestricciones_Condiciones_CondicionId",
                        column: x => x.CondicionId,
                        principalTable: "Condiciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AtraccionGrupoRestricciones",
                columns: table => new
                {
                    AtraccionId = table.Column<int>(type: "integer", nullable: false),
                    GrupoId = table.Column<int>(type: "integer", nullable: false),
                    Permitido = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AtraccionGrupoRestricciones", x => new { x.AtraccionId, x.GrupoId });
                    table.ForeignKey(
                        name: "FK_AtraccionGrupoRestricciones_Atracciones_AtraccionId",
                        column: x => x.AtraccionId,
                        principalTable: "Atracciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AtraccionGrupoRestricciones_Grupos_GrupoId",
                        column: x => x.GrupoId,
                        principalTable: "Grupos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OpcionesRespuesta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PreguntaId = table.Column<int>(type: "integer", nullable: false),
                    Texto = table.Column<string>(type: "text", nullable: false),
                    Orden = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpcionesRespuesta", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OpcionesRespuesta_Preguntas_PreguntaId",
                        column: x => x.PreguntaId,
                        principalTable: "Preguntas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Evaluaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VisitanteId = table.Column<int>(type: "integer", nullable: false),
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Edad = table.Column<int>(type: "integer", nullable: true),
                    Estatura = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Evaluaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Evaluaciones_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Evaluaciones_Visitantes_VisitanteId",
                        column: x => x.VisitanteId,
                        principalTable: "Visitantes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EvaluacionAtraccionResultados",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EvaluacionId = table.Column<int>(type: "integer", nullable: false),
                    AtraccionId = table.Column<int>(type: "integer", nullable: false),
                    PreseleccionadaAutomatica = table.Column<bool>(type: "boolean", nullable: false),
                    ValidadaPersonal = table.Column<bool>(type: "boolean", nullable: true),
                    Comentario = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluacionAtraccionResultados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EvaluacionAtraccionResultados_Atracciones_AtraccionId",
                        column: x => x.AtraccionId,
                        principalTable: "Atracciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EvaluacionAtraccionResultados_Evaluaciones_EvaluacionId",
                        column: x => x.EvaluacionId,
                        principalTable: "Evaluaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EvaluacionCondiciones",
                columns: table => new
                {
                    EvaluacionId = table.Column<int>(type: "integer", nullable: false),
                    CondicionId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluacionCondiciones", x => new { x.EvaluacionId, x.CondicionId });
                    table.ForeignKey(
                        name: "FK_EvaluacionCondiciones_Condiciones_CondicionId",
                        column: x => x.CondicionId,
                        principalTable: "Condiciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EvaluacionCondiciones_Evaluaciones_EvaluacionId",
                        column: x => x.EvaluacionId,
                        principalTable: "Evaluaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EvaluacionGrupos",
                columns: table => new
                {
                    EvaluacionId = table.Column<int>(type: "integer", nullable: false),
                    GrupoId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluacionGrupos", x => new { x.EvaluacionId, x.GrupoId });
                    table.ForeignKey(
                        name: "FK_EvaluacionGrupos_Evaluaciones_EvaluacionId",
                        column: x => x.EvaluacionId,
                        principalTable: "Evaluaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EvaluacionGrupos_Grupos_GrupoId",
                        column: x => x.GrupoId,
                        principalTable: "Grupos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EvaluacionRespuestas",
                columns: table => new
                {
                    EvaluacionId = table.Column<int>(type: "integer", nullable: false),
                    OpcionRespuestaId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluacionRespuestas", x => new { x.EvaluacionId, x.OpcionRespuestaId });
                    table.ForeignKey(
                        name: "FK_EvaluacionRespuestas_Evaluaciones_EvaluacionId",
                        column: x => x.EvaluacionId,
                        principalTable: "Evaluaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EvaluacionRespuestas_OpcionesRespuesta_OpcionRespuestaId",
                        column: x => x.OpcionRespuestaId,
                        principalTable: "OpcionesRespuesta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AtraccionCondicionRestricciones_CondicionId",
                table: "AtraccionCondicionRestricciones",
                column: "CondicionId");

            migrationBuilder.CreateIndex(
                name: "IX_AtraccionGrupoRestricciones_GrupoId",
                table: "AtraccionGrupoRestricciones",
                column: "GrupoId");

            migrationBuilder.CreateIndex(
                name: "IX_Condiciones_Codigo",
                table: "Condiciones",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EvaluacionAtraccionResultados_AtraccionId",
                table: "EvaluacionAtraccionResultados",
                column: "AtraccionId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluacionAtraccionResultados_EvaluacionId_AtraccionId",
                table: "EvaluacionAtraccionResultados",
                columns: new[] { "EvaluacionId", "AtraccionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EvaluacionCondiciones_CondicionId",
                table: "EvaluacionCondiciones",
                column: "CondicionId");

            migrationBuilder.CreateIndex(
                name: "IX_Evaluaciones_UsuarioId",
                table: "Evaluaciones",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Evaluaciones_VisitanteId",
                table: "Evaluaciones",
                column: "VisitanteId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluacionGrupos_GrupoId",
                table: "EvaluacionGrupos",
                column: "GrupoId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluacionRespuestas_OpcionRespuestaId",
                table: "EvaluacionRespuestas",
                column: "OpcionRespuestaId");

            migrationBuilder.CreateIndex(
                name: "IX_Grupos_Codigo",
                table: "Grupos",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OpcionesRespuesta_PreguntaId",
                table: "OpcionesRespuesta",
                column: "PreguntaId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Username",
                table: "Usuarios",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Visitantes_TipoDocumento_NumeroDocumento",
                table: "Visitantes",
                columns: new[] { "TipoDocumento", "NumeroDocumento" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AtraccionCondicionRestricciones");

            migrationBuilder.DropTable(
                name: "AtraccionGrupoRestricciones");

            migrationBuilder.DropTable(
                name: "EvaluacionAtraccionResultados");

            migrationBuilder.DropTable(
                name: "EvaluacionCondiciones");

            migrationBuilder.DropTable(
                name: "EvaluacionGrupos");

            migrationBuilder.DropTable(
                name: "EvaluacionRespuestas");

            migrationBuilder.DropTable(
                name: "Atracciones");

            migrationBuilder.DropTable(
                name: "Condiciones");

            migrationBuilder.DropTable(
                name: "Grupos");

            migrationBuilder.DropTable(
                name: "Evaluaciones");

            migrationBuilder.DropTable(
                name: "OpcionesRespuesta");

            migrationBuilder.DropTable(
                name: "Usuarios");

            migrationBuilder.DropTable(
                name: "Visitantes");

            migrationBuilder.DropTable(
                name: "Preguntas");
        }
    }
}
