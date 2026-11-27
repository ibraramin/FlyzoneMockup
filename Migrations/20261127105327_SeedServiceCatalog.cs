using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FlyzoneMockup.Migrations
{
    /// <inheritdoc />
    public partial class SeedServiceCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "TypingServices",
                keyColumn: "Id",
                keyValue: 1,
                column: "Description",
                value: "10-year residency visa processing for investors and specialists.");

            migrationBuilder.UpdateData(
                table: "TypingServices",
                keyColumn: "Id",
                keyValue: 2,
                column: "Description",
                value: "Renew your UAE driving license, including eye-test coordination.");

            migrationBuilder.InsertData(
                table: "TypingServices",
                columns: new[] { "Id", "BaseFee", "Category", "Description", "IsActive", "ServiceName" },
                values: new object[,]
                {
                    { 3, 180.00m, "Identity Documents", "Renew or replace your Emirates ID with biometric capture.", true, "Emirates ID Renewal" },
                    { 4, 650.00m, "Visa & Immigration", "New work permits, visa transfers and cancellations.", true, "Work Permit & Visa" },
                    { 5, 1450.00m, "Business Services", "Company formation, trade-name reservation and license issuance.", true, "Business License Setup" },
                    { 6, 120.00m, "Document Services", "Certificate attestation, certified translation and courier handling.", true, "Document Attestation" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "TypingServices",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "TypingServices",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "TypingServices",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "TypingServices",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.UpdateData(
                table: "TypingServices",
                keyColumn: "Id",
                keyValue: 1,
                column: "Description",
                value: "10-year residency visa processing.");

            migrationBuilder.UpdateData(
                table: "TypingServices",
                keyColumn: "Id",
                keyValue: 2,
                column: "Description",
                value: "Renew your UAE driving license.");
        }
    }
}
