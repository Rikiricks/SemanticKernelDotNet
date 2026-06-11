using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace SKPlugin
{
    public class OrderPlugin
    {
        // [KernelFunction] exposes this method to the LLM as a callable tool
        // [Description] is what the LLM reads to decide WHEN to call this method

        [KernelFunction, Description("Get the status of a customer order by order ID")]
        public string OrderStatus([Description("The unique order ID, e.g. ORD-1234")] string orderId)
        {
            return orderId switch
            {
                "ORD-1234" => "Shipped - Expected delivery: Tomorrow by 5 PM",
                "ORD-5678" => "Processing - Will ship within 24 hours",
                _ => $"Order {orderId} not found in our system"
            };

        }

        [KernelFunction, Description("Create a support ticket for a customer issue")]
        public string CreateTicket([Description("Customer's name")] string customerName,
                                   [Description("Description of the issue")] string issue)
        {
            var ticketId = $"TKT-{Random.Shared.Next(1000, 9999)}";
            // In real app: persist to your CRM/ticketing system
            return $"Ticket {ticketId} created for {customerName}. " +
                   $"Our team will respond within 2 hours.";
        }

    }
}
