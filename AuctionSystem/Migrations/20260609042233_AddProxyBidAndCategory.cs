using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AuctionSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddProxyBidAndCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "AuctionItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsApproved",
                table: "AuctionItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "AuctionCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Icon = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuctionCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProxyBids",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AuctionItemId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    MaxAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProxyBids", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProxyBids_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProxyBids_AuctionItems_AuctionItemId",
                        column: x => x.AuctionItemId,
                        principalTable: "AuctionItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "AuctionCategories",
                columns: new[] { "Id", "Icon", "Name" },
                values: new object[,]
                {
                    { 1, "fa-mobile-alt", "Điện thoại" },
                    { 2, "fa-laptop", "Laptop" },
                    { 3, "fa-clock", "Đồng hồ" },
                    { 4, "fa-tshirt", "Thời trang" },
                    { 5, "fa-car", "Xe cộ" },
                    { 6, "fa-gem", "Đồ cổ" },
                    { 7, "fa-box", "Khác" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuctionItems_CategoryId",
                table: "AuctionItems",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ProxyBids_AuctionItemId",
                table: "ProxyBids",
                column: "AuctionItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ProxyBids_UserId",
                table: "ProxyBids",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_AuctionItems_AuctionCategories_CategoryId",
                table: "AuctionItems",
                column: "CategoryId",
                principalTable: "AuctionCategories",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuctionItems_AuctionCategories_CategoryId",
                table: "AuctionItems");

            migrationBuilder.DropTable(
                name: "AuctionCategories");

            migrationBuilder.DropTable(
                name: "ProxyBids");

            migrationBuilder.DropIndex(
                name: "IX_AuctionItems_CategoryId",
                table: "AuctionItems");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "AuctionItems");

            migrationBuilder.DropColumn(
                name: "IsApproved",
                table: "AuctionItems");
        }
    }
}
