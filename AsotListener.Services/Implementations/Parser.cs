namespace AsotListener.Services.Implementations
{
    using System.Text.RegularExpressions;
    using Models;
    using System.Collections.ObjectModel;
    using Contracts;
    using Windows.Foundation.Diagnostics;
    using System.Collections.Generic;
    /// <summary>
    /// Provides methods for parsing html pages
    /// </summary>
    public sealed class Parser : IParser
    {
        private const string EPISODE_REGEX = @"(/episode/\?p=\d+)"">(.*)<";
        private const string DOWNLOAD_LINK_REGEX = @"http://\S*\.mp3";
        private const string EPISODE_NAME_START = @"A State Of Trance: ";
        private const string EPISODE_NUMBER_REGEX = @"(Episode )(\d+)";
        private readonly ILogger logger;

        /// <summary>
        /// Creates new instance of <see cref="Parser"/>
        /// </summary>
        /// <param name="logger">The logger instance</param>
        public Parser(ILogger logger)
        {
            this.logger = logger;
            logger.LogMessage("Parser initialized.", LoggingLevel.Information);
        }

        /// <summary>
        /// Extracts episodes information from given html page
        /// </summary>
        /// <param name="EpisodeListHtmlPage">Html page</param>
        /// <returns>List of Episode objects or empty list</returns>
        public IList<Episode> ParseEpisodeList(string EpisodeListHtmlPage)
        {
            logger.LogMessage("Parser: Parsing episode list...");
            Regex regex = new Regex(EPISODE_REGEX);
            MatchCollection matches = regex.Matches(EpisodeListHtmlPage);
            var result = new List<Episode>();
            for (var i = 0; i < matches.Count; i++)
            {
                // Regex puts episode relative url into the first group
                // and episode name into the second one
                if (matches[i].Groups.Count == 3)
                {
                    var episodeName = normalizeEpisodeName(matches[i].Groups[2].Value);
                    int episodeNumber = extractEpisodeNumber(episodeName);
                    result.Add(new Episode(episodeName, episodeNumber)
                    {
                        Url = matches[i].Groups[1].Value
                    });
                }
            }

            logger.LogMessage("Parser: Episode list parsed.");
            return result;
        }

        /// <summary>
        /// Extracts download links to mp3 files from given html page
        /// </summary>
        /// <param name="EpisodeHtmlPage">Html page</param>
        /// <returns>List of download links or empty list</returns>
        public string[] ExtractDownloadLinks(string EpisodeHtmlPage)
        {
            logger.LogMessage("Parser: Extracting download links...");

            Regex regex = new Regex(DOWNLOAD_LINK_REGEX);
            MatchCollection matches = regex.Matches(EpisodeHtmlPage);
            if (matches.Count == 0)
            {
                return new string[0];
            }

            string[] result = new string[matches.Count];
            for (var i = 0; i < matches.Count; i++)
            {
                result[i] = matches[i].Value;
            }

            logger.LogMessage($"Parser: Extracted {matches.Count} download links.", LoggingLevel.Information);
            return result;
        }

        private string normalizeEpisodeName(string rawName)
        {
            Regex episodeNameStart = new Regex(EPISODE_NAME_START);
            string nameWithStrippedStart = episodeNameStart.Replace(rawName, string.Empty);
            Regex containsABadCharacter = new Regex("[" + Regex.Escape(new string(System.IO.Path.GetInvalidPathChars())) + "]");
            return containsABadCharacter.Replace(nameWithStrippedStart, string.Empty);
        }

        private int extractEpisodeNumber(string normalizedEpisodeName)
        {
            int result = -1;
            Regex regex = new Regex(EPISODE_NUMBER_REGEX);
            var matches = regex.Matches(normalizedEpisodeName);
            if (matches.Count == 1 && matches[0].Groups.Count == 3)
            {
                int.TryParse(matches[0].Groups[2].Value, out result);
            }

            return result;
        }
    }
}
