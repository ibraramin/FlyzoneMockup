using Flyzone.Models;

namespace Flyzone.ViewModels
{
    public class RecentActivityItem
    {
        public string ApplicationId { get; set; } = null!;
        public string ApplicationType { get; set; } = null!;
        public DateTime DateSubmitted { get; set; }
        public string Status { get; set; } = null!;
    }

    public class ActiveApplicationInfo
    {
        public string ApplicationId { get; set; } = null!;
        public string ServiceName { get; set; } = null!;
        public ApplicationStatus Status { get; set; }
    }

    public class DashboardViewModel
    {
        public string FirstName { get; set; } = null!;
        
        public int TotalApplications { get; set; }
        public int InProgress { get; set; }
        public int ActionRequired { get; set; }
        public int Completed { get; set; }
        
        public List<RecentActivityItem> RecentActivity { get; set; } = new();
        
        public ActiveApplicationInfo? CurrentActiveApplication { get; set; }
    }
}