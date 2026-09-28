using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartGym.Api.Authorization;
using SmartGym.Api.DTOs.Reporting;
using SmartGym.Api.Services;

namespace SmartGym.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Produces("application/json")]
[Authorize(Policy = AppPolicies.RequireStaff)]
public class ReportsController : ControllerBase
{
    private readonly IReportingService _reportingService;

    public ReportsController(IReportingService reportingService)
    {
        _reportingService = reportingService;
    }

    [HttpGet("membership")]
    [ProducesResponseType(typeof(MembershipReportDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMembershipReport(CancellationToken cancellationToken = default)
    {
        var report = await _reportingService.GetMembershipReportAsync(cancellationToken);
        return Ok(report);
    }

    [HttpGet("classes")]
    [ProducesResponseType(typeof(ClassesReportDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetClassesReport(CancellationToken cancellationToken = default)
    {
        var report = await _reportingService.GetClassesReportAsync(cancellationToken);
        return Ok(report);
    }

    [HttpGet("inventory")]
    [ProducesResponseType(typeof(InventoryReportDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInventoryReport(CancellationToken cancellationToken = default)
    {
        var report = await _reportingService.GetInventoryReportAsync(cancellationToken);
        return Ok(report);
    }

    [HttpGet("facility")]
    [ProducesResponseType(typeof(FacilityReportDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFacilityReport(CancellationToken cancellationToken = default)
    {
        var report = await _reportingService.GetFacilityReportAsync(cancellationToken);
        return Ok(report);
    }

    [HttpGet("ai")]
    [ProducesResponseType(typeof(AiReportDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAiReport(CancellationToken cancellationToken = default)
    {
        var report = await _reportingService.GetAiReportAsync(cancellationToken);
        return Ok(report);
    }

    [HttpGet("executive-dashboard")]
    [ProducesResponseType(typeof(ExecutiveDashboardDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetExecutiveDashboard(CancellationToken cancellationToken = default)
    {
        var dashboard = await _reportingService.GetExecutiveDashboardAsync(cancellationToken);
        return Ok(dashboard);
    }
}
