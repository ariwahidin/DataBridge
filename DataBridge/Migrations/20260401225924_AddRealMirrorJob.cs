using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataBridge.Migrations
{
    /// <inheritdoc />
    public partial class AddRealMirrorJob : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$SrM9VAtyI3AtI4ENeQTFWun6FYBqMENT875AXU5zEj6K79cO/DEkS");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$zacaANgHvAW1RVwf9Sj12uwJYbcdhxDp/z9kBwwiaZGnnzi9cGwFC");
        }
    }
}
