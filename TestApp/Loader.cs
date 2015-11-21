namespace AsotListener
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Models;

    class Loader : IDisposable
    {
        private const string MAIN_URL = "http://asotarchive.org";
        private const string FALLBACK_FILENAME = "file";
        private const string FILE_EXTENSION = ".mp3";
        private const int CONNECTION_TIMEOUT_SECONDS = 30;

        private HttpClient httpClient;

        public Loader()
        {
            httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(CONNECTION_TIMEOUT_SECONDS);
        }

        public async Task<string> FetchEpisodeListAsync()
        {
            return await httpClient.GetStringAsync(MAIN_URL);
        }

        public async Task<string> FetchEpisodePageAsync(Episode episode)
        {
            return await httpClient.GetStringAsync(MAIN_URL + episode.Url);
        }

        public async Task DownloadEpisodeAsync(List<string> urls, string filename)
        {
            if (urls == null || urls.Count < 1)
            {
                return;
            }

            if (string.IsNullOrEmpty(filename))
            {
                filename = FALLBACK_FILENAME;
            }

            for (int i = 0; i < urls.Count; i++)
            {
                int fileId = i + 1;
                string episodeFilename = Path.GetFullPath(filename + fileId.ToString() + FILE_EXTENSION);
                Console.WriteLine(
                    "Starting download part {0} of {1} into {2} file",
                    fileId,
                    urls.Count,
                    episodeFilename);

                using (HttpResponseMessage response = await httpClient.GetAsync(
                    urls[i],
                    HttpCompletionOption.ResponseHeadersRead))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine("Server connection error. Status {0} {1}",
                            response.StatusCode, 
                            response.ReasonPhrase);
                    }
                    
                    Console.WriteLine("Starting download {0} bytes", 
                        response.Content.Headers.ContentLength);
                    using (Stream streamToReadFrom = await response.Content.ReadAsStreamAsync())
                    {
                        string fileToWriteTo = Path.GetTempFileName();
                        using (Stream streamToWriteTo = File.Open(
                            fileToWriteTo, 
                            FileMode.Create))
                        {
                            Console.WriteLine("Start loading...");
                            await streamToReadFrom.CopyToAsync(streamToWriteTo);
                            Console.WriteLine("Load complete.");
                        }
                    }
                }
            }
        }

        public void Dispose()
        {
            httpClient.Dispose();
        }
    }
}
