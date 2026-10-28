using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TWCF_BibleApp.Persistence.Contexts;

#nullable disable

namespace TWCF_BibleApp.Migrations
{
    [DbContext(typeof(UserDataContext))]
    [Migration("20261028_AddShowWordsOfJesus")]
    public partial class AddShowWordsOfJesus : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ShowWordsOfJesus",
                table: "UserSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShowWordsOfJesus",
                table: "UserSettings");
        }
    }
}
