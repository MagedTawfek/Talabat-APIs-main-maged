using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Talabat.Repository.Identity;

#nullable disable

namespace Talabat.Repository.Identity.Migrations;

[DbContext(typeof(AppIdentityDbContext))]
[Migration("20261007000000_AddRefreshTokens")]
public partial class AddRefreshTokens : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "RefreshToken", table: "AspNetUsers", type: "nvarchar(max)", nullable: true);
        migrationBuilder.AddColumn<DateTime>(name: "RefreshTokenExpiryTime", table: "AspNetUsers", type: "datetime2", nullable: false, defaultValue: new DateTime(1, 1, 1));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "RefreshToken", table: "AspNetUsers");
        migrationBuilder.DropColumn(name: "RefreshTokenExpiryTime", table: "AspNetUsers");
    }
}
