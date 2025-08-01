using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ItemManagement.Model.DTO;

namespace ItemManagement.Interface
{
    public interface IReportService
    {
        // MODIFIED: Changed 'date' to 'startDate' and 'endDate'
        Task<List<DailyOrderReportDto>> GetDailyOrderReportAsync(DateTime startDate, DateTime endDate, Guid? storeId);
        Task<List<DailyItemTransactionReportDto>> GetDailyItemTransactionReportAsync(DateTime startDate, DateTime endDate, Guid? storeId);
        Task<List<IndividualOrderReportDto>> GetIndividualOrdersReportAsync(DateTime startDate, DateTime endDate, Guid? storeId);
    }
}