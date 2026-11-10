using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Order.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialOrderDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdempotencyKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyCode = table.Column<string>(type: "varchar(3)", unicode: false, maxLength: 3, nullable: false),
                    ShippingFullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ShippingLine1 = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    ShippingLine2 = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    ShippingCity = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ShippingState = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ShippingPostalCode = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    ShippingCountryCode = table.Column<string>(type: "varchar(2)", unicode: false, maxLength: 2, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    PaidAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    PaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CancelledAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CancellationReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrderItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SellerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SellerListingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductVariantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SellerSku = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ProductTitle = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    VariantName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LineTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderItems_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_OrderId",
                table: "OrderItems",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_SellerId",
                table: "OrderItems",
                column: "SellerId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CustomerId_CreatedAtUtc",
                table: "Orders",
                columns: new[] { "CustomerId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CustomerId_IdempotencyKey",
                table: "Orders",
                columns: new[] { "CustomerId", "IdempotencyKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderItems");

            migrationBuilder.DropTable(
                name: "Orders");
        }
    }
}
