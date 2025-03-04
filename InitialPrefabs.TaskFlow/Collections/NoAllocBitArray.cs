using System;
using System.Runtime.CompilerServices;

namespace InitialPrefabs.TaskFlow.Collections {

    public ref struct NoAllocBitArrayEnumerator {
        internal Span<byte> Ptr;
        internal int Index;
        internal int Length;

        public NoAllocBitArrayEnumerator(NoAllocBitArray array) {
            Length = array.Length;
            Index = -1;
            Ptr = array.Bytes;
        }

        public readonly bool Current {
            get {
                var remappedIndex = Index / 4;
                var accessor = 1 << (Index % 4);
                var b = Ptr[remappedIndex];
                return (b & accessor) > 0;
            }
        }

        public bool MoveNext() {
            return ++Index < Length;
        }

        public void Reset() {
            Index = -1;
        }
    }

    public ref struct NoAllocBitArray {
        internal readonly Span<byte> Bytes;
        public readonly int Length;

        public NoAllocBitArray(Span<byte> bytes) {
            Bytes = bytes;
            Length = bytes.Length * 4;
        }

        public bool this[int i] {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get {
                ref var element = ref this.ElementAt(i);
                var accessor = i % 4;
                return ((1 << accessor) & element) > 0;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set {
                ref var element = ref this.ElementAt(i);
                var accessor = i % 4;
                var mask = (byte)(1 << accessor);
                if (value) {
                    element |= mask;
                } else {
                    element &= (byte)~mask;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CalculateSize(int totalBools) {
            return MathUtils.CeilToIntDivision(totalBools, 4);
        }
    }

    public static class NoAllocBitArrayExtensions {
        public static ref byte ElementAt(this ref NoAllocBitArray array, int i) {
            var index = i / 4;
            return ref array.Bytes[index];
        }
    }
}

