using System;

namespace OpenUtau.Core.Util {
    public abstract class SingletonBase<T> where T : new() {
        private static readonly Lazy<T> inst = new Lazy<T>(() => new T(), true);
        public static T Inst => inst.Value;
    }
}
