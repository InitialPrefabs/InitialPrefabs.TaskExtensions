using System;
using System.Runtime.CompilerServices;

namespace InitialPrefabs.TaskFlow {

    public static class MathUtils {
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static int CeilToIntDivision(int numerator, int denominator) {
#if UNITY_EDITOR || DEBUG
            if (denominator == 0) {
                throw new DivideByZeroException($"Cannot divide {numerator} by 0!");
            }
#endif
            return (numerator + denominator - 1) / denominator;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static int Min(int a, int b) {
            return a < b ? a : b;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static int RoundToInt(float value) {
            return (int)(value + (0.5f * (value < 0 ? -1 : 1)));
        }
    }
}
