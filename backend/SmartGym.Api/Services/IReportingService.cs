using SmartGym.Api.DTOs.Reporting;

namespace SmartGym.Api.Services;

public interface IReportingService
{
    Task<MembershipReportDto> GetMembershipReportAsync(CancellationToken cancellationToken = default);
    Task<ClassesReportDto> GetClassesReportAsync(CancellationToken cancellationToken = default);
    Task<InventoryReportDto> GetInventoryReportAsync(CancellationToken cancellationToken = default);
    Task<FacilityReportDto> GetFacilityReportAsync(CancellationToken cancellationToken = default);
    Task<AiReportDto> GetAiReportAsync(CancellationToken cancellationToken = default);
    Task<ExecutiveDashboardDto> GetExecutiveDashboardAsync(CancellationToken cancellationToken = default);
}
