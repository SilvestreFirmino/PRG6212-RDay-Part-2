using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace RaceDayApi.Data.Migrations;

[DbContext(typeof(RaceDayDbContext))]
[Migration("20261107000100_AddEventBanner")]
public class AddEventBanner : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>("BannerUrl", "Events", type: "nvarchar(2048)", maxLength: 2048, nullable: true);
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn("BannerUrl", "Events");
    }
}
