using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContentDownloader
{
    class Program
    {
        static void Main(string[] args)
        {
            RunAsync().Wait();
        }

        static async Task RunAsync()
        {
            using (var loader = new Loader())
            {
                // TODO: Add exception handling.
                string episodeListPage = await loader.FetchEpisodeListAsync();
                Console.WriteLine("Got episodes list.");

                List<Episode> episodes = Parser.ParseEpisodeList(episodeListPage);
                string episodePage = await loader.FetchEpisodePageAsync(episodes[1]);
                Console.WriteLine("Got episode page for {0} episode.", episodes[1].Name);

                List<string> downloadLinks = Parser.ExtractDownloadLinks(episodePage);
                Console.WriteLine("Found {0} download links.", downloadLinks.Count);

                await loader.DownloadEpisodeAsync(downloadLinks, "episode");
                Console.WriteLine("Completed. Press any key to exit...");

                Console.ReadKey();
            }
        }
    }
}
