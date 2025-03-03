using System;

namespace InitialPrefabs.TaskFlow.Collections {
    public static class SpanExtensions {
        public static int IndexOf<T>(this ref Span<T> span, in T value, int length) where T : IEquatable<T> {
            for (var i = 0; i < length; i++) {
                if (span[i].Equals(value)) {
                    return i;
                }
            }
            return -1;
        }
    }
}


