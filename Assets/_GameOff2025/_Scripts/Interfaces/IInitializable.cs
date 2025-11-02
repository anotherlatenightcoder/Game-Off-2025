namespace Route24.Core
{
    /// <summary>
    /// Marks a system to be part of the initialization workflow and will be
    /// initialized by the InitializationOrderManager. 
    /// </summary>
    public interface IInitializable
    {
        /// <summary>
        /// Determines the order in which the system runs.
        /// Lower numbers = run first.
        /// </summary>
        int InitializationPriority { get; }

        /// <summary>
        /// Called during the controlled initialization phase.
        /// </summary>
        void Initialize();
    }
}
