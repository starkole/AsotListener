namespace AsotListener.Services.Implementations
{
    using Contracts;
    using Windows.Foundation.Diagnostics;

    /// <summary>
    /// Factory for creating <see cref="ILoader"/> instances
    /// </summary>
    public class LoaderFactory: ILoaderFactory
    {
        private readonly ILogger logger;

        /// <summary>
        /// Creates instance of <see cref="LoaderFactory"/>
        /// </summary>
        /// <param name="logger">The logger instance</param>
        public LoaderFactory(ILogger logger)
        {
            this.logger = logger;
            logger.LogMessage("LoaderFactory initialized.", LoggingLevel.Information);
        }

        /// <summary>
        /// Creates new <see cref="ILoader"/> instance
        /// </summary>
        /// <returns><see cref="ILoader"/> instance</returns>
        public ILoader GetLoader() => new Loader(logger);
    }
}
