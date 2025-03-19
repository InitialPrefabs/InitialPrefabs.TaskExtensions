using System;

namespace InitialPrefabs.TaskFlow.Utils {

    public delegate void ExceptionHandler(Exception err);

    public static class LogUtils {

        private static ExceptionHandler ExceptionHandler;

        public static event ExceptionHandler OnException {
            add {
                ExceptionHandler -= value;
                ExceptionHandler += value;
            }
            remove => ExceptionHandler -= value;
        }

        public static void Emit(Exception err) {
            ExceptionHandler?.Invoke(err);
        }
    }
}

