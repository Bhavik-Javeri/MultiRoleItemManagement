using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System;
using System.Text.Json.Serialization;

namespace ItemManagement.Models.ViewModels
{
    public class OrderViewModel
    {
        [JsonPropertyName("orderId")]
        public Guid Id { get; set; }

        public Guid StoreId { get; set; }

        // This is the fix. Making these properties nullable (with '?')
        // tells the model validator not to require them on form submission.
        public string? StoreName { get; set; }
        public Guid UserId { get; set; }
        public string? UserName { get; set; }

        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public OrderStatus Status { get; set; }

        public List<OrderItemViewModel> Items { get; set; } = new List<OrderItemViewModel>();

        [Required(ErrorMessage = "Please enter your delivery address.")]
        public string? Address { get; set; }

        [Required(ErrorMessage = "Please enter your 6-digit pincode.")]
        public string Pincode { get; set; }
    }
}
