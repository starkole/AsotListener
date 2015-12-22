namespace AsotListener.Models
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Audio track model
    /// </summary>
    [DataContract]
    public class AudioTrack: BaseModel
    {
        private string name;
        private string uri;
        private TimeSpan startPosition = TimeSpan.FromSeconds(0);

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
        public string Uri
        {
            get { return uri; }
            set { SetField(ref uri, value, nameof(Uri)); }
        }

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
        /// Returns string representation of current audio track
        /// </summary>
        /// <returns>String representation of current audio track</returns>
        public override string ToString() => Name;
    }
}
