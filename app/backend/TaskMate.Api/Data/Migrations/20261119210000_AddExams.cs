using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TaskMate.Api.Data;

#nullable disable

namespace TaskMate.Api.Data.Migrations;

[DbContext(typeof(TaskMateDbContext))]
[Migration("20261119210000_AddExams")]
public sealed class AddExams : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Exams",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                CourseId = table.Column<Guid>(type: "TEXT", nullable: true),
                Title = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                ScheduledAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                DurationMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                Location = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Exams", exam => exam.Id);
                table.ForeignKey("FK_Exams_Users_UserId", exam => exam.UserId, "Users", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_Exams_Courses_CourseId", exam => exam.CourseId, "Courses", "Id", onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateIndex("IX_Exams_UserId_ScheduledAtUtc", "Exams", new[] { "UserId", "ScheduledAtUtc" });
        migrationBuilder.CreateIndex("IX_Exams_CourseId", "Exams", "CourseId");
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable("Exams");
}

