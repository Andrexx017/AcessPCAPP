using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppParque.Api.Migrations
{
    /// <inheritdoc />
    public partial class AgregarAcompananteEmergenciaYPoliticasParque : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AceptaPoliticasParque",
                table: "Visitantes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Alergias",
                table: "Visitantes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Eps",
                table: "Visitantes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaAceptacionPoliticas",
                table: "Visitantes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NombreAcompanante",
                table: "Visitantes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ParentescoAcompanante",
                table: "Visitantes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TelefonoAcompanante",
                table: "Visitantes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TipoSangre",
                table: "Visitantes",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AceptaPoliticasParque",
                table: "Visitantes");

            migrationBuilder.DropColumn(
                name: "Alergias",
                table: "Visitantes");

            migrationBuilder.DropColumn(
                name: "Eps",
                table: "Visitantes");

            migrationBuilder.DropColumn(
                name: "FechaAceptacionPoliticas",
                table: "Visitantes");

            migrationBuilder.DropColumn(
                name: "NombreAcompanante",
                table: "Visitantes");

            migrationBuilder.DropColumn(
                name: "ParentescoAcompanante",
                table: "Visitantes");

            migrationBuilder.DropColumn(
                name: "TelefonoAcompanante",
                table: "Visitantes");

            migrationBuilder.DropColumn(
                name: "TipoSangre",
                table: "Visitantes");
        }
    }
}
