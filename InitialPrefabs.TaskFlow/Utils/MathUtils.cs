using System;
using System.Runtime.CompilerServices;

namespace InitialPrefabs.TaskFlow.Utils {

    public static class MathUtils {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CeilToIntDivision(int numerator, int denominator) {
#if DEBUG
            if (denominator == 0) {
                throw new DivideByZeroException($"Cannot divide {numerator} by 0!");
            }
#endif
            return (numerator + denominator - 1) / denominator;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Min(int a, int b) {
            return a < b ? a : b;
        }
    }
}
