using AccountService;
using AutoMapper;
using GameService.Data.Models;
using GameService.Dtos;
using GameService.Dtos.SignalR_Responses;
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
                .ForMember(dest => dest.UserGlobalIdentifier, opt => opt.MapFrom(x => x.UserGlobalIdentifier))
                .ForMember(dest => dest.Id, opt => opt.Ignore());
            CreateMap<GrpcAccountModel, Users>()
                .ForMember(dest => dest.UserGlobalIdentifier, opt => opt.MapFrom(x => x.UserGlobalIdentifier))
                .ForMember(dest => dest.Username, opt => opt.MapFrom(x => x.Username))
                .ForMember(dest => dest.Id, opt => opt.Ignore());
            CreateMap<AnswerResponse, Answers>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.QuestionId, opt => opt.Ignore());
            CreateMap<QuestionResponse, Questions>()
                .ForMember(dest => dest.RoundId, org => org.MapFrom(x => x.RoundId))
                .ForMember(dest => dest.Id, org => org.Ignore());
            CreateMap<Answers, AnswerClientResponse>();
            CreateMap<Questions, QuestionClientResponse>();
        }
    }
}
