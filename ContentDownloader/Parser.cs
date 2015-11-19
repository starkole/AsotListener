using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ContentDownloader
{
    static class Parser
    {
        public static List<Episode> ParseEpisodeList(string EpisodeListHtmlPage)
        {
            Regex regex = new Regex(@"(/episode/\?p=\d+)"">(.*)<");
            MatchCollection matches = regex.Matches(EpisodeListHtmlPage);
            List<Episode> result = new List<Episode>();
            for (var i = 0; i < matches.Count; i++) {
                if (matches[i].Groups.Count == 3)
                {
                    result.Add(new Episode
                    {
                        Id = i,
                        Name = matches[i].Groups[2].Value,
                        Url = matches[i].Groups[1].Value
                    });
                }
            };
            return result;
        }

        public static List<string> ExtractDownloadLinks(string EpisodeHtmlPage)
        {
            Regex regex = new Regex(@"http://\S*\.mp3");
            MatchCollection matches = regex.Matches(EpisodeHtmlPage);
            List<string> result = new List<string>();
            foreach (Match match in matches)
            {
                result.Add(match.Value);
            }

            return result;
        }
    }
}
