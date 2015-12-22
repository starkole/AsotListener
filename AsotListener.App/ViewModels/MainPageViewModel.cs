namespace AsotListener.App.ViewModels
{
    using Models;
    using Services.Contracts;

    /// <summary>
    /// View model, that holds view models of nested pages
    /// </summary>
    public sealed class MainPageViewModel : BaseModel
    {
        #region Properties

        /// <summary>
        /// Audio player view model instance
        /// </summary>
        public PlayerViewModel PlayerModel { get; }

        /// <summary>
        /// Episode list view model instance
        /// </summary>
        public EpisodesViewModel EpisodesModel { get; }

        #endregion

        #region Ctor

        /// <summary>
        /// Initializes new instance of <see cref="MainPageViewModel"/>
        /// </summary>
        /// <param name="playerModel">Instance of <see cref="PlayerViewModel"/></param>
        /// <param name="episodesViewModel">Instance of <see cref="EpisodesViewModel"/></param>
        /// <param name="logger"></param>
        public MainPageViewModel(PlayerViewModel playerModel, EpisodesViewModel episodesViewModel, ILogger logger)
        {
            PlayerModel = playerModel;
            EpisodesModel = episodesViewModel;
            logger.LogMessage("Main page view model created.");
        }

        #endregion            
    }
}
