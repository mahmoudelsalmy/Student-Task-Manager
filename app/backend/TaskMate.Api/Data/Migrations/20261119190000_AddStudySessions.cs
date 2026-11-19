using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TaskMate.Api.Data;
#nullable disable
namespace TaskMate.Api.Data.Migrations;
[DbContext(typeof(TaskMateDbContext))]
[Migration("20261119190000_AddStudySessions")]
public sealed class AddStudySessions : Migration
{
 protected override void Up(MigrationBuilder m)
 {
  m.CreateTable(name:"StudySessions",columns:t=>new{
   Id=t.Column<Guid>(type:"TEXT",nullable:false),UserId=t.Column<Guid>(type:"TEXT",nullable:false),
   CourseId=t.Column<Guid>(type:"TEXT",nullable:true),TaskId=t.Column<Guid>(type:"TEXT",nullable:true),
   StartedAtUtc=t.Column<DateTime>(type:"TEXT",nullable:false),EndedAtUtc=t.Column<DateTime>(type:"TEXT",nullable:false),
   DurationMinutes=t.Column<int>(type:"INTEGER",nullable:false),Notes=t.Column<string>(type:"TEXT",maxLength:1000,nullable:true),
   CreatedAtUtc=t.Column<DateTime>(type:"TEXT",nullable:false)
  },constraints:t=>{
   t.PrimaryKey("PK_StudySessions",x=>x.Id);
   t.ForeignKey("FK_StudySessions_Users_UserId",x=>x.UserId,"Users","Id",onDelete:ReferentialAction.Cascade);
   t.ForeignKey("FK_StudySessions_Courses_CourseId",x=>x.CourseId,"Courses","Id",onDelete:ReferentialAction.SetNull);
   t.ForeignKey("FK_StudySessions_Tasks_TaskId",x=>x.TaskId,"Tasks","Id",onDelete:ReferentialAction.SetNull);
  });
  m.CreateIndex("IX_StudySessions_UserId_StartedAtUtc","StudySessions",new[]{"UserId","StartedAtUtc"});
  m.CreateIndex("IX_StudySessions_CourseId","StudySessions","CourseId");
  m.CreateIndex("IX_StudySessions_TaskId","StudySessions","TaskId");
 }
 protected override void Down(MigrationBuilder m)=>m.DropTable("StudySessions");
}
