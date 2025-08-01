namespace ItemManagement.Models.ViewModels
{
    public class OrderApprovalRequest
    {
        public Guid Id { get; set; }
        public OrderStatus NewStatus { get; set; }
        public string Comments { get; set; }
    }
} 