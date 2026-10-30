using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace healthcoreclinic_app_demo.Migrations
{
    /// <inheritdoc />
    public partial class AddUserCreatedByAdmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CreatedByAdminID",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_CreatedByAdminID",
                table: "Users",
                column: "CreatedByAdminID");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Users_CreatedByAdminID",
                table: "Users",
                column: "CreatedByAdminID",
                principalTable: "Users",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Users_CreatedByAdminID",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_CreatedByAdminID",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CreatedByAdminID",
                table: "Users");
        }
    }
}
