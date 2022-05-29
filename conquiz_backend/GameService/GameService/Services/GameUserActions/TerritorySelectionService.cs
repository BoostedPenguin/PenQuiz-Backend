using GameService.Data.Models;
using GameService.Dtos.SignalR_Responses;
using GameService.Services.Extensions;
using GameService.Services.GameTimerServices;
using Microsoft.AspNetCore.Http;
using System.Linq;

namespace GameService.Services.GameUserActions
{
    public interface ITerritorySelectionService
    {
        SelectedTerritoryResponse SelectTerritory(string mapTerritoryName);
    }

    public class TerritorySelectionService : ITerritorySelectionService
    {
        private readonly IGameTerritoryService gameTerritoryService;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IGameTimerService gameTimerService;
        private const string DefaultMap = "Antarctica";

        public TerritorySelectionService(IGameTerritoryService gameTerritoryService, IHttpContextAccessor httpContextAccessor, IGameTimerService gameTimerService)
        {
            this.gameTerritoryService = gameTerritoryService;
            this.httpContextAccessor = httpContextAccessor;
            this.gameTimerService = gameTimerService;
        }

        private SelectedTerritoryResponse SelectTerritoryPvp(GameInstance gm, CurrentRoundOverview currentRoundOverview, int userId, string mapTerritoryName)
        {
            var pvpRound = gm.Rounds.Where(x => x.Id == currentRoundOverview.RoundId).FirstOrDefault();

            // Person who selected a territory is the attacker
            if (pvpRound.PvpRound.AttackerId != userId)
                throw new GameException("Not this players turn");

            if (pvpRound.PvpRound.AttackedTerritoryId != null)
                throw new BorderSelectedGameException("You already selected a territory for this round");

            var mapTerritory = gm.ObjectTerritory.Where(e => e.MapTerritory.TerritoryName == mapTerritoryName).FirstOrDefault();

            if (mapTerritory == null)
                throw new GameException($"A territory with name `{mapTerritoryName}` for map `{DefaultMap}` doesn't exist");

            var gameObjTerritory = gameTerritoryService
                .SelectTerritoryAvailability(gm, userId, currentRoundOverview.GameInstanceId, mapTerritory.MapTerritoryId, false);

            if (gameObjTerritory == null)
                throw new BorderSelectedGameException("The selected territory doesn't border any of your borders or is attacked by someone else");

            // Set this territory as being attacked from this person
            pvpRound.PvpRound.AttackedTerritoryId = gameObjTerritory.Id;
            pvpRound.PvpRound.DefenderId = gameObjTerritory.TakenBy;

            // Set the ObjectTerritory as being attacked currently
            var obj = gm.ObjectTerritory.First(e => e.Id == gameObjTerritory.Id);
            obj.AttackedBy = pvpRound.PvpRound.AttackerId;

            return new SelectedTerritoryResponse()
            {
                GameLink = currentRoundOverview.InvitationLink,
                AttackedById = userId,
                TerritoryId = gameObjTerritory.Id
            };
        }

        private SelectedTerritoryResponse SelectTerritoryNeutral(GameInstance gm, CurrentRoundOverview currentRoundOverview, int userId, string mapTerritoryName)
        {
            var neutralRound = gm.Rounds
                .Where(x => x.Id == currentRoundOverview.RoundId).FirstOrDefault();


            // Check if it's this player's turn for selecting a neutral territory or not

            var currentTurnsPlayer = neutralRound
                .NeutralRound
                .TerritoryAttackers
                .FirstOrDefault(x => x.AttackOrderNumber == neutralRound.NeutralRound.AttackOrderNumber && x.AttackerId == userId);

            if (currentTurnsPlayer == null)
                throw new GameException("Unknown player turn.");

            if (currentTurnsPlayer.AttackedTerritoryId != null)
                throw new BorderSelectedGameException("You already selected a territory for this round");


            var mapTerritory = gm.ObjectTerritory.Where(e => e.MapTerritory.TerritoryName == mapTerritoryName).FirstOrDefault();

            if (mapTerritory == null)
                throw new GameException($"A territory with name `{mapTerritoryName}` for map `{DefaultMap}` doesn't exist");

            var gameObjTerritory = gameTerritoryService
                .SelectTerritoryAvailability(gm, userId, currentRoundOverview.GameInstanceId, mapTerritory.MapTerritoryId, true);

            if (gameObjTerritory == null)
                throw new BorderSelectedGameException("The selected territory doesn't border any of your borders or is attacked by someone else");

            if (gameObjTerritory.TakenBy != null)
                throw new BorderSelectedGameException("The selected territory is already taken by somebody else");

            // Set this territory as being attacked from this person
            currentTurnsPlayer.AttackedTerritoryId = gameObjTerritory.Id;

            // Set the ObjectTerritory as being attacked currently
            var obj = gm.ObjectTerritory.First(e => e.Id == gameObjTerritory.Id);
            obj.AttackedBy = currentTurnsPlayer.AttackerId;


            // Store it in state, and update when the closing event triggers
            // Makes sure entity does not track same entity twice

            //db.Update(obj);
            //await db.SaveChangesAsync();

            return new SelectedTerritoryResponse()
            {
                GameLink = currentRoundOverview.InvitationLink,
                AttackedById = userId,
                TerritoryId = gameObjTerritory.Id
            };
        }

        private class CurrentRoundOverview
        {
            public CurrentRoundOverview(int roundId, AttackStage attackStage, bool isTerritoryVotingOpen, int gameInstanceId, string invitationLink)
            {
                RoundId = roundId;
                AttackStage = attackStage;
                IsTerritoryVotingOpen = isTerritoryVotingOpen;
                GameInstanceId = gameInstanceId;
                InvitationLink = invitationLink;
            }

            public int RoundId { get; }
            public AttackStage AttackStage { get; }
            public bool IsTerritoryVotingOpen { get; }
            public int GameInstanceId { get; }
            public string InvitationLink { get; }
        }

        public SelectedTerritoryResponse SelectTerritory(string mapTerritoryName)
        {
            var globalUserId = httpContextAccessor.GetCurrentUserGlobalId();

            var playerGameTimer = gameTimerService.GameTimers.FirstOrDefault(e =>
                e.Data.GameInstance.Participants.FirstOrDefault(e => e.Player.UserGlobalIdentifier == globalUserId) is not null);

            if (playerGameTimer == null)
            {
                throw new GameException("There is no open game where this player participates");
            }

            var gm = playerGameTimer.Data.GameInstance;

            var userId = gm.Participants.First(e => e.Player.UserGlobalIdentifier == globalUserId).PlayerId;

            var currentRoundOverview = gm.Rounds
                .Where(x =>
                    x.GameRoundNumber == x.GameInstance.GameRoundNumber)
                .Select(x => new CurrentRoundOverview(x.Id, x.AttackStage, x.IsTerritoryVotingOpen, x.GameInstanceId, x.GameInstance.InvitationLink))
                .FirstOrDefault();

            if (currentRoundOverview == null)
                throw new GameException("The current round isn't valid");

            if (!currentRoundOverview.IsTerritoryVotingOpen)
                throw new GameException("The round's territory voting stage isn't open");

            var response = currentRoundOverview.AttackStage switch
            {
                AttackStage.MULTIPLE_NEUTRAL => SelectTerritoryNeutral(gm, currentRoundOverview, userId, mapTerritoryName),
                AttackStage.MULTIPLE_PVP => SelectTerritoryPvp(gm, currentRoundOverview, userId, mapTerritoryName),

                _ => throw new GameException("Current round isn't either multiple neutral nor multiple pvp"),
            };

            OnTerritorySelected(playerGameTimer);

            return response;
        }


        private static void CloseTimerWhenDone(TimerWrapper timerWrapper)
        {
            const int ModifyTimeMilis = 1000;


            if (timerWrapper.TimeUntilNextEvent < ModifyTimeMilis)
                return;

            timerWrapper.ChangeReminingInterval(ModifyTimeMilis);
        }


        private void OnTerritorySelected(TimerWrapper timerWrapper)
        {
            CloseTimerWhenDone(timerWrapper);
        }

    }
}
