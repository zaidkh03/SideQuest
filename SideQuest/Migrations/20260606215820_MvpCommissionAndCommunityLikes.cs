using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SideQuest.Migrations
{
    /// <inheritdoc />
    public partial class MvpCommissionAndCommunityLikes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ApprovedCommissionRate",
                table: "Jobs",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CommissionReviewNote",
                table: "Jobs",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CommissionReviewedAt",
                table: "Jobs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CommissionReviewedByAdminId",
                table: "Jobs",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DurationDays",
                table: "Jobs",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<decimal>(
                name: "HoursPerDay",
                table: "Jobs",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 8m);

            migrationBuilder.AddColumn<decimal>(
                name: "OfferedCommissionRate",
                table: "Jobs",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 10m);

            migrationBuilder.AddColumn<decimal>(
                name: "RequiredCommissionRate",
                table: "Jobs",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CommunityPostLikes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PostId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunityPostLikes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommunityPostLikes_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CommunityPostLikes_CommunityPosts_PostId",
                        column: x => x.PostId,
                        principalTable: "CommunityPosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CommunityPostLikes_PostId_UserId",
                table: "CommunityPostLikes",
                columns: new[] { "PostId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommunityPostLikes_UserId",
                table: "CommunityPostLikes",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommunityPostLikes");

            migrationBuilder.DropColumn(
                name: "ApprovedCommissionRate",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "CommissionReviewNote",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "CommissionReviewedAt",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "CommissionReviewedByAdminId",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "DurationDays",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "HoursPerDay",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "OfferedCommissionRate",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "RequiredCommissionRate",
                table: "Jobs");
        }
    }
}
