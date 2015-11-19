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
                List<Episode> episodes = Parser.ParseEpisodeList(episodeListPage);
                string episodePage = await loader.FetchEpisodePageAsync(episodes[1]);
                List<string> downloadLinks = Parser.ExtractDownloadLinks(episodePage);
                foreach (string link in downloadLinks)
                {
                    Console.WriteLine(link);
                }
                Console.ReadKey();
            }
        }
    }
}
