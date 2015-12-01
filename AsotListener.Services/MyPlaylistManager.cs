namespace AsotListener.Services
{
    /// <summary>
    /// Manage playlist information. For simplicity of this sample, we allow only one playlist
    /// </summary>
    public sealed class MyPlaylistManager
    {
        #region Private members
        private static MyPlaylist instance;
        #endregion

        #region Playlist management methods/properties
        public MyPlaylist Current
        {
            get
            {
                if (instance == null)
                {
                    instance = new MyPlaylist();
                }
                return instance;
            }
        }

        /// <summary>
        /// Clears playlist for re-initialization
        /// </summary>
        public void ClearPlaylist()
        {
            instance = null;
        }
        #endregion
    }
}
