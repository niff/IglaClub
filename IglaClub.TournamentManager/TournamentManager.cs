﻿using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IglaClub.ObjectModel;
using IglaClub.ObjectModel.Entities;
using IglaClub.ObjectModel.Enums;
using IglaClub.ObjectModel.Tools;


namespace IglaClub.TournamentManager
{
    public class TournamentManager
    {
        private readonly IIglaClubDbContext db;

        public TournamentManager(IIglaClubDbContext dbContext)
        {
            this.db = dbContext;
        }

        public bool AddPair(long tournamentId, long player1Id, long player2Id)
        {
            Tournament tournament = db.Tournaments.Find(tournamentId);
            User player1 = db.Users.Find(player1Id);
            User player2 = db.Users.Find(player2Id);
            tournament.Pairs.Add(new Pair() { Tournament = tournament, PairNumber = 1, Player1 = player1, Player2 = player2} );
            db.SaveChanges();
            return true;
        }

        public bool StartTournament(long tournamentId)
        {
            Tournament tournament = db.Tournaments.Find(tournamentId);
            IEnumerable<BoardInstance> boards = GenerateEmptyBoards(tournament);
            TournamentHelper.AddBoardsToTournament(tournament, boards);

            IEnumerable<Result> results = TournamentHelper.GenerateInitialSittingPosition(tournament);
            TournamentHelper.AddResultsToTournament(tournament, results);
            
            tournament.CurrentRound = 1;
            tournament.TournamentStatus = TournamentStatus.Started;
            tournament.StartDate = DateTime.Now;
            db.SaveChanges();
            return true;
        }

        public bool GenerateNextRound(long tournamentId, bool withPairRepeats)
        {
            Tournament tournament = db.Tournaments.Find(tournamentId);
            //check whether round is finished (all boards are played (number of tricks))
            IEnumerable<BoardInstance> boards = GenerateEmptyBoards(tournament);
            TournamentHelper.AddBoardsToTournament(tournament, boards);

            //todo: check if tournament should finish if michell
            IEnumerable<Result> results = TournamentHelper.GenerateNextCavendishRound(tournament,withPairRepeats);
            TournamentHelper.AddResultsToTournament(tournament, results);

            tournament.CurrentRound ++;
            
            db.SaveChanges();
            return true;
        }
        
        private IEnumerable<BoardInstance> GenerateEmptyBoards(Tournament tournament)
        {
            var boardInstances = new List<BoardInstance>();
            if (tournament.TournamentMovingType != TournamentMovingType.Cavendish)
                throw new NotSupportedException("By now only Cavendish tournament type is supported");
            var currentMaxBoardNumber = tournament.Boards.Count == 0 ? 0 : tournament.Boards.Max(b => b.BoardNumber);
            for (int i = currentMaxBoardNumber + 1; i < tournament.BoardsInRound + currentMaxBoardNumber + 1; i++)
            {
                boardInstances.Add(CreateEmptyBoardInstance(tournament, i));
            }

            return boardInstances;
        }

        private BoardInstance CreateEmptyBoardInstance(Tournament tournament, int boardNumber)
        {
            var boardDefinition = new BoardDefinition()
                {
                    CreationDate = DateTime.Now,
                    Dealer = TournamentHelper.GetDealerByBoardNumber(boardNumber),
                    Vulnerability = TournamentHelper.GetVulneralbityByBoardNumber(boardNumber)
                };
            db.BoardDefinitions.Add(boardDefinition);
            db.SaveChanges();

            var boardInstance = new BoardInstance()
                {
                    BoardDefinition = boardDefinition,
                    BoardNumber = boardNumber,
                    Tournament = tournament
                };
            return boardInstance;
        }

        
    }
}
