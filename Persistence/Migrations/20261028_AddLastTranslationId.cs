using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TWCF_BibleApp.Persistence.Contexts;

#nullable disable

namespace TWCF_BibleApp.Migrations
{
    [DbContext(typeof(UserDataContext))]
    [Migration("20261028_AddLastTranslationId")]
    public partial class AddLastTranslationId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LastTranslationId",
                table: "UserSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastTranslationId",
                table: "UserSettings");
        }
    }
}
