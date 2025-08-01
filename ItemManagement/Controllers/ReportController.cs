using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ClosedXML.Excel;
using ItemManagement.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ItemManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "SuperAdmin,StoreAdmin")]
    public class ReportController : ControllerBase
    {
        private readonly IReportService _reportService;
        public ReportController(IReportService reportService)
        {
            _reportService = reportService;
        }

        // MODIFIED: Changed 'date' to 'startDate' and 'endDate'
        [HttpGet("download-daily-report")]
        public async Task<IActionResult> DownloadDailyReport(
            [FromQuery] DateTime startDate, // New parameter
            [FromQuery] DateTime endDate,   // New parameter
            [FromQuery] Guid? storeId)      // Existing parameter
        {
            Guid? storeIdToFilter = null; // This will hold the final storeId to pass to the service

            // If the user is a StoreAdmin, get their StoreId from the token claims.
            if (User.IsInRole("StoreAdmin"))
            {
                var storeIdClaim = User.FindFirst("StoreId")?.Value;
                if (string.IsNullOrEmpty(storeIdClaim) || !Guid.TryParse(storeIdClaim, out Guid parsedStoreId))
                {
                    return Unauthorized("Store ID claim is missing or invalid for Store Admin.");
                }
                storeIdToFilter = parsedStoreId;
            }
            else if (User.IsInRole("SuperAdmin"))
            {
                // For SuperAdmin, use the storeId passed from the query string.
                // If storeId is null (e.g., "All Stores" was selected), it will remain null,
                // and the service will return data for all stores.
                storeIdToFilter = storeId;
            }

            // Get the report data from the service.
            // Pass the determined storeIdToFilter AND the new startDate/endDate.
            var reportData = await _reportService.GetDailyItemTransactionReportAsync(startDate, endDate, storeIdToFilter); // MODIFIED LINE

            // Use ClosedXML to create the Excel file.
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Sales Report"); // Changed sheet name for range report

                // Add headers
                worksheet.Cell("A1").Value = "Date";
                worksheet.Cell("B1").Value = "Store Name";
                worksheet.Cell("C1").Value = "Item Name";
                worksheet.Cell("D1").Value = "Quantity Sold";
                worksheet.Cell("E1").Value = "Total Sales";

                // Style the header
                var headerRange = worksheet.Range("A1:E1");
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // Add data from the report
                int currentRow = 2;
                foreach (var item in reportData)
                {
                    worksheet.Cell(currentRow, 1).Value = item.Date.ToShortDateString(); // Still using item.Date, which is startDate.Date from service
                    worksheet.Cell(currentRow, 2).Value = item.StoreName;
                    worksheet.Cell(currentRow, 3).Value = item.ItemName;
                    worksheet.Cell(currentRow, 4).Value = item.QuantitySold;
                    worksheet.Cell(currentRow, 5).Value = item.TotalSales;
                    worksheet.Cell(currentRow, 5).Style.NumberFormat.Format = "₹ #,##0.00"; // Currency format
                    currentRow++;
                }

                worksheet.Columns().AdjustToContents(); // Adjust column width to fit content

                // Save the workbook to a memory stream
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    // Generate a more descriptive filename for a date range report
                    var fileName = $"Sales_Report_{startDate:yyyy-MM-dd}_to_{endDate:yyyy-MM-dd}";

                    // Append store name/ID to filename if a specific store was filtered
                    if (storeIdToFilter.HasValue && reportData.Any())
                    {
                        var firstStoreName = reportData.FirstOrDefault()?.StoreName;
                        if (!string.IsNullOrEmpty(firstStoreName))
                        {
                            fileName += $"_{firstStoreName.Replace(" ", "_")}";
                        }
                        else
                        {
                            fileName += $"_{storeIdToFilter.Value.ToString().Substring(0, 8)}";
                        }
                    }
                    fileName += ".xlsx";

                    // Return the file for download
                    return File(
                        content,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        fileName);
                }
            }
        }

        [HttpGet("individual-orders")]
        public async Task<IActionResult> GetIndividualOrders(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] Guid? storeId)
        {
            Guid? storeIdToFilter = null;
            if (User.IsInRole("StoreAdmin"))
            {
                var storeIdClaim = User.FindFirst("StoreId")?.Value;
                if (string.IsNullOrEmpty(storeIdClaim) || !Guid.TryParse(storeIdClaim, out Guid parsedStoreId))
                {
                    return Unauthorized("Store ID claim is missing or invalid for Store Admin.");
                }
                storeIdToFilter = parsedStoreId;
            }
            else if (User.IsInRole("SuperAdmin"))
            {
                storeIdToFilter = storeId;
            }

            var orders = await _reportService.GetIndividualOrdersReportAsync(startDate, endDate, storeIdToFilter);
            return Ok(orders);
        }

        [HttpGet("download-individual-orders-excel")]
        public async Task<IActionResult> DownloadIndividualOrdersExcel(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] Guid? storeId)
        {
            Guid? storeIdToFilter = null;
            if (User.IsInRole("StoreAdmin"))
            {
                var storeIdClaim = User.FindFirst("StoreId")?.Value;
                if (string.IsNullOrEmpty(storeIdClaim) || !Guid.TryParse(storeIdClaim, out Guid parsedStoreId))
                {
                    return Unauthorized("Store ID claim is missing or invalid for Store Admin.");
                }
                storeIdToFilter = parsedStoreId;
            }
            else if (User.IsInRole("SuperAdmin"))
            {
                storeIdToFilter = storeId;
            }

            var orders = await _reportService.GetIndividualOrdersReportAsync(startDate, endDate, storeIdToFilter);

            using (var workbook = new ClosedXML.Excel.XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Individual Orders Report");
                // Add headers
                worksheet.Cell("A1").Value = "OrderId";
                worksheet.Cell("B1").Value = "User Name";
                worksheet.Cell("C1").Value = "Store Name";
                worksheet.Cell("D1").Value = "Order Date";
                worksheet.Cell("E1").Value = "Total Amount";
                worksheet.Cell("F1").Value = "Status";
                worksheet.Cell("G1").Value = "Address";
                worksheet.Cell("H1").Value = "Pincode";

                var headerRange = worksheet.Range("A1:H1");
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;
                headerRange.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;

                int currentRow = 2;
                foreach (var order in orders)
                {
                    worksheet.Cell(currentRow, 1).Value = order.OrderId.ToString();
                    worksheet.Cell(currentRow, 2).Value = order.UserName;
                    worksheet.Cell(currentRow, 3).Value = order.StoreName;
                    worksheet.Cell(currentRow, 4).Value = order.OrderDate.ToString("yyyy-MM-dd HH:mm");
                    worksheet.Cell(currentRow, 5).Value = order.TotalAmount;
                    worksheet.Cell(currentRow, 5).Style.NumberFormat.Format = "₹ #,##0.00";
                    worksheet.Cell(currentRow, 6).Value = order.Status.ToString();
                    worksheet.Cell(currentRow, 7).Value = order.Address;
                    worksheet.Cell(currentRow, 8).Value = order.Pincode;
                    currentRow++;
                }

                worksheet.Columns().AdjustToContents();

                using (var stream = new System.IO.MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    var fileName = $"IndividualOrders_{startDate:yyyy-MM-dd}_to_{endDate:yyyy-MM-dd}";
                    if (storeIdToFilter.HasValue && orders.Any())
                    {
                        var firstStoreName = orders.FirstOrDefault()?.StoreName;
                        if (!string.IsNullOrEmpty(firstStoreName))
                        {
                            fileName += $"_{firstStoreName.Replace(" ", "_")}";
                        }
                        else
                        {
                            fileName += $"_{storeIdToFilter.Value.ToString().Substring(0, 8)}";
                        }
                    }
                    fileName += ".xlsx";

                    return File(
                        content,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        fileName);
                }
            }
        }
    }
}