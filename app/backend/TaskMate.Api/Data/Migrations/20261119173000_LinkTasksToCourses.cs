using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TaskMate.Api.Data;

#nullable disable

namespace TaskMate.Api.Data.Migrations;

[DbContext(typeof(TaskMateDbContext))]
[Migration("20261119173000_LinkTasksToCourses")]
public sealed partial class LinkTasksToCourses : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Tasks_new",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                Title = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                Description = table.Column<string>(type: "TEXT", maxLength: 5000, nullable: true),
                DueDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                Priority = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                Status = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                Category = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                CourseId = table.Column<Guid>(type: "TEXT", nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Tasks", x => x.Id);
                table.ForeignKey("FK_Tasks_Users_UserId", x => x.UserId, "Users", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_Tasks_Courses_CourseId", x => x.CourseId, "Courses", "Id", onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.Sql("INSERT INTO Tasks_new (Id, UserId, Title, Description, DueDate, Priority, Status, Category, CreatedAtUtc, UpdatedAtUtc) SELECT Id, UserId, Title, Description, DueDate, Priority, Status, Category, CreatedAtUtc, UpdatedAtUtc FROM Tasks;");
        migrationBuilder.DropTable(name: "Tasks");
        migrationBuilder.RenameTable(name: "Tasks_new", newName: "Tasks");
        migrationBuilder.CreateIndex(name: "IX_Tasks_UserId_DueDate", table: "Tasks", columns: new[] { "UserId", "DueDate" });
        migrationBuilder.CreateIndex(name: "IX_Tasks_UserId_Status", table: "Tasks", columns: new[] { "UserId", "Status" });
        migrationBuilder.CreateIndex(name: "IX_Tasks_CourseId", table: "Tasks", column: "CourseId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Tasks_old",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                Title = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                Description = table.Column<string>(type: "TEXT", maxLength: 5000, nullable: true),
                DueDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                Priority = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                Status = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                Category = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Tasks", x => x.Id);
                table.ForeignKey("FK_Tasks_Users_UserId", x => x.UserId, "Users", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.Sql("INSERT INTO Tasks_old (Id, UserId, Title, Description, DueDate, Priority, Status, Category, CreatedAtUtc, UpdatedAtUtc) SELECT Id, UserId, Title, Description, DueDate, Priority, Status, Category, CreatedAtUtc, UpdatedAtUtc FROM Tasks;");
        migrationBuilder.DropTable(name: "Tasks");
        migrationBuilder.RenameTable(name: "Tasks_old", newName: "Tasks");
        migrationBuilder.CreateIndex(name: "IX_Tasks_UserId_DueDate", table: "Tasks", columns: new[] { "UserId", "DueDate" });
        migrationBuilder.CreateIndex(name: "IX_Tasks_UserId_Status", table: "Tasks", columns: new[] { "UserId", "Status" });
    }
}
