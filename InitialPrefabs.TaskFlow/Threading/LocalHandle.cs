using System;

namespace InitialPrefabs.TaskFlow.Threading {

    public readonly struct LocalHandle<T0> : IEquatable<LocalHandle<T0>> where T0 : struct, ITaskFor {
        private readonly ushort index;

        public LocalHandle(ushort index) {
            this.index = index;
        }

        public bool Equals(LocalHandle<T0> other) {
            return other.index == index;
        }

        public static implicit operator ushort(LocalHandle<T0> value) {
            return value.index;
        }

        public static implicit operator LocalHandle<T0>(ushort value) {
            return new LocalHandle<T0>(value);
        }
    }
}


