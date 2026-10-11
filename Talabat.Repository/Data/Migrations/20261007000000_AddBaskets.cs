using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Talabat.Repository.Data;

#nullable disable

namespace Talabat.Repository.Data.Migrations;

[DbContext(typeof(StoreContext))]
[Migration("20261007000000_AddBaskets")]
public sealed class AddBaskets : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "CustomerBaskets",
            columns: table => new
            {
                id = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                PaymentIntentId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                ClientSecret = table.Column<string>(type: "nvarchar(max)", nullable: true),
                DeliveryMethodId = table.Column<int>(type: "int", nullable: true),
                ShippingPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_CustomerBaskets", x => x.id));

        migrationBuilder.CreateTable(
            name: "BasketItems",
            columns: table => new
            {
                BasketId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                Id = table.Column<int>(type: "int", nullable: false),
                productName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                PictureUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                Category = table.Column<string>(type: "nvarchar(max)", nullable: false),
                Brand = table.Column<string>(type: "nvarchar(max)", nullable: false),
                Quantity = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_BasketItems", x => new { x.BasketId, x.Id });
                table.ForeignKey("FK_BasketItems_CustomerBaskets_BasketId", x => x.BasketId, "CustomerBaskets", "id", onDelete: ReferentialAction.Cascade);
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "BasketItems");
        migrationBuilder.DropTable(name: "CustomerBaskets");
    }
}
