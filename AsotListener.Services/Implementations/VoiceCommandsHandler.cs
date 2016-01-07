namespace AsotListener.Services.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Models;
    using Models.Enums;
    using Services.Contracts;
    using Windows.ApplicationModel.Activation;
    using Windows.Foundation.Diagnostics;
    using Windows.Media.Playback;
    using Windows.Media.SpeechRecognition;

    public sealed class VoiceCommandsHandler : IVoiceCommandsHandler
    {
        private enum Direction
        {
            Forward,
            Backward
        }

        const string intervalKey = "interval";
        const string numberKey = "number";

        private readonly ILogger logger;
        private readonly IApplicationSettingsHelper applicationSettingsHelper;
        private readonly IEpisodeListManager episodeListManager;
        private readonly IPlaybackManager playbackManager;
        private readonly ITextSpeaker textSpeaker;

        private EpisodeList episodeList => EpisodeList.Instance;
        private Playlist playlist => Playlist.Instance;
        private MediaPlayer mediaPlayer => BackgroundMediaPlayer.Current;

        // TODO: Update documentation.
        // TODO: Add logging
        public VoiceCommandsHandler(
            ILogger logger,
            IApplicationSettingsHelper applicationSettingsHelper,
            IEpisodeListManager episodeListManager,
            IPlaybackManager playbackManager,
            ITextSpeaker textSpeaker)
        {
            this.textSpeaker = textSpeaker;
            this.playbackManager = playbackManager;
            this.episodeListManager = episodeListManager;
            this.applicationSettingsHelper = applicationSettingsHelper;
            this.logger = logger;

            Initialization = initializeAsync();
        }

        public Task Initialization { get; }

        public async Task HandleVoiceCommnadAsync(VoiceCommandActivatedEventArgs args)
        {
            string commandName = args?.Result.RulePath.FirstOrDefault();
            switch (commandName)
            {
                case "playTheLastEpisode":
                    // TODO: Move the latter logic to episode list manager
                    var ep = episodeList.FirstOrDefault(e => e.CanBePlayed);
                    if (ep == null)
                    {
                        // TODO: Speak error
                        logger.LogMessage("VoiceCommandHandler: Cannot play the last downloaded episode. No episodes found.", LoggingLevel.Error);
                        return;
                    }

                    playlist.Clear();
                    await episodeListManager.PlayEpisode(ep);
                    playbackManager.Play();
                    await episodeListManager.UpdateEpisodeStates();
                    break;
                case "playEpisodeByNumber":
                    if (isVoiceCommand(args.Result))
                    {
                        int episodeNumber = -1;
                        var spokenNumber = args.Result.SemanticInterpretation.Properties["number"].FirstOrDefault();
                        int.TryParse(spokenNumber, out episodeNumber);

                        // TODO: Move the latter logic to episode list manager
                        // TODO: Use constants here
                        if (episodeNumber < 1 || episodeNumber > 745)
                        {
                            // TODO: Speak error
                            logger.LogMessage("VoiceCommandHandler: Cannot play episode by number. The number is out of range.", LoggingLevel.Error);

                            return;
                        }

                        // TODO: Use better search algorithm here
                        var episodeToPlay = episodeList.FirstOrDefault(e => e.Name.Contains(episodeNumber.ToString()) && e.CanBePlayed);
                        if (episodeToPlay == null)
                        {
                            // TODO: Speak error
                            logger.LogMessage($"VoiceCommandHandler: Cannot play episode with number {episodeNumber}.", LoggingLevel.Error);
                            return;
                        }

                        playlist.Clear();
                        await episodeListManager.PlayEpisode(episodeToPlay);
                        playbackManager.Play();
                        await episodeListManager.UpdateEpisodeStates();
                    }
                    break;
                case "startPlayback":
                    if (playlist.CurrentTrack != null)
                    {
                        playbackManager.Play();
                    }
                    break;
                case "pausePlayback":
                    playbackManager.SchedulePause();
                    break;
                case "goForward":
                    changePosition(args?.Result.SemanticInterpretation.Properties, Direction.Forward);
                    break;
                case "goBack":
                    changePosition(args?.Result.SemanticInterpretation.Properties, Direction.Backward);
                    break;
                case "nextTrack":
                case "nextEpisode":
                    playbackManager.GoToNextTrack();
                    break;
                case "previousTrack":
                case "previousEpisode":
                    playbackManager.GoToPreviousTrack();
                    break;
                case "checkForUpdates":
                    int oldEpisodesCount = episodeList.Count;
                    await episodeListManager.LoadEpisodeListFromServer();
                    int delta = episodeList.Count - oldEpisodesCount;
                    string message = "Update complete! ";
                    if (delta == 1)
                    {
                        message += "There is one new episode.";
                    }
                    else if (delta > 1)
                    {
                        message += $"There are {delta} new episodes.";
                    }
                    else
                    {
                        message += "There are no new episodes yet!";
                    }
                    // TODO: Speak result
                    // await textSpeaker.SpeakText(message);
                    break;
            }
        }

        private async Task initializeAsync()
        {
            await applicationSettingsHelper.Initialization;
        }

        private static bool isVoiceCommand(SpeechRecognitionResult commandResult) =>
            commandResult?.SemanticInterpretation?.Properties["commandMode"]?.FirstOrDefault() == "voice";

        private void changePosition(IReadOnlyDictionary<string, IReadOnlyList<string>> properties, Direction direction)
        {
            if (properties == null || !properties.ContainsKey(intervalKey) || !properties.ContainsKey(numberKey))
            {
                // TODO: Speak error
                logger.LogMessage("VoiceCommandHandler: No parameters specified for changing position.", LoggingLevel.Error);
                return;
            }

            var interval = convertStringToNavigationInterval(properties[intervalKey].FirstOrDefault());
            if (interval == NavigationInterval.Unspecified)
            {
                // TODO: Speak error
                logger.LogMessage("VoiceCommandHandler: Invalid interval specified for changing position.", LoggingLevel.Error);
                return;
            }

            int count = 0;
            int.TryParse(properties[numberKey].FirstOrDefault(), out count);
            if (count <= 0)
            {
                // TODO: Speak error
                logger.LogMessage("VoiceCommandHandler: Invalid number specified for changing position.", LoggingLevel.Error);
                return;
            }
            if (direction == Direction.Backward)
            {
                count = -count;
            }

            logger.LogMessage($"VoiceCommandHandler: Sending command to navigate {count} {interval} {direction}.", LoggingLevel.Information);
            playbackManager.Navigate(count, interval);
        }

        private NavigationInterval convertStringToNavigationInterval(string stringToConvert)
        {
            switch (stringToConvert)
            {
                case "second":
                case "seconds":
                    return NavigationInterval.Second;
                case "minute":
                case "minutes":
                    return NavigationInterval.Minute;
                case "hour":
                case "hours":
                    return NavigationInterval.Hour;
                case "track":
                case "tracks":
                    return NavigationInterval.Track;
                case "episode":
                case "episodes":
                    return NavigationInterval.Episode;
            }

            return NavigationInterval.Unspecified;
        }
    }
}
