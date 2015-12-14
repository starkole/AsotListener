namespace AsotListener.Models
{
    using Enums;
    using Windows.UI.Xaml;

    public class Episode: BaseModel
    {
        private string name;
        private string url;
        private EpisodeStatus status = EpisodeStatus.CanBeLoaded;
        private double overallDownloadSize = double.MaxValue;
        private double downloadedAmount = 0;
        private Visibility downloadProgressbarVisibility = Visibility.Collapsed;

        public string Name
        {
            get { return name; }
            set { SetField(ref name, value, nameof(Name)); }
        }

        public string Url
        {
            get { return url; }
            set { SetField(ref url, value, nameof(Url)); }
        }

        public EpisodeStatus Status
        {
            get { return status; }
            set
            {
                if (value == EpisodeStatus.Downloading)
                {
                    DownloadProgressbarVisibility = Visibility.Visible;
                }
                else
                {
                    DownloadProgressbarVisibility = Visibility.Collapsed;
                }

                SetField(ref status, value, nameof(Status));
            }
        }

        public double OverallDownloadSize
        {
            get { return overallDownloadSize; }
            set { SetField(ref overallDownloadSize, value, nameof(OverallDownloadSize)); }
        }

        public double DownloadedAmount
        {
            get { return downloadedAmount; }
            set { SetField(ref downloadedAmount, value, nameof(DownloadedAmount)); }
        }

        public Visibility DownloadProgressbarVisibility
        {
            get { return downloadProgressbarVisibility; }
            set { SetField(ref downloadProgressbarVisibility, value, nameof(DownloadProgressbarVisibility)); }
        }

        public string[] DownloadLinks { get; set; }

        public override string ToString() => Name;
    }
}
