using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TaskMate.Api.Data;

#nullable disable

namespace TaskMate.Api.Data.Migrations;

[DbContext(typeof(TaskMateDbContext))]
[Migration("20261119220000_AddStudyGoals")]
public sealed class AddStudyGoals : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "StudyGoals",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                CourseId = table.Column<Guid>(type: "TEXT", nullable: true),
                Title = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                TargetMinutesPerWeek = table.Column<int>(type: "INTEGER", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_StudyGoals", goal => goal.Id);
                table.ForeignKey("FK_StudyGoals_Users_UserId", goal => goal.UserId, "Users", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_StudyGoals_Courses_CourseId", goal => goal.CourseId, "Courses", "Id", onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateIndex("IX_StudyGoals_UserId", "StudyGoals", "UserId");
        migrationBuilder.CreateIndex("IX_StudyGoals_CourseId", "StudyGoals", "CourseId");
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable("StudyGoals");
}

