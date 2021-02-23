using Microsoft.EntityFrameworkCore.Migrations;

namespace TekTox.DAL.Migrations.Migrations
{
    public partial class MessageId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "EventMessageId",
                table: "EventLists",
                type: "decimal(20,0)",
                nullable: false,
                defaultValue: 0m);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EventMessageId",
                table: "EventLists");
        }
    }
}
