using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppParque.Api.Migrations
{
    /// <inheritdoc />
    public partial class AgregarIntensidadAtraccion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Intensidad",
                table: "Atracciones",
                type: "text",
                nullable: false,
                defaultValue: "media");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Intensidad",
                table: "Atracciones");
        }
    }
}
