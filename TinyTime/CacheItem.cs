using System;

namespace TinyTime
{
    public class CacheItem<T>
    {
        #region Public Properties

        public T Value { get; }

        #endregion

        #region Internal Properties

        internal DateTimeOffset Created { get; } = DateTimeOffset.Now;
        internal TimeSpan ExpiresAfter { get; }

        #endregion

        #region Constructors

        public CacheItem(T value, TimeSpan expiresAfter)
        {
            Value = value;
            ExpiresAfter = expiresAfter;
        }

        #endregion
    }
}