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
        }
    }
}
