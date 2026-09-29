using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartGym.Api.Data;
using SmartGym.Api.DTOs.Common;

namespace SmartGym.Api.Controllers;

public class TrainerDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}".Trim();
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string Role { get; set; } = "Trainer";
    public string Specialization { get; set; } = "Fitness & Strength Conditioning";
    public string Bio { get; set; } = "Certified fitness instructor specializing in functional movement, HIIT and strength.";
    public int ActiveClassesCount { get; set; }
    public DateTime JoinedAt { get; set; }
}

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class TrainersController : ControllerBase
{
    private readonly SmartGymDbContext _context;

    public TrainersController(SmartGymDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<TrainerDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTrainers(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var trainerUsersQuery = _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Where(u => u.UserRoles.Any(ur => ur.Role.Name == "Trainer" || ur.Role.Name == "Admin"));

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            trainerUsersQuery = trainerUsersQuery.Where(u =>
                u.FirstName.ToLower().Contains(s) ||
                u.LastName.ToLower().Contains(s) ||
                u.Email.ToLower().Contains(s));
        }

        var total = await trainerUsersQuery.CountAsync(cancellationToken);

        var users = await trainerUsersQuery
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var trainerIds = users.Select(u => u.Id).ToList();

        var classCounts = await _context.ClassSchedules
            .Where(cs => trainerIds.Contains(cs.TrainerId))
            .GroupBy(cs => cs.TrainerId)
            .Select(g => new { TrainerId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TrainerId, x => x.Count, cancellationToken);

        var items = users.Select(u => new TrainerDto
        {
            Id = u.Id,
            FirstName = u.FirstName,
            LastName = u.LastName,
            Email = u.Email,
            PhoneNumber = u.PhoneNumber,
            Role = u.UserRoles.FirstOrDefault()?.Role.Name ?? "Trainer",
            Specialization = u.UserRoles.Any(r => r.Role.Name == "Admin") ? "Head Fitness Director" : "Certified Personal Trainer",
            Bio = "Professional SmartGym trainer dedicated to athletic progression and technique.",
            ActiveClassesCount = classCounts.TryGetValue(u.Id, out var count) ? count : 0,
            JoinedAt = u.CreatedAt
        }).ToList();

        return Ok(new PagedResult<TrainerDto>
        {
            Items = items,
            PageNumber = page,
            PageSize = pageSize,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling((double)total / pageSize)
        });
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TrainerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTrainerById(Guid id, CancellationToken cancellationToken)
    {
        var u = await _context.Users
            .Include(x => x.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (u == null)
            return NotFound(new { message = "Trainer not found" });

        var count = await _context.ClassSchedules
            .CountAsync(cs => cs.TrainerId == id, cancellationToken);

        return Ok(new TrainerDto
        {
            Id = u.Id,
            FirstName = u.FirstName,
            LastName = u.LastName,
            Email = u.Email,
            PhoneNumber = u.PhoneNumber,
            Role = u.UserRoles.FirstOrDefault()?.Role.Name ?? "Trainer",
            Specialization = u.UserRoles.Any(r => r.Role.Name == "Admin") ? "Head Fitness Director" : "Certified Personal Trainer",
            Bio = "Professional SmartGym trainer dedicated to athletic progression and technique.",
            ActiveClassesCount = count,
            JoinedAt = u.CreatedAt
        });
    }
}
