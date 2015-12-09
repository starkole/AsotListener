namespace AsotListener.Models
{
    public class Episode : BaseModel
    {
        private string name;
        private string url;
        private EpisodeStatus status = EpisodeStatus.CanBeLoaded;

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
            set { SetField(ref status, value, nameof(Status)); }
        }

        public string[] DownloadLinks { get; set; }

        public override string ToString() => Name;
    }
}
