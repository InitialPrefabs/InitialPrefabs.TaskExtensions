using System;
using System.Runtime.CompilerServices;

namespace InitialPrefabs.TaskExtensions {
    public static class Utils {

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CeilToIntDivision(int numerator, int denominator) {
#if UNITY_EDITOR || DEBUG
            if (denominator == 0) {
                throw new DivideByZeroException($"Cannot divide {numerator} by 0!");
            }
#endif
            return (numerator + denominator - 1) / denominator;
        }
    }
}
