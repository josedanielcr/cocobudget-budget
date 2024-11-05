using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace web_api.Migrations
{
    /// <inheritdoc />
    public partial class folderswithperiods : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "GeneralId",
                table: "Folders",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "PeriodId",
                table: "Folders",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Folders_PeriodId",
                table: "Folders",
                column: "PeriodId");

            migrationBuilder.AddForeignKey(
                name: "FK_Folders_Periods_PeriodId",
                table: "Folders",
                column: "PeriodId",
                principalTable: "Periods",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Folders_Periods_PeriodId",
                table: "Folders");

            migrationBuilder.DropIndex(
                name: "IX_Folders_PeriodId",
                table: "Folders");

            migrationBuilder.DropColumn(
                name: "GeneralId",
                table: "Folders");

            migrationBuilder.DropColumn(
                name: "PeriodId",
                table: "Folders");
        }
    }
}
