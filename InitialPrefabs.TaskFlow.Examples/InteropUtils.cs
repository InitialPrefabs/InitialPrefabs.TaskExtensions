using System;
using System.Runtime.InteropServices;

namespace InitialPrefabs.TaskFlow.Examples {
    public static class InteropUtils {
        public static nuint SizeOf<T>(Span<T> span) where T : unmanaged {
            unsafe {
                return (nuint)(span.Length * sizeof(T));
            }
        }
    }
}


