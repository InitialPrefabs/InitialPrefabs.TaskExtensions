using System;

namespace InitialPrefabs.TaskFlow.Threading {

    public static class TaskGraphManager {

        internal delegate void ResetHandler();

        // TODO: I dont really like this, but I need to check and see if it's something to keep at
        internal sealed class ShutdownHandler {
            private Action onShutdown;

            public void RegisterShutdown(ResetHandler resetHandler) {
                onShutdown += () => {
                    _OnReset -= resetHandler;
                };
            }

            ~ShutdownHandler() {
                onShutdown?.Invoke();
                onShutdown = null;
            }
        }

        private static event ResetHandler _OnReset;

        internal static event ResetHandler OnReset {
            add {
                _OnReset -= value;
                _OnReset += value;
                _ShutdownHandler.RegisterShutdown(value);
            }

            remove => _OnReset -= value;
        }

        internal static readonly ShutdownHandler _ShutdownHandler = new ShutdownHandler();

        public static TaskGraph Graph => Default;

        internal static TaskGraph Default;
        internal static ushort UniqueID;

        public static TaskGraph Initialize(int capacity = 5) {
            Default = new TaskGraph(capacity);
            UniqueID = 0;
            return Default;
        }

        public static void ResetContext() {
            UniqueID = 0;
            Default.Reset();
            _OnReset?.Invoke();
        }

        public static void Process() {
            Default.Sort();
            Default.Process();
        }

        public static void Shutdown() {
            Default.Reset();
            Default = null;
            UniqueID = 0;
        }
    }
}
