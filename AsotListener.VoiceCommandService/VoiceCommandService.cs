namespace AsotListener.VoiceCommandService
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Windows.ApplicationModel.Background;

    public sealed class VoiceCommandService : IBackgroundTask
    {
        private BackgroundTaskDeferral _deferral;
        //VoiceCommandServiceConnection _voiceServiceConnection;
        

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            throw new NotImplementedException();
        }
    }
}
