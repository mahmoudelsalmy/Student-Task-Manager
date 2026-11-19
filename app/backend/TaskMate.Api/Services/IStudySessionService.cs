using TaskMate.Api.Contracts;

namespace TaskMate.Api.Services;

public sealed record StudySessionCreateResult(StudySessionResponse? Session, string? Error)
{
    public bool Succeeded => Session is not null;
}

public interface IStudySessionService
{
    Task<IReadOnlyList<StudySessionResponse>> GetSessionsAsync(Guid userId, int limit, CancellationToken cancellationToken);
    Task<StudySummaryResponse> GetSummaryAsync(Guid userId, CancellationToken cancellationToken);
    Task<StudySessionCreateResult> CreateSessionAsync(Guid userId, CreateStudySessionRequest request, CancellationToken cancellationToken);
}

