using InitialPrefabs.TaskFlow.Collections;
using System;
using System.Runtime.CompilerServices;

namespace InitialPrefabs.TaskFlow {

    public struct TaskHandle<T0> where T0 : struct, ITaskFor {
        private readonly Handle<T0> handle;

        // TODO: Add the dependencies, the dependencies need to store the handles.
        public FixedUInt16Array32 Dependencies;

        internal readonly void Map<T>() where T : struct, ITaskFor{
            foreach (var handleIndex in Dependencies) {
            }
        }
    }
}