using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Talabat.Repository.Data;

#nullable disable

namespace Talabat.Repository.Data.Migrations;

[DbContext(typeof(StoreContext))]
[Migration("20261007000001_RemovePaymentData")]
public partial class RemovePaymentData : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "PaymentIntentId", table: "Orders");
        migrationBuilder.DropColumn(name: "PaymentIntentId", table: "CustomerBaskets");
        migrationBuilder.DropColumn(name: "ClientSecret", table: "CustomerBaskets");
        migrationBuilder.Sql("UPDATE Orders SET Status = 'Pending' WHERE Status IN ('paymentReceived', 'PaymentFailed')");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "PaymentIntentId", table: "Orders", type: "nvarchar(max)", nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<string>(name: "PaymentIntentId", table: "CustomerBaskets", type: "nvarchar(max)", nullable: true);
        migrationBuilder.AddColumn<string>(name: "ClientSecret", table: "CustomerBaskets", type: "nvarchar(max)", nullable: true);
    }
}
