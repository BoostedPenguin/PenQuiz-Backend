using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuestionService.Data.Models.Requests;
using QuestionService.Services;
using System;
using System.Threading.Tasks;

namespace QuestionService.Controllers
{
    [ApiController]
    [Authorize(Roles = "admin")]
    [Route("api/[controller]")]
    public class QuestionAdminController : ControllerBase
    {
        private readonly IAdminQuestionCrudService service;

        public QuestionAdminController(IAdminQuestionCrudService service)
        {
            this.service = service;
        }

        [Authorize(Roles = "admin")]
        [HttpPost("verify")]
        public async Task<IActionResult> VerifyQuestion(VerifyQuestionRequest request)
        {
            try
            {
                await service.VerifyQuestion(request);

                return Ok(new { message = "Successfully verified question!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize(Roles = "admin")]
        [HttpGet]
        public async Task<IActionResult> GetUnverifiedQuestions([FromQuery] int pageNumber, [FromQuery] int pageEntries)
        {
            try
            {
                var questions = await service.GetUnverifiedQuestions(pageNumber, pageEntries);

                return Ok(questions);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
