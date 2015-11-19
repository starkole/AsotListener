using System;
using Windows.Web.Http;

namespace TestApp
{
	public class Loader {
		public String ResponseString {get; private set;}
		
		public async void FetchEpisodeListAsync() {
			try {
				HttpClient httpClient = new HttpClient();
				httpClient.DefaultRequestHeaders.AcceptTryParseAdd("text/html");			
				ResponseString = await httpClient.GetStringAsync(new Uri("http://asotarchive.org"));				
			} catch Exception (ex) {
				// TODO: Show error to user.
			} finally {
				httpClient.Close();
			}
		}
		
		public void LoadEpisode(String episodeName) {
			throw new NotImplementedException();
		}
	}
}