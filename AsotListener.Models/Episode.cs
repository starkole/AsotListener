namespace AsotListener.Models
{
    using Windows.UI.Xaml;
    using Enums;
    using static Enums.EpisodeStatus;

    /// <summary>
    /// Episode model
    /// </summary>
    public class Episode : BaseModel
    {
        private string name;
        private string url;
        private EpisodeStatus status = EpisodeStatus.CanBeLoaded;
        private double overallDownloadSize = double.MaxValue;
        private double downloadedAmount = 0;
        private Visibility downloadProgressbarVisibility = Visibility.Collapsed;

        /// <summary>
        /// Episode name
        /// </summary>
        public string Name
        {
            get { return name; }
            set { SetField(ref name, value, nameof(Name)); }
        }

        /// <summary>
        /// Relative url of episode description page
        /// </summary>
        public string Url
        {
            get { return url; }
            set { SetField(ref url, value, nameof(Url)); }
        }

        /// <summary>
        /// Current episode status
        /// </summary>
        public EpisodeStatus Status
        {
            get { return status; }
            set
            {
                DownloadProgressbarVisibility = value == Downloading ?
                    DownloadProgressbarVisibility = Visibility.Visible :
                    DownloadProgressbarVisibility = Visibility.Collapsed;
                SetField(ref status, value, nameof(Status));
            }
        }

        /// <summary>
        /// Determines if episode can be played
        /// </summary>
        public bool CanBePlayed => Status == InPlaylist || Status == Loaded;

        /// <summary>
        /// Overall amount of bytes to download
        /// </summary>
        public double OverallDownloadSize
        {
            get { return overallDownloadSize; }
            set { SetField(ref overallDownloadSize, value, nameof(OverallDownloadSize)); }
        }

        /// <summary>
        /// Amount of bytes has already been downloaded
        /// </summary>
        public double DownloadedAmount
        {
            get { return downloadedAmount; }
            set { SetField(ref downloadedAmount, value, nameof(DownloadedAmount)); }
        }

        /// <summary>
        /// Determines the visibility of downloading progress bar
        /// </summary>
        public Visibility DownloadProgressbarVisibility
        {
            get { return downloadProgressbarVisibility; }
            set { SetField(ref downloadProgressbarVisibility, value, nameof(DownloadProgressbarVisibility)); }
        }

        /// <summary>
        /// Links for downloading episode audio files
        /// </summary>
        public string[] DownloadLinks { get; set; }

        /// <summary>
        /// Returns string representation of <see cref="Episode"/>
        /// </summary>
        /// <returns></returns>
        public override string ToString() => Name;
    }
}
