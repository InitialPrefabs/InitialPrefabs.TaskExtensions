using System;

namespace InitialPrefabs.TaskFlow.Threading {
    // This size is 4 bytes
    internal struct Slice : IEquatable<Slice> {
        public ushort Start;
        public ushort Count;

        public readonly bool Equals(Slice other) {
            return other.Start == Start && other.Count == Count;
        }

        public override readonly string ToString() {
            return $"Start: {Start}, Count: {Count}";
        }
    }
}

