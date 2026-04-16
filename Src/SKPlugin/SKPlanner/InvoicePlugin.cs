using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace SKPlanner
{
    public class InvoicePlugin
    {
        [KernelFunction, Description("Extract structured data from an invoice by invoice ID")]
        public string ExtractInvoiceData([Description("Invoice ID")] string invoiceId)
        {
            return """
                {
                    "vendor": "Acme Corp",
                    "amount": 4500.00,
                    "due_date": "2025-04-15",
                    "line_items": ["Cloud Servers x5", "Support Plan"]
                }
                """;
        }

        [KernelFunction, Description("Validate if a vendor is approved in our system")]
        public string ValidateVendor([Description("Vendor name")] string vendorName)
        => vendorName == "Acme Corp" ? "APPROVED - Net 30 terms" : "REJECTED - Not in vendor list";

        [KernelFunction, Description("Post a validated invoice to the accounting system")]
        public string PostToAccounting(
        [Description("Vendor name")] string vendor,
        [Description("Invoice amount")] decimal amount)
        => $"Posted to accounting. GL Code: 6200-OPEX. Payment scheduled for {DateTime.Now.AddDays(30):d}";

        [KernelFunction, Description("Send email confirmation of invoice processing")]
        public string SendConfirmation(
        [Description("Recipient email")] string email,
        [Description("Summary of what was processed")] string summary)
        => $"Email sent to {email}: {summary}";

    }
}





