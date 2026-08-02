using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinancialTransaction.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCreatedAtUtcToTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "transactions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_transactions_CreatedAtUtc",
                table: "transactions",
                column: "CreatedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_transactions_CreatedAtUtc",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "transactions");
        }
    }
}
