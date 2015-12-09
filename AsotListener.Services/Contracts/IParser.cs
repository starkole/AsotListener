namespace AsotListener.Services.Contracts
{
    using System.Collections.ObjectModel;
    using Models;

    public interface IParser
    {
        string[] ExtractDownloadLinks(string EpisodeHtmlPage);
        ObservableCollection<Episode> ParseEpisodeList(string EpisodeListHtmlPage);
    }
}