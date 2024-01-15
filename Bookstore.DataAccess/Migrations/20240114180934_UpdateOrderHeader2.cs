using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bookstore.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class UpdateOrderHeader2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PaymentDue",
                table: "OrderHeaders",
                newName: "PaymentDueDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PaymentDueDate",
                table: "OrderHeaders",
                newName: "PaymentDue");
        }
    }
}
