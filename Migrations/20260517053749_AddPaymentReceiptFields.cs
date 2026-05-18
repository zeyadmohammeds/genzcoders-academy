using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenZCoders.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentReceiptFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PaymentMethod",
                table: "CourseApplications",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PaymentReceiptPendingReview",
                table: "CourseApplications",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PaymentReceiptUrl",
                table: "CourseApplications",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "CourseApplications");

            migrationBuilder.DropColumn(
                name: "PaymentReceiptPendingReview",
                table: "CourseApplications");

            migrationBuilder.DropColumn(
                name: "PaymentReceiptUrl",
                table: "CourseApplications");
        }
    }
}
