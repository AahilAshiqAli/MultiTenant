using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthECAPI.Migrations
{
    /// <inheritdoc />
    public partial class TenantTableChanged : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Name",
                table: "Products");

            migrationBuilder.AddColumn<string>(
                name: "DefaultBlobTier",
                table: "Tenants",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "EnableVersioning",
                table: "Tenants",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "RetentionDays",
                table: "Tenants",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultBlobTier",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "EnableVersioning",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "RetentionDays",
                table: "Tenants");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Products",
                type: "nvarchar(150)",
                nullable: false,
                defaultValue: "");
        }
    }
}
