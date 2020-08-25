using System;
using System.Runtime.InteropServices;

namespace MemoryCache.Net
{
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    public struct CacheEntry<T>
    {
        public bool Serializable;
        public string Key;
        public T Value;
    }
}
