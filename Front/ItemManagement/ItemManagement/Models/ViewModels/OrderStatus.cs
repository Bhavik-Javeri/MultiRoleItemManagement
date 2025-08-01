namespace ItemManagement.Models.ViewModels
{
    public enum OrderStatus
    {
        Pending = 0,    // Initial state when order is placed
        Approved = 1,   // Order approved by Store Admin
        //Accepted = 1,   // Order accepted and processed
        Rejected = 2,   // Order rejected by Store Admin
        //Cancelled = 4   // Order cancelled by user or system
    }
} 