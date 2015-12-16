namespace AsotListener.App.ViewModels
{
    using Models;
    using Services.Contracts;

    public class MainPageViewModel : BaseModel
    {
        #region Properties

        public PlayerViewModel PlayerModel { get; }
        public EpisodesViewModel EpisodesModel { get; }

        #endregion

        #region Ctor

        public MainPageViewModel(PlayerViewModel playerModel, EpisodesViewModel episodesViewModel, ILogger logger)
        {
            PlayerModel = playerModel;
            EpisodesModel = episodesViewModel;
            logger.LogMessage("Main page view model created.");
        }

        #endregion            
    }
}
