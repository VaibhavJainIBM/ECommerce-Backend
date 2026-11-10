using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Payment.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialPaymentDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PaymentAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyCode = table.Column<string>(type: "char(3)", unicode: false, fixedLength: true, maxLength: 3, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentAttempts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAttempts_CustomerId",
                table: "PaymentAttempts",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAttempts_OrderId_RequestKey",
                table: "PaymentAttempts",
                columns: new[] { "OrderId", "RequestKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Payments_OneCreated",
                table: "PaymentAttempts",
                column: "OrderId",
                unique: true,
                filter: "[Status] = 'Created'");

            migrationBuilder.CreateIndex(
                name: "UX_Payments_OneSucceeded",
                table: "PaymentAttempts",
                column: "OrderId",
                unique: true,
                filter: "[Status] = 'Succeeded'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentAttempts");
        }
    }
}
