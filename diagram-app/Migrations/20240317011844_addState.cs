using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace diagram_app.Migrations
{
    /// <inheritdoc />
    public partial class addState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "State",
                table: "ExternalServiceToken",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "State",
                table: "ExternalServiceToken");
        }
    }
}
