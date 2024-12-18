using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace web_api.Migrations
{
    /// <inheritdoc />
    public partial class creditcards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentDate",
                table: "CreditCards");

            migrationBuilder.DropColumn(
                name: "StatementClosingDate",
                table: "CreditCards");

            migrationBuilder.AddColumn<int>(
                name: "PaymentOffset",
                table: "CreditCards",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "StatementClosingDay",
                table: "CreditCards",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentOffset",
                table: "CreditCards");

            migrationBuilder.DropColumn(
                name: "StatementClosingDay",
                table: "CreditCards");

            migrationBuilder.AddColumn<DateTime>(
                name: "PaymentDate",
                table: "CreditCards",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "StatementClosingDate",
                table: "CreditCards",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }
    }
}
