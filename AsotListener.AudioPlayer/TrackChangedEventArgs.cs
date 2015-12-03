﻿namespace AsotListener.AudioPlayer
{
    using System;
    using Models;

    internal class TrackChangedEventArgs: EventArgs
    {
        public AudioTrack Track { get; }

        public TrackChangedEventArgs(AudioTrack track)
        {
            Track = track;
        }
    }
}