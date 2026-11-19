using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TaskMate.Api.Data;

#nullable disable

namespace TaskMate.Api.Data.Migrations;

[DbContext(typeof(TaskMateDbContext))]
[Migration("20261119230000_AddStudyPlanBlocks")]
public sealed class AddStudyPlanBlocks : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "StudyPlanBlocks",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                CourseId = table.Column<Guid>(type: "TEXT", nullable: true),
                TaskId = table.Column<Guid>(type: "TEXT", nullable: true),
                Title = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                StartsAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                DurationMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_StudyPlanBlocks", block => block.Id);
                table.ForeignKey("FK_StudyPlanBlocks_Users_UserId", block => block.UserId, "Users", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_StudyPlanBlocks_Courses_CourseId", block => block.CourseId, "Courses", "Id", onDelete: ReferentialAction.SetNull);
                table.ForeignKey("FK_StudyPlanBlocks_Tasks_TaskId", block => block.TaskId, "Tasks", "Id", onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateIndex("IX_StudyPlanBlocks_UserId_StartsAtUtc", "StudyPlanBlocks", new[] { "UserId", "StartsAtUtc" });
        migrationBuilder.CreateIndex("IX_StudyPlanBlocks_CourseId", "StudyPlanBlocks", "CourseId");
        migrationBuilder.CreateIndex("IX_StudyPlanBlocks_TaskId", "StudyPlanBlocks", "TaskId");
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable("StudyPlanBlocks");
}
