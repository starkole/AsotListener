namespace AsotListener.Services.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Models;
    using Models.Enums;
    using Services.Contracts;
    using Windows.ApplicationModel.Activation;
    using Windows.Media.Playback;
    using Windows.Media.SpeechRecognition;
    using Windows.Media.SpeechSynthesis;
    using Windows.UI.Xaml;
    public sealed class VoiceCommandsHandler : IVoiceCommandsHandler
    {
        private readonly ILogger logger;
        private readonly INavigationService navigationService;
        private readonly IApplicationSettingsHelper applicationSettingsHelper;
        private readonly IEpisodeListManager episodeListManager;
        private readonly IPlaybackManager playbackManager;

        private EpisodeList episodeList => EpisodeList.Instance;
        private Playlist playlist => Playlist.Instance;
        private MediaPlayer mediaPlayer => BackgroundMediaPlayer.Current;
        
        public VoiceCommandsHandler(
            ILogger logger,
            INavigationService navigationService,
            IApplicationSettingsHelper applicationSettingsHelper,
            IEpisodeListManager episodeListManager,
            IPlaybackManager playbackManager)
        {
            this.playbackManager = playbackManager;
            this.episodeListManager = episodeListManager;
            this.applicationSettingsHelper = applicationSettingsHelper;
            this.navigationService = navigationService;
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
                        var ep = episodeList.LastOrDefault(e => e.Status== EpisodeStatus.Loaded );
                        if (ep == null)
                        {
                            // TODO: Speak error
                            return;
                        }

                        playlist.Clear();
                        await episodeListManager.PlayEpisode(ep);
                    break;
                case "playEpisodeByNumber":
                    if (isVoiceCommand(args.Result))
                    {
                        int episodeNumber = -1;
                        var spokenNumber = args.Result.SemanticInterpretation.Properties["number"].FirstOrDefault();
                        int.TryParse(spokenNumber, out episodeNumber);

                        // TODO: Use constants here
                        if (episodeNumber < 1 || episodeNumber > 745)
                        {
                            await SpeakText($"Cannot play episode with number {episodeNumber}");
                            return;
                        }

                        // TODO: Use better search algorithm here
                        var episodeToPlay = episodeList.FirstOrDefault(e => e.Name.Contains(episodeNumber.ToString()));
                        if (episodeToPlay == null)
                        {
                            await SpeakText($"Cannot find episode with number {episodeNumber}");
                            return;
                        }

                        playlist.Clear();
                        await episodeListManager.PlayEpisode(episodeToPlay);
                    }
                    break;
                case "startPlayback":
                    if (playlist.CurrentTrack != null)
                    {
                        playbackManager.Play();
                    }
                    break;
                case "pausePlayback":
                    if (mediaPlayer.CanPause)
                    {
                        mediaPlayer.Pause();
                    }
                    break;
                case "goForward":
                    string interval = args?.Result.SemanticInterpretation.Properties["interval"].FirstOrDefault();
                    string howMany = args?.Result.SemanticInterpretation.Properties["number"].FirstOrDefault();
                    if (string.IsNullOrEmpty(interval))
                    {
                        // TODO: Speak about some error.
                        return;
                    }
                    // TODO: Add logic here
                    break;
                case "goBack":
                    // TODO: Add logic here
                    break;
                case "checkForUpdates":
                    int oldEpisodesCount = episodeList.Count;
                    await episodeListManager.LoadEpisodeListFromServer();
                    int delta = episodeList.Count - oldEpisodesCount;
                    string message = "Update complete. ";
                    if (delta == 1)
                    {
                        message += "There is one new episode.";
                    } else if (delta > 1)
                    {
                        message += $"There are {delta} new episodes.";
                    } else
                    {
                        message += "There are no new episodes yet.";
                    }
                    await SpeakText(message);
                    break;
            }
        }

        private async Task initializeAsync()
        {
            await applicationSettingsHelper.Initialization;
        }

        private static bool isVoiceCommand(SpeechRecognitionResult commandResult) =>
            commandResult?.SemanticInterpretation?.Properties["commandMode"]?.FirstOrDefault() == "voice";



        // TODO: Test if this works
        private TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();

        private async Task SpeakText(string text)
        {
            using (var synthesizer = new SpeechSynthesizer())
            using (var stream = await synthesizer.SynthesizeTextToStreamAsync(text))
            {
                if (mediaPlayer.CanPause)
                {
                    mediaPlayer.Pause();
                }
                mediaPlayer.AutoPlay = true;
                mediaPlayer.SetStreamSource(stream);
                mediaPlayer.MediaEnded -= onSpeechEnded;
                mediaPlayer.MediaEnded += onSpeechEnded;
                await tcs.Task;
            }
        }

        private void onSpeechEnded(MediaPlayer sender, object args)
        {
            mediaPlayer.MediaEnded -= onSpeechEnded;
            tcs.SetResult(true);
        }
    }
}
