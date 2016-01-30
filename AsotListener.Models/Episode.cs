namespace AsotListener.Models
{
    using Windows.UI.Xaml;
    using Enums;
    using System.Runtime.Serialization;
    using static Enums.EpisodeStatus;

    /// <summary>
    /// Episode model
    /// </summary>
    [DataContract]
    public class Episode : BaseModel
    {
        #region Fields

        private EpisodeStatus status = CanBeLoaded;
        private double overallDownloadSize = double.MaxValue;
        private double downloadedAmount = 0;
        private Visibility downloadProgressbarVisibility = Visibility.Collapsed;

        #endregion

        #region Ctor

        /// <summary>
        /// Creates instance of <see cref="Episode"/>
        /// </summary>
        /// <param name="name">Unique episode name</param>
        /// <param name="episodeNumber">Episode number</param>
        public Episode(string name, int episodeNumber = -1)
        {
            Name = name;
            EpisodeNumber = episodeNumber;
        }

        #endregion

        #region Non-bindable Properties

        /// <summary>
        /// Episode name
        /// </summary>
        [DataMember]
        public string Name { get; set; }

        /// <summary>
        /// Relative url of episode description page
        /// </summary>
        [DataMember]
        public string Url { get; set; }

        /// <summary>
        /// Links for downloading episode audio files
        /// </summary>
        [DataMember]
        public string[] DownloadLinks { get; set; }

        /// <summary>
        /// Determines if episode can be played
        /// </summary>
        public bool CanBePlayed => Status == InPlaylist || Status == Loaded;

        /// <summary>
        /// Episode number
        /// </summary>
        [DataMember]
        public int EpisodeNumber { get; set; }

        #endregion

        #region Bindable Properties

        /// <summary>
        /// Current episode status
        /// </summary>
        [DataMember]
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

        #endregion

        #region Overrided Methods

        /// <summary>
        /// Returns string representation of <see cref="Episode"/>
        /// </summary>
        /// <returns></returns>
        public override string ToString() => Name;

        /// <summary>
        /// Compares two <see cref="Episode"/> objects
        /// </summary>
        /// <param name="obj">Episode to compare to</param>
        /// <returns>True when Episodes has the same names</returns>
        public override bool Equals(object obj)
        {
            var other = obj as Episode;
            return other != null && other.Name == Name;
        }

        /// <summary>
        /// Gets hash code of the Episode
        /// </summary>
        /// <returns>Hash code of the Episode</returns>
        public override int GetHashCode() => Name.GetHashCode();

        #endregion
    }
}
