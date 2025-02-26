namespace InitialPrefabs.TaskFlow.Collections {

    public static class FixedUInt16Array32Extensions {
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

    public unsafe struct FixedUInt16Array32 {
        public const int Capacity = 32;
        internal fixed byte Data[Capacity * sizeof(ushort)];

        public int Count {
            get;
            internal set;
        }

        public ushort this[int i] {
            get {
                return this.ElementAt(i);
            }
            set {
                ref var element = ref this.ElementAt(i);
                element = value;
            }
        }
    };
}
