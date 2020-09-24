using Microsoft.EntityFrameworkCore.Migrations;

namespace VideoChat.Migrations
{
    public partial class updatedb : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_IdentityUserLogins",
                table: "IdentityUserLogins");

            migrationBuilder.RenameTable(
                name: "IdentityUserLogins",
                newName: "IdentityUserLogin");

            migrationBuilder.AddPrimaryKey(
                name: "PK_IdentityUserLogin",
                table: "IdentityUserLogin",
                columns: new[] { "LoginProvider", "UserID" });

            migrationBuilder.CreateTable(
                name: "ClassRoom",
                columns: table => new
                {
                    ClassID = table.Column<string>(nullable: false),
                    ClassName = table.Column<string>(nullable: true),
                    Topic = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassRoom", x => x.ClassID);
                });

            migrationBuilder.CreateTable(
                name: "IdentityUser",
                columns: table => new
                {
                    ID = table.Column<string>(nullable: false),
                    UserName = table.Column<string>(nullable: true),
                    ConnectionID = table.Column<string>(nullable: true),
                    Passwd = table.Column<string>(nullable: true),
                    InCall = table.Column<bool>(nullable: false),
                    IsCaller = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdentityUser", x => x.ID);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClassRoom");

            migrationBuilder.DropTable(
                name: "IdentityUser");

            migrationBuilder.DropPrimaryKey(
                name: "PK_IdentityUserLogin",
                table: "IdentityUserLogin");

            migrationBuilder.RenameTable(
                name: "IdentityUserLogin",
                newName: "IdentityUserLogins");

            migrationBuilder.AddPrimaryKey(
                name: "PK_IdentityUserLogins",
                table: "IdentityUserLogins",
                columns: new[] { "LoginProvider", "UserID" });
        }
    }
}
