﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using IglaClub.ObjectModel.Entities;
using IglaClub.ObjectModel.Repositories;
using IglaClub.ObjectModel.Tools;
using IglaClub.Web.Authorization;
using IglaClub.Web.Models;
using IglaClub.Web.Models.ViewModels;
using IglaClub.TournamentManager;

namespace IglaClub.Web.Controllers
{
    [Authorize]
    public class ResultsController : Controller
    {
        private readonly IglaClubDbContext db = new IglaClubDbContext();
        private readonly TournamentManager.TournamentManager tournamentManager = new TournamentManager.TournamentManager(new IglaClubDbContext());

        private readonly PairRepository pairRepository;
        private readonly UserRepository userRepository;
        private readonly ResultRepository resultRepository;
        private readonly TournamentRepository tournamentRepository;
        private const string _itemNotFound = "Tournament not found.";

        public ResultsController()
        {
            pairRepository = new PairRepository(db);
            userRepository = new UserRepository(db);
            resultRepository = new ResultRepository(db);
            tournamentRepository = new TournamentRepository(db);
        }

        public PartialViewResult Index(long tournamentId)
        {
            throw new NotImplementedException();
        }

        public ActionResult Edit(long tournamentId)
        {
            var results = db.Tournaments.Find(tournamentId).Results.ToList();
            results = results.OrderBy(r => r.Board.BoardNumber).ThenBy(r => r.TableNumber).ToList();
            return View(results);
        }
              
        [HttpPost]
        public ActionResult Edit(List<Result> results)
        {
            if (!ModelState.IsValid)
                return View(results);

            if (results != null)
            {
                for (int i = 0; i < results.Count; i++)
                {
                    var result = results[i];
                    var parsedResult = ResultsParser.Parse(Request["ShortScore[" + i + "]"]);
                    if (parsedResult != null)
                        result = ResultsParser.UpdateResult(result, parsedResult);
                    resultRepository.InsertOrUpdate(result);
                }
                this.resultRepository.SaveChanges();

                if (results.Any())
                {
                    this.tournamentManager.CalculateResultsComplete(results[0].TournamentId);
                    return RedirectToAction("Manage", new { results.FirstOrDefault().TournamentId });
                }
            }
            return null;
        }

        public ActionResult EditResult(long resultId)
        {
            var result = db.Results.Find(resultId);
            return View(result);
        }

        [HttpPost]
        public ActionResult EditResult(Result result)
        {
            if (ModelState.IsValid)
            {
                var parsedResult = ResultsParser.Parse(Request["ShortScore"]);
                if (parsedResult != null)
                    result = ResultsParser.UpdateResult(result, parsedResult);
                var board = this.resultRepository.Get<BoardInstance>(result.BoardId);
                result.Board = board;
                result.ResultNsPoints = TournamentHelper.CalculateScoreInBoard(result);
                resultRepository.InsertOrUpdate(result);
                this.resultRepository.SaveChanges();
                return RedirectToAction("RoundDetails", "Round", new { result.TournamentId });
            }
            return View(result);
        }

        [TournamentOwner]
        public ActionResult Manage(long tournamentId, string sort, string sortdir)
        {
            Tournament tournament = tournamentRepository.GetTournament(tournamentId);
            if (tournament == null)
                return Content(_itemNotFound);
            List<Result> results = tournament.Results.ToList();
            if (!string.IsNullOrEmpty(sort))
            {
               switch (sort)
               { 
                    case "BoardNumber":
                        results = tournament.Results.OrderBy(r => r.Board.BoardNumber).ToList();
                        break;
                    case "TableNumber":
                        results = tournament.Results.OrderBy(r => r.TableNumber).ToList();
                        break;
                    default:
                        results = tournament.Results.OrderBy(r => r.Board.BoardNumber).OrderBy(r => r.RoundNumber).ToList();
                        break;
                }
                if (sortdir == "DESC")
                    results.Reverse();
            }
            return View(new TournamentResultsVm{Tournament = tournament, Results = results});
        }

        public ActionResult CreateEmpty(long tournamentId)
        {
            var tournament = db.Tournaments.Find(tournamentId);
            var result = new Result {Tournament = tournament};
            db.Results.Add(result);
            return RedirectToAction("Manage",new { tournamentId });
        }

        [ActionName("Delete")]
        public ActionResult DeleteConfirmed(long id)
        {
            long tournamentId = tournamentManager.DeleteResult(id);
            if (Request.UrlReferrer != null) 
                return Redirect(Request.UrlReferrer.ToString());
            return RedirectToAction("Manage", new { tournamentId});
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }

        public ActionResult RemoveLastRound(long tournamentId)
        {
            tournamentManager.RemoveLastRound(tournamentId);
            return RedirectToAction("Manage", new {tournamentId });
        }

        public ActionResult GenerateNextRound(long tournamentId, bool withPairsRepeat)
        {
            tournamentManager.GenerateNextRound(tournamentId, withPairsRepeat);
            return RedirectToAction("Manage", new { tournamentId });
        }

        public ActionResult AddNewResult(long tournamentId)
        {
            tournamentManager.AddNewResult(tournamentId);
            if (Request.UrlReferrer != null) 
                return Redirect(Request.UrlReferrer.ToString());
            return RedirectToAction("Edit", new { tournamentId });
        }

        public PartialViewResult PairsResults(long tournamentId)
        {
            var tournament = tournamentRepository.GetTournament(tournamentId);
            Dictionary<long, int> pairNumberMaxPoints = resultRepository.GetDictionaryPairNumberMaxPoints(tournamentId);
            var pairsResultsViewModel = new PairsResultsViewModel
                {
                    TournamentScoringType = tournament.TournamentScoringType,
                    PairsInTounament = pairRepository.GetPairsByTournament(tournamentId).OrderByDescending(p=>p.Score).ToList(),
                    PairNumberMaxPoints = pairNumberMaxPoints
                    
                };

            return PartialView("_PairsResults", pairsResultsViewModel);
        }

    }
}