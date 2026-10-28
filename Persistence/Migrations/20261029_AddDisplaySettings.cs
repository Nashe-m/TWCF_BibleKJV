using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TWCF_BibleApp.Persistence.Contexts;

#nullable disable

namespace TWCF_BibleApp.Migrations
{
    // Dated the 29th so it sorts after 20261028_AddShowWordsOfJesus (ids are applied in string order).
    [DbContext(typeof(UserDataContext))]
    [Migration("20261029_AddDisplaySettings")]
    public partial class AddDisplaySettings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(name: "LineSpacing", table: "UserSettings", type: "REAL", nullable: false, defaultValue: 1.45);
            migrationBuilder.AddColumn<bool>(name: "ShowVerseNumbers", table: "UserSettings", type: "INTEGER", nullable: false, defaultValue: true);
            migrationBuilder.AddColumn<int>(name: "ProjectorTheme", table: "UserSettings", type: "INTEGER", nullable: false, defaultValue: 0);
            migrationBuilder.AddColumn<int>(name: "ProjectorMargins", table: "UserSettings", type: "INTEGER", nullable: false, defaultValue: 1);
            migrationBuilder.AddColumn<bool>(name: "ShowAnnotationsWhenProjecting", table: "UserSettings", type: "INTEGER", nullable: false, defaultValue: false);
            migrationBuilder.AddColumn<bool>(name: "MarkJumpWhenProjecting", table: "UserSettings", type: "INTEGER", nullable: false, defaultValue: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "LineSpacing", table: "UserSettings");
            migrationBuilder.DropColumn(name: "ShowVerseNumbers", table: "UserSettings");
            migrationBuilder.DropColumn(name: "ProjectorTheme", table: "UserSettings");
            migrationBuilder.DropColumn(name: "ProjectorMargins", table: "UserSettings");
            migrationBuilder.DropColumn(name: "ShowAnnotationsWhenProjecting", table: "UserSettings");
            migrationBuilder.DropColumn(name: "MarkJumpWhenProjecting", table: "UserSettings");
        }
    }
}
