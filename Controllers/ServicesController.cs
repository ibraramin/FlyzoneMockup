using Flyzone.Data;
using Flyzone.Models;
using Flyzone.Models.Forms;
using Flyzone.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.IO;

namespace Flyzone.Controllers
{
    [Authorize]
    [EnableRateLimiting("FormSubmitLimit")]
    public class ServicesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ServicesController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // --- GOLDEN VISA FORM ---
        [HttpGet]
        public IActionResult GoldenVisa()
        {
            return View(new GoldenVisaViewModel());
        }

        [HttpPost]
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

            var application = new ServiceApplication
            {
                UserId = userId,
                ServiceId = 1,
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

            return RedirectToAction("Dashboard", "Account");
        }

        // --- DRIVING LICENSE FORM ---
        [HttpGet]
        public IActionResult DrivingLicense()
        {
            return View(new DrivingLicenseViewModel());
        }

        [HttpPost]
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

            var application = new ServiceApplication
            {
                UserId = userId,
                ServiceId = 2,
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

            return RedirectToAction("Dashboard", "Account");
        }

        // --- SECURE FILE UPLOAD ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadDocument(IFormFile file, string documentType, string? applicationId)
        {
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

            // Allowed extensions
            var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
            var allowedMimeTypes = new[] { "application/pdf", "image/jpeg", "image/png" };

            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension))
            {
                return BadRequest(new { error = "Invalid file type. Allowed: PDF, JPG, PNG." });
            }

            // Validate MIME type by reading file signature (magic bytes)
            using var stream = file.OpenReadStream();
            var headerBytes = new byte[8];
            stream.Read(headerBytes, 0, 8);

            if (!IsValidSignature(headerBytes, fileExtension))
            {
                return BadRequest(new { error = "Invalid file content." });
            }

            // Check actual MIME type
            var contentType = file.ContentType.ToLowerInvariant();
            if (!allowedMimeTypes.Contains(contentType))
            {
                return BadRequest(new { error = "Invalid MIME type." });
            }

            // Rename to secure GUID
            var safeFileName = $"{Guid.NewGuid()}{fileExtension}";
            var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            var filePath = Path.Combine(uploadPath, safeFileName);
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            // Save to database
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var document = new UserDocument
            {
                UserId = userId,
                DocumentType = documentType,
                FilePath = $"/uploads/{safeFileName}",
                UploadedAt = DateTime.UtcNow
            };

            if (!string.IsNullOrEmpty(applicationId))
            {
                document.ServiceApplicationId = applicationId;
            }

            _context.UserDocuments.Add(document);
            await _context.SaveChangesAsync();

            return Ok(new { message = "File uploaded successfully.", filePath = document.FilePath });
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