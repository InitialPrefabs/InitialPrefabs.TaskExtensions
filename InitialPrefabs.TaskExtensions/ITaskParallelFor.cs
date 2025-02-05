namespace InitialPrefabs.TaskExtensions {

    /// <summary>
    /// Represents a unit of work that needs to be executed among many threads.
    /// </summary>
    public interface ITaskParallelFor {
        void Execute(int index);
    }

    /// <summary>
    /// Represents a unit of work that needs to be executed on a single thread.
    /// </summary>
    public interface ITask {
        void Execute();
    }
}
