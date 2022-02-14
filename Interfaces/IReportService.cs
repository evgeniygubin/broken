using BrokenCode.Etc;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace BrokenCode.Interfaces
{
    public interface IReportService
    {
        Task<IActionResult> GetReport(GetReportRequest request);
    }
}
