using EduDev_Tracker.Features.Analytics.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace EduDev_Tracker.Services.Analytics
{
    public interface IAnalyticsService
    {
        Task<AnalyticsReport> BuildReportAsync(int profileId, DateTime from, DateTime to,
            ReportModule module = ReportModule.All);
        Task<string> ExportPdfAsync(AnalyticsReport report);
        Task<string> ExportCsvAsync(AnalyticsReport report);
        Task<string> ExportExcelAsync(AnalyticsReport report);
    }
}
