namespace AsotListener.Services.Implementations
{
    using System;
    using System.Threading.Tasks;
    using Common;
    using Contracts;
    using Windows.Media.SpeechSynthesis;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;

    public sealed class TextSpeaker : ITextSpeaker
    {
        private readonly MediaElement mediaElement;
        private readonly IApplicationSettingsHelper applicationSettingsHelper;
        private readonly ILogger logger;
        private readonly IPlaybackManager playbackManager;
        private TaskCompletionSource<bool> taskCompletionSource;

        private bool isBackgroundTaskRunning => applicationSettingsHelper.ReadSettingsValue<bool>(Keys.IsBackgroundTaskRunning);

        // TODO: Add documentation.
        // TODO: Add logging.
        public TextSpeaker(ILogger logger, IApplicationSettingsHelper applicationSettingsHelper, IPlaybackManager playbackManager)
        {
            this.playbackManager = playbackManager;
            this.logger = logger;
            this.applicationSettingsHelper = applicationSettingsHelper;

            mediaElement = new MediaElement { Visibility = Visibility.Collapsed };
            attachMediElement();
        }

        private void attachMediElement()
        {
            Frame frame = Window.Current.Content as Frame;
            if (frame == null)
            {
                Window.Current.Content = new Page { Content = mediaElement };
                return;
            }

            Page page = frame.Content as Page;
            if (page == null)
            {
                frame.Content = new Page { Content = mediaElement };
                return;
            }

            Panel panel = page.Content as Panel;
            if (panel == null)
            {
                page.Content = mediaElement;
                return;
            }

            panel.Children.Add(mediaElement);
        }
        
        public async Task SpeakText(string text)
        {
            using (var synthesizer = new SpeechSynthesizer())
            using (SpeechSynthesisStream stream = await synthesizer.SynthesizeTextToStreamAsync(text))
            {
                taskCompletionSource = new TaskCompletionSource<bool>();
                bool haveToResumePlayback = isBackgroundTaskRunning;
                mediaElement.MediaEnded += onSpeechEnded;
                mediaElement.SetSource(stream, stream.ContentType);
                await taskCompletionSource.Task;
                if (haveToResumePlayback)
                {
                    playbackManager.Play();
                }
            }
        }

        private void onSpeechEnded(object sender, object args)
        {
            mediaElement.MediaEnded -= onSpeechEnded;
            taskCompletionSource.SetResult(true);
        }
    }
}
