using InitialPrefabs.TaskFlow.Collections;
using System;

namespace InitialPrefabs.TaskFlow.Threading {

    public class Payload {
        public UnmanagedRef<TaskMetadata> TaskMetadata;
        public Action TaskAction;
        public Action OnComplete;
        public int Index;
        public ushort WrapperIndex;
    }
}

