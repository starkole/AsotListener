using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace ContentDownloader
{
    class Loader: IDisposable
    {
        private const string MAIN_URL = "http://asotarchive.org";

        private HttpClient httpClient = new HttpClient();

        public async Task<String> FetchEpisodeListAsync()
        {
            return await httpClient.GetStringAsync(MAIN_URL);
        }

        public async Task<String> FetchEpisodePageAsync(Episode episode)
        {
            return await httpClient.GetStringAsync(MAIN_URL + episode.Url);
        }

        public void Dispose()
        {
            httpClient.Dispose();
        }
    }
}
