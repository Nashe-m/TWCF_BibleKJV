using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TWCF_BibleApp.Persistence.Contexts;

#nullable disable

namespace TWCF_BibleApp.Migrations
{
    [DbContext(typeof(UserDataContext))]
    [Migration("20261103_AddReadingFont")]
    public partial class AddReadingFont : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(name: "ReadingFont", table: "UserSettings", type: "INTEGER", nullable: false, defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "ReadingFont", table: "UserSettings");
        }
    }
}
