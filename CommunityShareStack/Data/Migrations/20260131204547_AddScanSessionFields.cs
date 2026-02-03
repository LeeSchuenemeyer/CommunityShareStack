using Microsoft.EntityFrameworkCore.Migrations;

namespace CommunityShareStack.Data.Migrations
{
    public partial class AddScanSessionFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "IsbnConfidence",
                table: "ScanSessions",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OcrText",
                table: "ScanSessions",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsbnConfidence",
                table: "ScanSessions");

            migrationBuilder.DropColumn(
                name: "OcrText",
                table: "ScanSessions");
        }
    }
}
