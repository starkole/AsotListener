namespace AsotListener.Models
{
    using System;
    using System.Runtime.Serialization;
    using Common;

    /// <summary>
    /// Audio track model
    /// </summary>
    [DataContract]
    public class AudioTrack : BaseModel
    {
        #region Fields

        private string name;
        private string artist = Constants.DefaultArtist;
        private string albumArtist = Constants.DefaultAlbumArtist;
        private TimeSpan startPosition = TimeSpan.Zero;
        private TimeSpan duration = Constants.UnknownDuration;

        #endregion

        #region Ctor
        
        /// <summary>
        /// Creates new instance of <see cref="AudioTrack"/>
        /// </summary>
        /// <param name="episodeName">Episode name this audio track belongs to</param>
        public AudioTrack(string episodeName)
        {
            if (string.IsNullOrEmpty(episodeName))
            {
                throw new ArgumentNullException(nameof(episodeName));
            }

            EpisodeName = episodeName;
        }

        #endregion
        
        /// <summary>
        /// Episode name this audio track belongs to
        /// </summary>
        [DataMember]
        public string EpisodeName { get; set; }

        /// <summary>
        /// Audio track name
        /// </summary>
        [DataMember]
        public string Name
        {
            get { return name; }
            set { SetField(ref name, value, nameof(Name)); }
        }

        /// <summary>
        /// Audio track URI in local file system
        /// </summary>
        [DataMember]
        public string Uri { get; set; }

        /// <summary>
        /// Audio track start position
        /// </summary>
        [DataMember]
        public TimeSpan StartPosition
        {
            get { return startPosition; }
            set { SetField(ref startPosition, value, nameof(StartPosition)); }
        }

        /// <summary>
        /// Audio track duration
        /// </summary>
        [DataMember]
        public TimeSpan Duration
        {
            get { return duration; }
            set { SetField(ref duration, value, nameof(Duration)); }
        }

        /// <summary>
        /// Audio track artist
        /// </summary>
        [DataMember]
        public string Artist
        {
            get { return artist; }
            set { SetField(ref artist, value, nameof(Artist)); }
        }

        /// <summary>
        /// Audio track album artist
        /// </summary>
        [DataMember]
        public string AlbumArtist
        {
            get { return albumArtist; }
            set { SetField(ref albumArtist, value, nameof(AlbumArtist)); }
        }

        #region Overrides

        /// <summary>
        /// Returns string representation of current audio track
        /// </summary>
        /// <returns>String representation of current audio track</returns>
        public override string ToString() => Name; 
        
        #endregion
    }
}
