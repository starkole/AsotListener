namespace AsotListener
{
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using Models;

    /// <summary>
    /// Provides methods for parsing html pages
    /// </summary>
    static class Parser
    {
        private const string EPISODE_REGEX = @"(/episode/\?p=\d+)"">(.*)<";
        private const string DOWNLOAD_LINK_REGEX = @"http://\S*\.mp3";

        /// <summary>
        /// Extracts episodes information from given html page
        /// </summary>
        /// <param name="EpisodeListHtmlPage">Html page</param>
        /// <returns>List of Episode objects or empty list</returns>
        public static List<Episode> ParseEpisodeList(string EpisodeListHtmlPage)
        {
            Regex regex = new Regex(EPISODE_REGEX);
            MatchCollection matches = regex.Matches(EpisodeListHtmlPage);
            List<Episode> result = new List<Episode>();
            for (var i = 0; i < matches.Count; i++) {

                // Regex puts episode relative url into the first group
                // and episode name into the second one
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
        
        /// <summary>
        /// Extracts download links to mp3 files from given html page
        /// </summary>
        /// <param name="EpisodeHtmlPage">Html page</param>
        /// <returns>List of download links or empty list</returns>
        public static List<string> ExtractDownloadLinks(string EpisodeHtmlPage)
        {
            Regex regex = new Regex(DOWNLOAD_LINK_REGEX);
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
