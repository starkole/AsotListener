namespace AsotListener.Services.Implementations
{
    using Contracts;
    using Windows.Foundation.Diagnostics;

    public class LoaderFactory: ILoaderFactory
    {
        private readonly IFileUtils fileUtils;
        private readonly ILogger logger;

        public LoaderFactory(ILogger logger, IFileUtils fileUtils)
        {
            this.fileUtils = fileUtils;
            this.logger = logger;
            logger.LogMessage("LoaderFactory initialized.", LoggingLevel.Information);
        }

        public ILoader GetLoader() => new Loader(logger, fileUtils);
    }
}
