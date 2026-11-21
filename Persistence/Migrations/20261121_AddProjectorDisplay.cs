using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TWCF_BibleApp.Persistence.Contexts;

#nullable disable

namespace TWCF_BibleApp.Migrations
{
    [DbContext(typeof(UserDataContext))]
    [Migration("20261121_AddProjectorDisplay")]
    public partial class AddProjectorDisplay : Migration
    {
        // Existing users get one verse at a time too (1 = ProjectorDisplay.SingleVerse).
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(name: "ProjectorDisplay", table: "UserSettings", type: "INTEGER", nullable: false, defaultValue: 1);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "ProjectorDisplay", table: "UserSettings");
        }
    }
}
