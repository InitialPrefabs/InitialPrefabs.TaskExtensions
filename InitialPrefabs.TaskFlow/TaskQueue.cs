using InitialPrefabs.TaskFlow.Collections;

namespace InitialPrefabs.TaskFlow {

    public struct TaskHandle<T0> where T0 : struct, ITaskFor {
        private readonly Handle<T0> handle;
        // TODO: Add the dependencies, the dependencies need to store the handles.
    }
}

