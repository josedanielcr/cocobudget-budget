using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace web_api.Migrations
{
    /// <inheritdoc />
    public partial class foldernodesc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "Folders");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Folders",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");
        }
    }
}
