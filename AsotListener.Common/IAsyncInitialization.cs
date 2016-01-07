namespace AsotListener.Common
{
    using System.Threading.Tasks;

    /// <summary>
    /// Marks a type as requiring asynchronous initialization and provides the result of that initialization.
    /// </summary>
    public interface IAsyncInitialization
    {
        /// <summary>
        /// The result of the asynchronous initialization.
        /// </summary>
        Task Initialization { get; }
    }
}
