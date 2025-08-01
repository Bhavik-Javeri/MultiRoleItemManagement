namespace ItemManagement.Model.DTO
{
    public class DashboardCountsDto
    {
        public int TotalUsers { get; set; }
        public int? TotalStores { get; set; } // Only for SuperAdmin
        public int TotalItems { get; set; }
        public int TotalOrders { get; set; }
    }
} 