using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FurnitureStore.Data.Migrations
{
    /// <inheritdoc />
    public partial class addclientId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Client",
                table: "Orders",
                newName: "ClientId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ClientId",
                table: "Orders",
                newName: "Client");
        }
    }
}
