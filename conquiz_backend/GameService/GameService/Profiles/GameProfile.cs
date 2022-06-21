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


            CreateMap<GameInstance, GameInstanceResponse>();
            CreateMap<Round, RoundResponse>();
            CreateMap<Answers, AnswersResponse>();
            CreateMap<PvpRound, PvpRoundResponse>();
            CreateMap<CapitalRound, CapitalRoundResponse>();
            CreateMap<NeutralRound, NeutralRoundResponse>();
            CreateMap<AttackingNeutralTerritory, AttackingNeutralTerritoryResponse>();
            CreateMap<AttackingNeutralTerritory, AttackingNeutralTerritoryResponse>();

            CreateMap<Participants, ParticipantsResponse>()
                .ForMember(dest => dest.AvatarName, opt => opt.MapFrom(e => e.GameCharacter.Character.AvatarName));

            CreateMap<Users, UsersResponse>();
            CreateMap<ObjectTerritory, ObjectTerritoryResponse>();
            CreateMap<MapTerritory, MapTerritoryResponse>();

            CreateMap<Character, CharacterResponse>();

            CreateMap<GameCharacter, GameCharacterResponse>()
                .ForMember(dest => dest.GameInstanceId, opt => opt.MapFrom(e => e.Participant.GameId))
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(e => e.Participant.PlayerId));

            CreateMap<GameCharacterAbilities, GameCharacterAbilitiesResponse>()
                .ForMember(dest => dest.VikingCharacterAbilitiesResponse, 
                    opt => opt.MapFrom(e => e.GetType() == typeof(VikingCharacterAbilities) ? (VikingCharacterAbilities)e : null))
                .ForMember(dest => dest.WizardCharacterAbilitiesResponse,
                    opt => opt.MapFrom(e => e.GetType() == typeof(WizardCharacterAbilities) ? (WizardCharacterAbilities)e : null))
                .ForMember(dest => dest.KingCharacterAbilitiesResponse,
                    opt => opt.MapFrom(e => e.GetType() == typeof(KingCharacterAbilities) ? (KingCharacterAbilities)e : null))
                .ForMember(dest => dest.ScientistCharacterAbilitiesResponse,
                    opt => opt.MapFrom(e => e.GetType() == typeof(ScientistCharacterAbilities) ? (ScientistCharacterAbilities)e : null));

            CreateMap<ScientistCharacterAbilities, ScientistCharacterAbilitiesResponse>();
            CreateMap<VikingCharacterAbilities, VikingCharacterAbilitiesResponse>();
            CreateMap<WizardCharacterAbilities, WizardCharacterAbilitiesResponse>();
            CreateMap<KingCharacterAbilities, KingCharacterAbilitiesResponse>();
        }
    }
}
