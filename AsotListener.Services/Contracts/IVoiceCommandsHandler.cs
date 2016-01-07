namespace AsotListener.Services.Contracts
{
    using System.Threading.Tasks;
    using Windows.ApplicationModel.Activation;
    using Common;

    public interface IVoiceCommandsHandler: IAsyncInitialization
    {
        Task HandleVoiceCommnadAsync(VoiceCommandActivatedEventArgs args);
    }
}
