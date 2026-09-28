using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppParque.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddVisitanteConsentimiento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ConsentimientoTratamientoDatos",
                table: "Visitantes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaConsentimiento",
                table: "Visitantes",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConsentimientoTratamientoDatos",
                table: "Visitantes");

            migrationBuilder.DropColumn(
                name: "FechaConsentimiento",
                table: "Visitantes");
        }
    }
}
