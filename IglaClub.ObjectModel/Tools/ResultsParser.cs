﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using IglaClub.ObjectModel.Entities;
using IglaClub.ObjectModel.Enums;

namespace IglaClub.ObjectModel.Tools
{
     public class ResultsParser
    {

        public const string ParseScorePattern =
            @"(?<level>[1-7])(?<color>s|h|c|d|nt)(?<double>x{0,2})(?<player>[nesw])(?<tricks>(=|[+-](1[0-3]|[1-9])))";

        public static readonly Dictionary<string, NESW> PlayersDictionary = new Dictionary<string, NESW>
        {
            {"S", NESW.South},
            {"W", NESW.West},
            {"N", NESW.North},
            {"E", NESW.East}
        };

        public static readonly Dictionary<string, ContractColors> ColorsDictionary = new Dictionary<string, ContractColors>
        {
            {"S", ContractColors.Spade},
            {"H", ContractColors.Heart},
            {"D", ContractColors.Diamond},
            {"C", ContractColors.Club},
            {"NT",ContractColors.NoTrump}
        };

        public static readonly Dictionary<string, ContractDoubled> DoubledDictionary = new Dictionary<string, ContractDoubled>
        {
            {"", ContractDoubled.NotDoubled},
            {"x", ContractDoubled.Doubled},
            {"xx", ContractDoubled.Redoubled}
        };

         
        public static string GetFormatResult(Result result)
        {
            if (result.PlayedBy == NESW.Unavailable)
                return "";
            var tricks = result.NumberOfTricks - result.ContractLevel - 6;
            var tricksString = tricks == 0 ? "=" : tricks.ToString("+#;-#;0");
            var doubledString = DoubledDictionary.First(kpv => kpv.Value == result.ContractDoubled).Key;

            return string.Format("{0}{1}{2} {3} {4}", 
                result.ContractLevel, 
                ColorsDictionary.First(kpv=>kpv.Value == result.ContractColor).Key,
                doubledString, 
                PlayersDictionary.First(kpv=>kpv.Value == result.PlayedBy).Key, 
                tricksString);
        }
        
        public static Result Parse(string stringToParse)
        {
            var r = new Regex(ParseScorePattern);
            var preparedString = String.Join("", stringToParse.Where(c => !char.IsWhiteSpace(c))).ToLowerInvariant();
            if (!r.IsMatch(preparedString))
                return null;
            Match m = r.Match(preparedString);

            var contractLevel = int.Parse(m.Groups["level"].Value);

            var result = new Result
            {
                ContractLevel = contractLevel,
                ContractColor = ParseContractColor(m.Groups["color"].Value),
                PlayedBy = ParsePlayedBy(m.Groups["player"].Value),
                ContractDoubled = ParseDoubled(m.Groups["double"].Value),
                NumberOfTricks = ParseNumberOfTricks(m.Groups["tricks"].Value, contractLevel)
            };
            return result;

        }

        public static Result UpdateResult(Result original, Result resultToCopy)
        {
            original.ContractLevel = resultToCopy.ContractLevel;
            original.ContractColor = resultToCopy.ContractColor;
            original.NumberOfTricks = resultToCopy.NumberOfTricks;
            original.ContractDoubled = resultToCopy.ContractDoubled;
            original.PlayedBy = resultToCopy.PlayedBy;

            return original;
        }
        private static int ParseNumberOfTricks(string numberOfTricksString, int contractLevel)
        {
            var tricks = numberOfTricksString.StartsWith("=") ? 0 : int.Parse(numberOfTricksString);
            return 6 + contractLevel + tricks;
        }

        private static ContractDoubled ParseDoubled(string doubleString)
        {
            return DoubledDictionary[doubleString.ToLowerInvariant()];
        }

        private static NESW ParsePlayedBy(string playerString)
        {
            return PlayersDictionary[playerString.ToUpperInvariant()];
        }

        private static ContractColors ParseContractColor(string colorString)
        {
            return ColorsDictionary[colorString.ToUpperInvariant()];
        }
    }
}