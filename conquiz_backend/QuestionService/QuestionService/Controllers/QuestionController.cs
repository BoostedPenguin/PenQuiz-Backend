using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuestionService.Models.Requests;
using QuestionService.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuestionService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class QuestionController : ControllerBase
    {
        private readonly INumberQuestionsService numberQuestionsService;

        public QuestionController(INumberQuestionsService numberQuestionsService)
        {
            this.numberQuestionsService = numberQuestionsService;
        }

        [HttpGet]
        public IActionResult PingService()
        {
            try
            {
                return Ok("Successfully contacted ConQuiz question service. Version 1.0");
            }
            catch(Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("number")]
        public async Task<IActionResult> CreateNumberQuestion([FromBody] CreateNumberQuestionRequest request)
        {
            try
            {
                await numberQuestionsService.AddNumberQuestion(request.Question, request.Answer);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{questionId}")]
        public async Task<IActionResult> RemoveExistingQuestion([FromRoute]int questionId)
        {
            try
            {
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
