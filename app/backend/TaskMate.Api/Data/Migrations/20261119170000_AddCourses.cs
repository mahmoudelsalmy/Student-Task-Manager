using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TaskMate.Api.Data;

#nullable disable

namespace TaskMate.Api.Data.Migrations;

[DbContext(typeof(TaskMateDbContext))]
[Migration("20261119170000_AddCourses")]
public sealed class AddCourses : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Courses",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                Code = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                Instructor = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                Semester = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                Color = table.Column<string>(type: "TEXT", maxLength: 7, nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Courses", x => x.Id);
                table.ForeignKey(
                    name: "FK_Courses_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Courses_UserId_Name",
            table: "Courses",
            columns: new[] { "UserId", "Name" });

        migrationBuilder.CreateIndex(
            name: "IX_Courses_UserId_Code",
            table: "Courses",
            columns: new[] { "UserId", "Code" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable(name: "Courses");
}
