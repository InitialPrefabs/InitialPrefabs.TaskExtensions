using System;

namespace InitialPrefabs.TaskFlow.Threading {

    public readonly struct LocalHandle : IEquatable<LocalHandle> {
        private readonly ushort index;

        public LocalHandle(ushort index) {
            this.index = index;
        }

        public bool Equals(LocalHandle other) {
            return other.index == index;
        }

        public static implicit operator ushort(LocalHandle value) {
            return value.index;
        }

        public static implicit operator LocalHandle(ushort value) {
            return new LocalHandle(value);
        }
    }
}


