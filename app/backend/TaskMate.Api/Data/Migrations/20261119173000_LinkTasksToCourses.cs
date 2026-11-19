using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TaskMate.Api.Data;

#nullable disable

namespace TaskMate.Api.Data.Migrations;

[DbContext(typeof(TaskMateDbContext))]
[Migration("20261119173000_LinkTasksToCourses")]
public sealed class LinkTasksToCourses : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "CourseId",
            table: "Tasks",
            type: "TEXT",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_Tasks_CourseId",
            table: "Tasks",
            column: "CourseId");

        migrationBuilder.AddForeignKey(
            name: "FK_Tasks_Courses_CourseId",
            table: "Tasks",
            column: "CourseId",
            principalTable: "Courses",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(name: "FK_Tasks_Courses_CourseId", table: "Tasks");
        migrationBuilder.DropIndex(name: "IX_Tasks_CourseId", table: "Tasks");
        migrationBuilder.DropColumn(name: "CourseId", table: "Tasks");
    }
}
