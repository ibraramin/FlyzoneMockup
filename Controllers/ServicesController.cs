using Flyzone.Data;
using Flyzone.Models;
using Flyzone.Models.Forms;
using Flyzone.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace Flyzone.Controllers
{
    [Authorize]
    public class ServicesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;

        public ServicesController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration)
        {
            _context = context;
            _userManager = userManager;
            _configuration = configuration;
        }

        // --- GOLDEN VISA FORM ---
        [HttpGet]
        public IActionResult GoldenVisa()
        {
            return View(new GoldenVisaViewModel());
        }

        [HttpPost]
        [EnableRateLimiting("FormSubmitLimit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GoldenVisa(GoldenVisaViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var serviceId = await ResolveServiceIdAsync("Golden Visa Application");

            // The application row generates the IDs used by its details and
            // history rows, so both saves must succeed or neither may.
            using var transaction = await _context.Database.BeginTransactionAsync();

            var application = new ServiceApplication
            {
                UserId = userId,
                ServiceId = serviceId,
                Status = ApplicationStatus.Submitted,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.ServiceApplications.Add(application);
            await _context.SaveChangesAsync();

            var goldenVisaForm = new GoldenVisaForm
            {
                ServiceApplicationId = application.Id,
                PassportNumber = model.PassportNumber,
                CurrentEmiratesId = model.CurrentEmiratesId,
                Profession = model.Profession,
                MonthlySalary = model.MonthlySalary,
                IsInvestorCategory = model.IsInvestorCategory
            };

            _context.GoldenVisaForms.Add(goldenVisaForm);

            var history = new ApplicationHistory
            {
                ServiceApplicationId = application.Id,
                StatusState = ApplicationStatus.Submitted,
                Comments = "Application Submitted",
                Timestamp = DateTime.UtcNow
            };

            _context.ApplicationHistories.Add(history);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return RedirectToAction("Dashboard", "Account");
        }

        // --- DRIVING LICENSE FORM ---
        [HttpGet]
        public IActionResult DrivingLicense()
        {
            return View(new DrivingLicenseViewModel());
        }

        [HttpPost]
        [EnableRateLimiting("FormSubmitLimit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DrivingLicense(DrivingLicenseViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var serviceId = await ResolveServiceIdAsync("Driving License Renewal");

            using var transaction = await _context.Database.BeginTransactionAsync();

            var application = new ServiceApplication
            {
                UserId = userId,
                ServiceId = serviceId,
                Status = ApplicationStatus.Submitted,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.ServiceApplications.Add(application);
            await _context.SaveChangesAsync();

            var drivingLicenseForm = new DrivingLicenseRenewalForm
            {
                ServiceApplicationId = application.Id,
                TrafficFileNumber = model.TrafficFileNumber,
                CurrentLicenseExpiry = model.CurrentLicenseExpiry,
                EyeTestCenterName = model.EyeTestCenterName
            };

            _context.DrivingLicenseRenewalForms.Add(drivingLicenseForm);

            var history = new ApplicationHistory
            {
                ServiceApplicationId = application.Id,
                StatusState = ApplicationStatus.Submitted,
                Comments = "Application Submitted",
                Timestamp = DateTime.UtcNow
            };

            _context.ApplicationHistories.Add(history);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return RedirectToAction("Dashboard", "Account");
        }

        private static readonly HashSet<string> AllowedDocumentTypes =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "Passport", "EmiratesId", "DrivingLicense", "ProofOfAddress", "Photo", "Other"
            };

        // --- SECURE FILE UPLOAD ---
        [HttpPost]
        [EnableRateLimiting("FormSubmitLimit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadDocument(IFormFile file, string documentType, string? applicationId)
        {
            // Authenticate and authorize before touching the filesystem.
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "No file selected." });
            }

            // Maximum file size: 5 MB
            const long maxFileSize = 5 * 1024 * 1024;
            if (file.Length > maxFileSize)
            {
                return BadRequest(new { error = "File size exceeds 5 MB limit." });
            }

            var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
            var allowedMimeTypes = new[] { "application/pdf", "image/jpeg", "image/png" };

            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension))
            {
                return BadRequest(new { error = "Invalid file type. Allowed: PDF, JPG, PNG." });
            }

            if (string.IsNullOrWhiteSpace(documentType) || !AllowedDocumentTypes.Contains(documentType))
            {
                return BadRequest(new { error = "Invalid document type." });
            }

            // Never let a caller attach a document to another user's application (IDOR).
            if (!string.IsNullOrWhiteSpace(applicationId))
            {
                var ownsApplication = await _context.ServiceApplications
                    .AnyAsync(a => a.Id == applicationId && a.UserId == userId);

                if (!ownsApplication)
                {
                    // JSON endpoint: return a real 403 instead of the cookie handler's redirect.
                    return StatusCode(StatusCodes.Status403Forbidden,
                        new { error = "You do not own this application." });
                }
            }

            // Validate MIME type by reading file signature (magic bytes)
            using var stream = file.OpenReadStream();
            var headerBytes = new byte[8];
            var bytesRead = await stream.ReadAsync(headerBytes.AsMemory(0, 8));
            if (bytesRead < 4 || !IsValidSignature(headerBytes, fileExtension))
            {
                return BadRequest(new { error = "Invalid file content." });
            }

            var contentType = file.ContentType.ToLowerInvariant();
            if (!allowedMimeTypes.Contains(contentType))
            {
                return BadRequest(new { error = "Invalid MIME type." });
            }

            // Rename to secure GUID
            var safeFileName = $"{Guid.NewGuid()}{fileExtension}";
            var uploadPath = ResolveUploadDirectory();

            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            var filePath = Path.Combine(uploadPath, safeFileName);
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            var document = new UserDocument
            {
                UserId = userId,
                DocumentType = documentType,
                FilePath = $"/uploads/{safeFileName}",
                UploadedAt = DateTime.UtcNow,
                ServiceApplicationId = string.IsNullOrWhiteSpace(applicationId) ? null : applicationId
            };

            _context.UserDocuments.Add(document);
            await _context.SaveChangesAsync();

            return Ok(new { message = "File uploaded successfully.", filePath = document.FilePath });
        }

        // --- APPLICANT FLOW ---

        [HttpGet]
        public IActionResult Upload()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> MyDocuments()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var documents = await _context.UserDocuments
                .AsNoTracking()
                .Where(d => d.UserId == userId)
                .OrderByDescending(d => d.UploadedAt)
                .ToListAsync();

            return View(documents);
        }

        [HttpGet]
        public async Task<IActionResult> DownloadDocument(string id)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var document = await _context.UserDocuments
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId);

            if (document == null)
            {
                return NotFound();
            }

            // The stored path is untrusted data: take only its file name and
            // resolve it inside the configured upload directory so uploads kept
            // outside wwwroot remain reachable only through this authorized action.
            var storedFileName = Path.GetFileName(document.FilePath);
            if (string.IsNullOrEmpty(storedFileName))
            {
                return NotFound();
            }

            var physicalPath = Path.GetFullPath(Path.Combine(ResolveUploadDirectory(), storedFileName));
            if (!System.IO.File.Exists(physicalPath))
            {
                return NotFound();
            }

            var contentType = Path.GetExtension(storedFileName).ToLowerInvariant() switch
            {
                ".pdf" => "application/pdf",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                _ => "application/octet-stream"
            };

            return PhysicalFile(physicalPath, contentType, storedFileName, enableRangeProcessing: false);
        }

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var application = await _context.ServiceApplications
                .AsNoTracking()
                .Include(a => a.Service)
                .Include(a => a.StatusHistory)
                .Include(a => a.AttachedDocuments)
                .AsSplitQuery()
                .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

            if (application == null)
            {
                return NotFound();
            }

            return View(application);
        }

        [HttpGet]
        public async Task<IActionResult> Track(string? reference)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var model = new TrackViewModel { Reference = reference };

            if (!string.IsNullOrWhiteSpace(reference))
            {
                var normalized = reference.Trim();
                model.Reference = normalized;

                // Exact reference only (case-insensitive); no partial disclosure.
                model.Application = await _context.ServiceApplications
                    .AsNoTracking()
                    .Include(a => a.Service)
                    .Include(a => a.StatusHistory)
                    .Include(a => a.AttachedDocuments)
                    .AsSplitQuery()
                    .FirstOrDefaultAsync(a => a.UserId == userId
                        && a.Id.ToLower() == normalized.ToLowerInvariant());
            }

            return View(model);
        }

        // Forms bind to the catalog row by name instead of a magic integer, so a
        // re-seeded or re-ordered catalog cannot silently attach the wrong service.
        private async Task<int> ResolveServiceIdAsync(string serviceName)
        {
            var id = await _context.TypingServices
                .AsNoTracking()
                .Where(s => s.ServiceName == serviceName && s.IsActive)
                .OrderBy(s => s.Id)
                .Select(s => s.Id)
                .FirstOrDefaultAsync();

            if (id == 0)
            {
                throw new InvalidOperationException($"Active catalog service '{serviceName}' was not found.");
            }

            return id;
        }

        private string ResolveUploadDirectory()
        {
            var configuredPath = _configuration["Uploads:Path"];
            return string.IsNullOrWhiteSpace(configuredPath)
                ? Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "uploads")
                : configuredPath;
        }

        // Helper method for magic bytes validation
        private bool IsValidSignature(byte[] header, string extension)
        {
            // PDF: %PDF (25 50 44 46)
            if (extension == ".pdf" && header[0] == 0x25 && header[1] == 0x50 && header[2] == 0x44 && header[3] == 0x46)
                return true;

            // JPEG: FF D8 FF
            if ((extension == ".jpg" || extension == ".jpeg") && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
                return true;

            // PNG: 89 50 4E 47
            if (extension == ".png" && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47)
                return true;

            return false;
        }
    }
}
