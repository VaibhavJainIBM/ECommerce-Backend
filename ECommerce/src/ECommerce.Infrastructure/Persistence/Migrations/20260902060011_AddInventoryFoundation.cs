using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InventoryItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SellerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SellerListingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OnHandQuantity = table.Column<int>(type: "int", nullable: false),
                    ReservedQuantity = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryItems", x => x.Id);
                    table.CheckConstraint("CK_InventoryItems_OnHand_NonNegative", "[OnHandQuantity] >= 0");
                    table.CheckConstraint("CK_InventoryItems_Reserved_NonNegative", "[ReservedQuantity] >= 0");
                    table.CheckConstraint("CK_InventoryItems_Reserved_NotGreaterThan_OnHand", "[ReservedQuantity] <= [OnHandQuantity]");
                    table.ForeignKey(
                        name: "FK_InventoryItems_SellerListings_SellerId_SellerListingId",
                        columns: x => new { x.SellerId, x.SellerListingId },
                        principalTable: "SellerListings",
                        principalColumns: new[] { "SellerId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryItems_Warehouses_SellerId_WarehouseId",
                        columns: x => new { x.SellerId, x.WarehouseId },
                        principalTable: "Warehouses",
                        principalColumns: new[] { "SellerId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_SellerId_SellerListingId",
                table: "InventoryItems",
                columns: new[] { "SellerId", "SellerListingId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_SellerId_WarehouseId_SellerListingId",
                table: "InventoryItems",
                columns: new[] { "SellerId", "WarehouseId", "SellerListingId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventoryItems");
        }
    }
}
