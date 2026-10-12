using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Talabat.Repository.Data;

#nullable disable

namespace Talabat.Repository.Data.Migrations;

[DbContext(typeof(StoreContext))]
[Migration("20261012120000_RemoveDeliveryMethods")]
public partial class RemoveDeliveryMethods : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Orders_DeliveryMethods_DeliveryMethodId",
            table: "Orders");

        migrationBuilder.DropIndex(
            name: "IX_Orders_DeliveryMethodId",
            table: "Orders");

        migrationBuilder.DropColumn(name: "DeliveryMethodId", table: "Orders");
        migrationBuilder.DropColumn(name: "DeliveryMethodId", table: "CustomerBaskets");
        migrationBuilder.DropColumn(name: "ShippingPrice", table: "CustomerBaskets");
        migrationBuilder.DropTable(name: "DeliveryMethods");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "DeliveryMethods",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Cost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                DeliveryTime = table.Column<string>(type: "nvarchar(max)", nullable: false),
                Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                ShortName = table.Column<string>(type: "nvarchar(max)", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_DeliveryMethods", x => x.Id));

        migrationBuilder.AddColumn<int>(
            name: "DeliveryMethodId", table: "Orders", type: "int", nullable: true);
        migrationBuilder.AddColumn<int>(
            name: "DeliveryMethodId", table: "CustomerBaskets", type: "int", nullable: true);
        migrationBuilder.AddColumn<decimal>(
            name: "ShippingPrice", table: "CustomerBaskets", type: "decimal(18,2)", nullable: false, defaultValue: 0m);

        migrationBuilder.CreateIndex(
            name: "IX_Orders_DeliveryMethodId", table: "Orders", column: "DeliveryMethodId");
        migrationBuilder.AddForeignKey(
            name: "FK_Orders_DeliveryMethods_DeliveryMethodId",
            table: "Orders",
            column: "DeliveryMethodId",
            principalTable: "DeliveryMethods",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);
    }
}
