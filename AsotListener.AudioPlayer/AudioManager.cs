namespace AsotListener.AudioPlayer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Windows.Media.Playback;

    public class AudioManager
    {
        // TODO: Use DI here
        private IPlayList playlist = Playlist.Instance;
        private MediaPlayer player = BackgroundMediaPlayer.Current; // TODO: Wrap this with some interface.

        // TODO: Think about using singleton here
        public AudioManager()
        {

        }
    }
}
