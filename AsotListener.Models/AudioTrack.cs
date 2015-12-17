﻿namespace AsotListener.Models
{
    using System;

    public class AudioTrack: BaseModel
    {
        private string name;
        private string uri;
        private TimeSpan startPosition = TimeSpan.FromSeconds(0);

        public AudioTrack(string episodeName)
        {
            if (string.IsNullOrEmpty(episodeName))
            {
                throw new ArgumentNullException(nameof(episodeName));
            }

            EpisodeName = episodeName;
        }

        public string EpisodeName { get; } //TODO: Check if this deserializes correctly

        public string Name
        {
            get { return name; }
            set { SetField(ref name, value, nameof(Name)); }
        }

        public string Uri
        {
            get { return uri; }
            set { SetField(ref uri, value, nameof(Uri)); }
        }

        public TimeSpan StartPosition
        {
            get { return startPosition; }
            set { SetField(ref startPosition, value, nameof(StartPosition)); }
        }

        public override string ToString() => Name;
    }
}
