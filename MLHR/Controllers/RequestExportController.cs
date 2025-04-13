using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.IService;

namespace MLHR.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RequestExportController : ControllerBase
    {
        private readonly IRequestExportService _requestExportService;

        public RequestExportController(IRequestExportService requestExportService)
        {
            _requestExportService = requestExportService;
        }

        // ✅ API GET: Lấy danh sách RequestExport kèm RequestExportDetail
        [HttpGet("all")]
        //[Authorize(Roles = "3, 4")]
        public async Task<IActionResult> GetAllRequestExports([FromQuery] string? sortBy)
        {
            var requestExports = await _requestExportService.GetAllRequestExportsAsync(sortBy);
            return Ok(requestExports);
        }

        [HttpGet("{requestExportId}")]
        //[Authorize(Roles = "3, 4")]
        public async Task<IActionResult> GetRequestExportByID(int requestExportId)
        {
            var requestExports = await _requestExportService.GetRequestExportByIdAsync(requestExportId);
            return Ok(requestExports);
        }
    }
}
