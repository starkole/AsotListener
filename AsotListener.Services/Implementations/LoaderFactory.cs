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
        private readonly IParser parser;

        /// <summary>
        /// Creates instance of <see cref="LoaderFactory"/>
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> instance</param>
        /// <param name="parser">The <see cref="IParser"/> instance</param>
        public LoaderFactory(ILogger logger, IParser parser)
        {
            this.parser = parser;
            this.logger = logger;
            logger.LogMessage("LoaderFactory initialized.", LoggingLevel.Information);
        }

        /// <summary>
        /// Creates new <see cref="ILoader"/> instance
        /// </summary>
        /// <returns><see cref="ILoader"/> instance</returns>
        public ILoader GetLoader() => new Loader(logger, parser);
    }
}
