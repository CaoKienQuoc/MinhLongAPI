using Microsoft.AspNetCore.Mvc;
using Repo.IRepository;
using Services.IService;

namespace MLHR.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AgencyScoreController : ControllerBase
    {
        private readonly IAgencyScoreService _scoreService;

        public AgencyScoreController(IAgencyScoreService scoreService)
        {
            _scoreService = scoreService;
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddScore(long agencyId, int score, string reason)
        {
            await _scoreService.AddScoreAsync(agencyId, score, reason);
            return Ok("Đã cộng điểm tín nhiệm.");
        }
    }

}
