namespace AsotListener.Services.Implementations
{
    using Contracts;

    public class LoaderFactory: ILoaderFactory
    {
        private readonly IFileUtils fileUtils;
        private readonly ILogger logger;

        public LoaderFactory(ILogger logger, IFileUtils fileUtils)
        {
            this.fileUtils = fileUtils;
            this.logger = logger;
            logger.LogMessage("LoaderFactory initialized.");
        }

        public ILoader GetLoader() => new Loader(logger, fileUtils);
    }
}
