using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuestionService.Data.Models.Requests;
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
        private readonly IMCQuestionsService mCQuestionsService;

        public QuestionController(INumberQuestionsService numberQuestionsService, IMCQuestionsService mCQuestionsService)
        {
            this.numberQuestionsService = numberQuestionsService;
            this.mCQuestionsService = mCQuestionsService;
        }

        [HttpGet]
        public IActionResult PingService()
        {
            try
            {
                return Ok("Successfully contacted ConQuiz question service. Version 1.1");
            }
            catch(Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize]
        [HttpPost("number")]
        public async Task<IActionResult> CreateNumberQuestion([FromBody] CreateNumberQuestionRequest request)
        {
            try
            {
                await numberQuestionsService.AddNumberQuestion(request);
                return Ok(new { message = "Successfully submitted a number question! We will review it and if it follows our guidelines it will be added to ConQuiz!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize]
        [HttpPost("multiple")]
        public async Task<IActionResult> CreateMultipleChoiceQuestion([FromBody] CreateMultipleChoiceQuestionRequest request)
        {
            try
            {
                await mCQuestionsService.CreateMultipleChoiceQuestion(request);

                return Ok(new { message = "Successfully submitted a multiple choice question! We will review it and if it follows our guidelines it will be added to ConQuiz!" });
            }
            catch(Exception ex)
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
