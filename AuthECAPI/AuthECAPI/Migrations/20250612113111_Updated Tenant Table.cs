using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthECAPI.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedTenantTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Container",
                table: "Tenants",
                type: "varchar(100)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Provider",
                table: "Tenants",
                type: "varchar(50)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Container",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "Provider",
                table: "Tenants");
        }
    }
}
