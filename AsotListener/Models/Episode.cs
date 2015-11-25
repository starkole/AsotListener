namespace AsotListener.Models
{
    public class Episode
    {
        public string Name { get; set; }
        public string Url { get; set; }
        public EpisodeStatus Status { get; set; } = EpisodeStatus.CanBeLoaded;

        public override string ToString()
        {
            return Name;
        }
    }
}
