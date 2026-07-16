using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionProduits.Api.Migrations
{
    /// <inheritdoc />
    public partial class DropUsersTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // La table Users n'est plus gérée par l'application : les utilisateurs
            // vivent désormais dans Keycloak. On la supprime définitivement.
            migrationBuilder.DropTable(name: "Users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Recréation minimale (rollback) — reflète l'état après RemoveUserAuthFields,
            // c'est-à-dire sans les colonnes d'authentification.
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstName = table.Column<string>(type: "text", nullable: false),
                    LastName = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    AvatarUrl = table.Column<string>(type: "text", nullable: true),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    Department = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });
        }
    }
}
