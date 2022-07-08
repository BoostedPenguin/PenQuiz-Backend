using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
        private readonly IHttpContextAccessor httpContextAccessor;

        public QuestionController(INumberQuestionsService numberQuestionsService, IMCQuestionsService mCQuestionsService, IHttpContextAccessor httpContextAccessor)
        {
            this.numberQuestionsService = numberQuestionsService;
            this.mCQuestionsService = mCQuestionsService;
            this.httpContextAccessor = httpContextAccessor;
        }

        [HttpGet]
        public IActionResult PingService()
        {
            try
            {
                return Ok("Successfully contacted PenQuiz question service. Version 1.1");
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
                // Get Claim
                var userRole = httpContextAccessor.GetCurrentUserRole();
                var userName = httpContextAccessor.GetCurrentUserName();

                await numberQuestionsService.AddNumberQuestion(request, userName, userRole);
                return Ok(new { message = "Successfully submitted a number question! We will review it and if it follows our guidelines it will be added to PenQuiz!" });
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
                // Get Claim
                var userRole = httpContextAccessor.GetCurrentUserRole();
                var userName = httpContextAccessor.GetCurrentUserName();

                await mCQuestionsService.CreateMultipleChoiceQuestion(request, userName, userRole);

                return Ok(new { message = "Successfully submitted a multiple choice question! We will review it and if it follows our guidelines it will be added to PenQuiz!" });
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
