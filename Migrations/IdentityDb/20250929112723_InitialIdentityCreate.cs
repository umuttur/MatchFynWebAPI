using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MatchFynWebAPI.Migrations.IdentityDb
{
    /// <inheritdoc />
    public partial class InitialIdentityCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MatchFyn_Roles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Priority = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchFyn_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MatchFyn_Users",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Gender = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ProfileImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Bio = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsEmailVerified = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsPhoneVerified = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    LastLoginAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RefreshToken = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RefreshTokenExpiryTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchFyn_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MatchFyn_RoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchFyn_RoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchFyn_RoleClaims_MatchFyn_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "MatchFyn_Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MatchFyn_UserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchFyn_UserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchFyn_UserClaims_MatchFyn_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "MatchFyn_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MatchFyn_UserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchFyn_UserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_MatchFyn_UserLogins_MatchFyn_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "MatchFyn_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MatchFyn_UserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchFyn_UserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_MatchFyn_UserRoles_MatchFyn_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "MatchFyn_Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MatchFyn_UserRoles_MatchFyn_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "MatchFyn_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MatchFyn_UserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchFyn_UserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_MatchFyn_UserTokens_MatchFyn_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "MatchFyn_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "MatchFyn_Roles",
                columns: new[] { "Id", "ConcurrencyStamp", "CreatedAt", "Description", "IsActive", "Name", "NormalizedName", "Priority" },
                values: new object[,]
                {
                    { "001e8c78-bb14-4ec7-acc0-235eb7ada4c0", null, new DateTime(2025, 9, 29, 11, 27, 22, 800, DateTimeKind.Utc).AddTicks(1875), "Regular user with standard features", true, "User", "USER", 80 },
                    { "139c9928-af89-47e3-b101-714a1ee73dfb", null, new DateTime(2025, 9, 29, 11, 27, 22, 800, DateTimeKind.Utc).AddTicks(1879), "Premium user with extended features", true, "PremiumUser", "PREMİUMUSER", 70 },
                    { "2e198ca8-1dbf-4cf6-8e0e-063444b82ad2", null, new DateTime(2025, 9, 29, 11, 27, 22, 800, DateTimeKind.Utc).AddTicks(1865), "Content moderator with limited admin access", true, "Moderator", "MODERATOR", 90 },
                    { "82432d0a-8c64-46c3-a1cc-bd66a669ee03", null, new DateTime(2025, 9, 29, 11, 27, 22, 800, DateTimeKind.Utc).AddTicks(1857), "System administrator with full access", true, "Admin", "ADMİN", 100 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_MatchFyn_RoleClaims_RoleId",
                table: "MatchFyn_RoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationRoles_IsActive",
                table: "MatchFyn_Roles",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "MatchFyn_Roles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MatchFyn_UserClaims_UserId",
                table: "MatchFyn_UserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchFyn_UserLogins_UserId",
                table: "MatchFyn_UserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchFyn_UserRoles_RoleId",
                table: "MatchFyn_UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "MatchFyn_Users",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationUsers_CreatedAt",
                table: "MatchFyn_Users",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationUsers_Email",
                table: "MatchFyn_Users",
                column: "Email",
                unique: true,
                filter: "[Email] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationUsers_IsActive",
                table: "MatchFyn_Users",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationUsers_PhoneNumber",
                table: "MatchFyn_Users",
                column: "PhoneNumber");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "MatchFyn_Users",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MatchFyn_RoleClaims");

            migrationBuilder.DropTable(
                name: "MatchFyn_UserClaims");

            migrationBuilder.DropTable(
                name: "MatchFyn_UserLogins");

            migrationBuilder.DropTable(
                name: "MatchFyn_UserRoles");

            migrationBuilder.DropTable(
                name: "MatchFyn_UserTokens");

            migrationBuilder.DropTable(
                name: "MatchFyn_Roles");

            migrationBuilder.DropTable(
                name: "MatchFyn_Users");
        }
    }
}
