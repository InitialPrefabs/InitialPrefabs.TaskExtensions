using System;

namespace InitialPrefabs.TaskFlow.Utils {

    public delegate void ExceptionHandler(Exception err);
    public delegate void LogHandler(string msg);

    public static class LogUtils {

        private static ExceptionHandler ExceptionHandler;
        private static LogHandler LogHandler;

        public static event ExceptionHandler OnException {
            add {
                ExceptionHandler -= value;
                ExceptionHandler += value;
            }
            remove => ExceptionHandler -= value;
        }

        public static event LogHandler OnLog {
            add {
                LogHandler -= value;
                LogHandler += value;
            }
            remove => LogHandler -= value;
        }

        public static void Emit(Exception err) {
            ExceptionHandler?.Invoke(err);
        }

        public static void Emit(string msg) {
            LogHandler?.Invoke(msg);
        }
    }
}

