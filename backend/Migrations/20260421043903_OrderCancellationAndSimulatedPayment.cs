using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class OrderCancellationAndSimulatedPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BuyerCancellationReason",
                table: "Orders",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CancellationReviewedByUserId",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancellationReviewerNote",
                table: "Orders",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PreCancellationStatus",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SimulatedPaymentMethod",
                table: "Orders",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BuyerCancellationReason",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CancellationReviewedByUserId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CancellationReviewerNote",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PreCancellationStatus",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "SimulatedPaymentMethod",
                table: "Orders");
        }
    }
}
