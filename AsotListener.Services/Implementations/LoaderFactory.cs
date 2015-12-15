namespace AsotListener.Services.Implementations
{
    using Contracts;

    public class LoaderFactory: ILoaderFactory
    {
        private IFileUtils fileUtils;
        private ILogger logger;

        public LoaderFactory(ILogger logger, IFileUtils fileUtils)
        {
            this.fileUtils = fileUtils;
            this.logger = logger;
            logger.LogMessage("LoaderFactory initialized.");
        }

        public ILoader GetLoader() => new Loader(logger, fileUtils);
    }
}
