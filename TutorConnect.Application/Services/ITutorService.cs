using TutorConnect.Application.Features.Tutors.DTOs;

namespace TutorConnect.Application.Services
{
    public interface ITutorService
    {
        Task<TutorOwnerProfileResponse> GetOwnerProfileAsync(long tutorId, CancellationToken cancellationToken = default);
        Task<TutorOwnerProfileResponse> UpdateOwnerProfileAsync(long tutorId, TutorProfileUpdateRequest request, CancellationToken cancellationToken = default);
        Task<TutorOwnerProfileResponse> SubmitProfileAsync(long tutorId, CancellationToken cancellationToken = default);
        Task<TutorPublicProfileResponse> GetPublicProfileAsync(long tutorId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<TutorAdminProfileResponse>> GetAdminProfilesAsync(string? status, long adminId, CancellationToken cancellationToken = default);
        Task<TutorAdminProfileResponse> GetAdminProfileAsync(long tutorId, long adminId, CancellationToken cancellationToken = default);
        Task<TutorAdminProfileResponse> ReviewProfileAsync(long tutorId, long adminId, TutorApprovalUpdateRequest request, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<TutorSubjectResponse>> GetTutorSubjectsAsync(long tutorId, CancellationToken cancellationToken = default);
        Task<TutorSubjectResponse> CreateTutorSubjectAsync(long tutorId, TutorSubjectCreateRequest request, CancellationToken cancellationToken = default);
        Task<TutorSubjectResponse> UpdateTutorSubjectAsync(long tutorId, long tutorSubjectId, TutorSubjectUpdateRequest request, CancellationToken cancellationToken = default);
        Task<TutorSubjectResponse> SetTutorSubjectStatusAsync(long tutorId, long tutorSubjectId, TutorSubjectStatusRequest request, CancellationToken cancellationToken = default);
    }
}
