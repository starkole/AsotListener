namespace AsotListener.Services.Contracts
{
    using System.Collections.Generic;
    using Models;

    public interface IParser
    {
        string[] ExtractDownloadLinks(string EpisodeHtmlPage);
        IList<Episode> ParseEpisodeList(string EpisodeListHtmlPage);
    }
}