using System;

namespace InitialPrefabs.TaskFlow.Threading {

    public sealed class TaskFaultException : Exception {

        public TaskFaultException() : base() { }
        public TaskFaultException(string msg) : base(msg) { }
        public TaskFaultException(string msg, Exception inner) : base(msg, inner) { }
    }
}
