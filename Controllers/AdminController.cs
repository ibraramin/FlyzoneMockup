using Flyzone.Data;
using Flyzone.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Flyzone.Controllers;

/// <summary>
/// Minimal officer queue: list applications, change status and edit internal notes.
/// Every transition appends an ApplicationHistory row in the same transaction.
/// </summary>
[Authorize(Roles = DemoDataSeeder.AdminRole)]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;

    public AdminController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? status)
    {
        ApplicationStatus? filter = null;
        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse(status, ignoreCase: true, out ApplicationStatus parsed))
        {
            filter = parsed;
        }

        var query = _context.ServiceApplications
            .AsNoTracking()
            .Include(a => a.Service)
            .Include(a => a.User)
            .Include(a => a.StatusHistory)
            .AsSplitQuery();

        if (filter.HasValue)
        {
            query = query.Where(a => a.Status == filter.Value);
        }

        var applications = await query
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        ViewData["StatusFilter"] = status;
        return View(applications);
    }

    [HttpGet]
    public async Task<IActionResult> Details(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return NotFound();
        }

        var application = await _context.ServiceApplications
            .AsNoTracking()
            .Include(a => a.Service)
            .Include(a => a.User)
            .Include(a => a.StatusHistory.OrderByDescending(h => h.Timestamp))
            .Include(a => a.AttachedDocuments)
            .Include(a => a.GoldenVisaDetails)
            .Include(a => a.DrivingLicenseDetails)
            .AsSplitQuery()
            .FirstOrDefaultAsync(a => a.Id == id);

        return application == null ? NotFound() : View(application);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(
        string id,
        string? status,
        string? internalNotes,
        string? comment)
    {
        // Bind the raw string: a non-numeric value would otherwise model-bind to
        // the enum default (Submitted) and silently commit a bogus transition.
        if (string.IsNullOrWhiteSpace(status) ||
            !Enum.TryParse(status, ignoreCase: true, out ApplicationStatus parsed) ||
            !Enum.IsDefined(parsed))
        {
            return BadRequest(new { error = "Unknown status." });
        }

        using var transaction = await _context.Database.BeginTransactionAsync();

        var application = await _context.ServiceApplications
            .Include(a => a.StatusHistory)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (application == null)
        {
            return NotFound();
        }

        application.Status = parsed;
        application.UpdatedAt = DateTime.UtcNow;
        application.InternalNotes = string.IsNullOrWhiteSpace(internalNotes)
            ? null
            : internalNotes.Trim();

        application.StatusHistory.Add(new ApplicationHistory
        {
            ServiceApplicationId = application.Id,
            StatusState = parsed,
            Comments = string.IsNullOrWhiteSpace(comment)
                ? $"Status changed to {parsed}"
                : comment.Trim(),
            Timestamp = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return RedirectToAction(nameof(Details), new { id });
    }
}
