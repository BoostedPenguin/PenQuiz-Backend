using AutoMapper;
using QuestionService.Dtos;
using QuestionService.Models;
using QuestionService.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuestionService.Profiles
{
    public class QuestionProfile : Profile
    {
        public QuestionProfile()
        {
            // Org -> Dest
            CreateMap<Questions, QuestionResponse>();
            CreateMap<Answers, AnswerResponse>();
        }
    }
}
