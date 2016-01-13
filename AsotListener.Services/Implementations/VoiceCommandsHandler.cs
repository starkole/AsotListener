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

    /// <summary>
    /// Contains logic for processing voice commands
    /// </summary>
    public sealed class VoiceCommandsHandler : IVoiceCommandsHandler
    {
        #region Private Declarations

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

        private Playlist playlist => Playlist.Instance;

        #endregion

        #region Public Properties

        /// <summary>
        /// The result of the asynchronous initialization.
        /// </summary>
        public Task Initialization { get; }

        #endregion

        #region Ctor
        
        /// <summary>
        /// Creates ne instance of <see cref="VoiceCommandsHandler"/>
        /// </summary>
        /// <param name="logger">Instance of <see cref="ILogger"/></param>
        /// <param name="applicationSettingsHelper">Instance of <see cref="IApplicationSettingsHelper"/></param>
        /// <param name="episodeListManager">Instance of <see cref="IEpisodeListManager"/></param>
        /// <param name="playbackManager">Instance of <see cref="IPlaybackManager"/></param>
        /// <param name="textSpeaker">Instance of <see cref="ITextSpeaker"/></param>
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
            logger.LogMessage("Voice commands handler initialized.", LoggingLevel.Information);
        }

        #endregion

        #region Piblic Methods

        /// <summary>
        /// Handles voice command defined by given voice command arguments
        /// </summary>
        /// <param name="args">Arguments that define voice command</param>
        /// <returns>Awaitable <see cref="Task"/></returns>
        public async Task HandleVoiceCommnadAsync(VoiceCommandActivatedEventArgs args)
        {
            string commandName = args?.Result.RulePath.FirstOrDefault();
            logger.LogMessage($"Voice commands handler: Processing {commandName} command...");
            switch (commandName)
            {
                case "playTheLastEpisode":
                    await episodeListManager.PlayLastDownloadedEpisodeAsync();
                    playbackManager.Play();
                    break;
                case "playEpisodeByNumber":
                    if (isVoiceCommand(args.Result))
                    {
                        int episodeNumber = -1;
                        var spokenNumber = args.Result.SemanticInterpretation.Properties["number"].FirstOrDefault();
                        int.TryParse(spokenNumber, out episodeNumber);
                        var episodeToPlay = episodeListManager.GetEpisodeByNumber(episodeNumber);
                        if (episodeToPlay == null)
                        {
                            // TODO: Speak error
                            logger.LogMessage($"VoiceCommandHandler: Cannot play episode with number {episodeNumber}.", LoggingLevel.Warning);
                            return;
                        }

                        if (!episodeToPlay.CanBePlayed)
                        {
                            // TODO: Speak error
                            logger.LogMessage($"VoiceCommandHandler: Found episode with number {episodeNumber}, but cannot play it", LoggingLevel.Warning);
                            return;
                        }

                        playlist.Clear();
                        await episodeListManager.PlayEpisodeAsync(episodeToPlay);
                        playbackManager.Play();
                        await episodeListManager.UpdateEpisodeStatesAsync();
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
                    int newEpisodesCount = await episodeListManager.LoadEpisodeListFromServerAsync();
                    string message = "Update complete. ";
                    if (newEpisodesCount == 1)
                    {
                        message += "There is one new episode.";
                    }
                    else if (newEpisodesCount > 1)
                    {
                        message += $"There are {newEpisodesCount} new episodes.";
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

        #endregion

        #region Private Methods

        private async Task initializeAsync()
        {
            await applicationSettingsHelper.Initialization;
        }

        private static bool isVoiceCommand(SpeechRecognitionResult commandResult) =>
            commandResult?.SemanticInterpretation?.Properties["commandMode"]?.FirstOrDefault() == "voice";

        private void changePosition(IReadOnlyDictionary<string, IReadOnlyList<string>> properties, Direction direction)
        {
            logger.LogMessage("Voice commands handler: Changing current position...");
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

        #endregion
    }
}
