using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppParque.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddOpcionRespuestaCodigo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Codigo",
                table: "OpcionesRespuesta",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_OpcionesRespuesta_Codigo",
                table: "OpcionesRespuesta",
                column: "Codigo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OpcionesRespuesta_Codigo",
                table: "OpcionesRespuesta");

            migrationBuilder.DropColumn(
                name: "Codigo",
                table: "OpcionesRespuesta");
        }
    }
}
