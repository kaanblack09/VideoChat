using Microsoft.EntityFrameworkCore.Migrations;

namespace VideoChat.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IdentityUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(nullable: false),
                    UserID = table.Column<string>(nullable: false),
                    ProviderKey = table.Column<string>(nullable: true),
                    ProviderDisplayName = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdentityUserLogins", x => new { x.LoginProvider, x.UserID });
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IdentityUserLogins");
        }
    }
}
