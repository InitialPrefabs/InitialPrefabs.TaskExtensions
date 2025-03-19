using System;
using System.Collections;
using System.Collections.Generic;

namespace InitialPrefabs.TaskFlow.Collections {

    public unsafe struct FixedUInt16Array32 : IEnumerable<ushort>, IEquatable<FixedUInt16Array32> {

        public unsafe struct Enumerator : IEnumerator<ushort> {
            public ushort* Ptr;
            public int Index;
            public int Length;
            public ushort Current => Ptr[Index];

            object IEnumerator.Current => Current;

            /// <summary>
            /// No op.
            /// </summary>
            public readonly void Dispose() { }

            public bool MoveNext() {
                return ++Index < Length;
            }

            public void Reset() {
                Index = -1;
            }
        }

        public const int Capacity = 32;
        internal fixed byte Data[Capacity * sizeof(ushort)];

        public int Count {
            get;
            internal set;
        }

        public ushort this[int i] {
            get => this.ElementAt(i);
            set {
                ref var element = ref this.ElementAt(i);
                element = value;
            }
        }

        public IEnumerator<ushort> GetEnumerator() {
            fixed (byte* ptr = Data) {
                return new Enumerator {
                    Ptr = (ushort*)ptr,
                    Index = -1,
                    Length = Count
                };
            }
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        public bool Equals(FixedUInt16Array32 other) {
            if (Count != other.Count) {
                return false;
            }
            for (var i = 0; i < Count; i++) {
                if (other[i] != this[i]) {
                    return false;
                }
            }
            return true;
        }
    };

    public static class FixedUInt16Array32Extensions {

        public static void Clear(this ref FixedUInt16Array32 fixedArray) {
            fixedArray.Count = 0;
        }

        public static void Add(this ref FixedUInt16Array32 fixedArray, ushort value) {
            if (fixedArray.Count >= FixedUInt16Array32.Capacity) {
                return;
            }
            fixedArray[fixedArray.Count++] = value;
        }

        public static void RemoveAt(this ref FixedUInt16Array32 fixedArray, ushort index) {
            fixedArray.Count--;
            for (var i = index; i < fixedArray.Count; i++) {
                fixedArray[i] = fixedArray[i + 1];
            }
        }

        public static void RemoveAtSwapback(this ref FixedUInt16Array32 fixedArray, ushort index) {
            fixedArray.Count--;
            var last = fixedArray[fixedArray.Count];
            fixedArray[index] = last;
        }

        public static ref ushort ElementAt(this ref FixedUInt16Array32 fixedArray, int index) {
            unsafe {
                fixed (byte* head = fixedArray.Data) {
                    var ptr = (ushort*)head;
                    var indexed = ptr + index;
                    return ref *indexed;
                }
            }
        }
    }
}
