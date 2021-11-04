using AccountService;
using AutoMapper;
using GameService.Dtos;
using GameService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameService.Profiles
{
    public class GameProfile : Profile
    {
        public GameProfile()
        {
            CreateMap<UserPublishedDto, Users>()
                .ForMember(dest => dest.ExternalId, opt => opt.MapFrom(x => x.Id))
                .ForMember(dest => dest.Id, opt => opt.Ignore());
            CreateMap<GrpcAccountModel, Users>()
                .ForMember(dest => dest.ExternalId, opt => opt.MapFrom(x => x.AccountId))
                .ForMember(dest => dest.Username, opt => opt.MapFrom(x => x.Username))
                .ForMember(dest => dest.Id, opt => opt.Ignore());
            CreateMap<AnswerResponse, Answers>();
            CreateMap<QuestionResponse, Questions>();
        }
    }
}
