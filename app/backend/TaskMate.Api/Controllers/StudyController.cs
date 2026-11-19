using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskMate.Api.Contracts;
using TaskMate.Api.Data;
using TaskMate.Api.Domain;
namespace TaskMate.Api.Controllers;
[ApiController, Authorize, Route("api/study")]
public sealed class StudyController(TaskMateDbContext db, TimeProvider clock) : ControllerBase
{
 [HttpGet("sessions")]
 public async Task<ActionResult<IReadOnlyList<StudySessionResponse>>> GetSessions([FromQuery] int limit=50, CancellationToken cancellationToken=default)
 {
  var uid=UserId(); if(uid is null)return Unauthorized();
  if(limit is <1 or >100)return BadRequest(new ProblemDetails{Title="Limit must be between 1 and 100.",Status=400});
  var rows=await db.StudySessions.AsNoTracking().Where(x=>x.UserId==uid).OrderByDescending(x=>x.StartedAtUtc).Take(limit).ToListAsync(cancellationToken);
  return Ok(rows.Select(Map).ToArray());
 }
 [HttpGet("summary")]
 public async Task<ActionResult<StudySummaryResponse>> GetSummary(CancellationToken cancellationToken)
 {
  var uid=UserId();if(uid is null)return Unauthorized();
  var now=clock.GetUtcNow().UtcDateTime;
  var week=DateTime.SpecifyKind(now.Date.AddDays(-(((int)now.DayOfWeek+6)%7)),DateTimeKind.Utc);
  var month=DateTime.SpecifyKind(new DateTime(now.Year,now.Month,1),DateTimeKind.Utc);
  var q=db.StudySessions.AsNoTracking().Where(x=>x.UserId==uid);
  return Ok(new StudySummaryResponse(await q.CountAsync(cancellationToken),await q.SumAsync(x=>(int?)x.DurationMinutes,cancellationToken)??0,
   await q.Where(x=>x.StartedAtUtc>=week).SumAsync(x=>(int?)x.DurationMinutes,cancellationToken)??0,
   await q.Where(x=>x.StartedAtUtc>=month).SumAsync(x=>(int?)x.DurationMinutes,cancellationToken)??0));
 }
 [HttpPost("sessions")]
 public async Task<ActionResult<StudySessionResponse>> CreateSession(CreateStudySessionRequest request,CancellationToken cancellationToken)
 {
  var uid=UserId();if(uid is null)return Unauthorized();
  var now=clock.GetUtcNow().UtcDateTime;
  var start=DateTime.SpecifyKind(request.StartedAtUtc,DateTimeKind.Utc);var end=DateTime.SpecifyKind(request.EndedAtUtc,DateTimeKind.Utc);
  var duration=end-start;
  if(start>now.AddMinutes(5)||end>now.AddMinutes(5)||duration<TimeSpan.FromMinutes(1)||duration>TimeSpan.FromHours(4))
   return BadRequest(new ProblemDetails{Title="Invalid study session.",Detail="A session must last 1–240 minutes and cannot be in the future.",Status=400});
  if(request.CourseId is Guid c && !await db.Courses.AnyAsync(x=>x.Id==c&&x.UserId==uid,cancellationToken))
   return BadRequest(new ProblemDetails{Title="Course not found for this user.",Status=400});
  if(request.TaskId is Guid t && !await db.Tasks.AnyAsync(x=>x.Id==t&&x.UserId==uid,cancellationToken))
   return BadRequest(new ProblemDetails{Title="Task not found for this user.",Status=400});
  var row=new StudySession{UserId=uid.Value,CourseId=request.CourseId,TaskId=request.TaskId,StartedAtUtc=start,EndedAtUtc=end,
   DurationMinutes=(int)Math.Round(duration.TotalMinutes,MidpointRounding.AwayFromZero),Notes=string.IsNullOrWhiteSpace(request.Notes)?null:request.Notes.Trim(),CreatedAtUtc=now};
  db.StudySessions.Add(row);await db.SaveChangesAsync(cancellationToken);
  return CreatedAtAction(nameof(GetSessions),new{limit=50},Map(row));
 }
 private Guid? UserId()=>Guid.TryParse(User.FindFirst("sub")?.Value,out var id)?id:null;
 private static StudySessionResponse Map(StudySession x)=>new(x.Id,x.CourseId,x.TaskId,x.StartedAtUtc,x.EndedAtUtc,x.DurationMinutes,x.Notes,x.CreatedAtUtc);
}
