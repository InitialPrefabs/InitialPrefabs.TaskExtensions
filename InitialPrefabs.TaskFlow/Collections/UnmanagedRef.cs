using System;

namespace InitialPrefabs.TaskFlow.Collections {

    public readonly struct UnmanagedRef<T> where T : unmanaged {

        internal readonly bool IsValid => Pointer == IntPtr.Zero;

        internal readonly IntPtr Pointer;
        public ref T Ref {
            get {
                unsafe {
                    var ptr = (T*)Pointer.ToPointer();
                    return ref *ptr;
                }
            }
        }

        public UnmanagedRef(ref T element) {
            unsafe {
                fixed (void* ptr = &element) {
                    Pointer = new IntPtr(ptr);
                }
            }
        }
    }
}

