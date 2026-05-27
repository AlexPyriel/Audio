using System;
using System.Collections.Generic;
using LocalDataStorage;

namespace Audio.Tests.EditMode
{
    /// <summary>
    /// In-memory <see cref="ILocalDataStorage"/> fake for EditMode tests. Keeps a per-type entry and
    /// exposes call counters and the last saved payload for assertions.
    /// </summary>
    internal sealed class InMemoryLocalDataStorage : ILocalDataStorage
    {
        private readonly Dictionary<Type, LocalData> _store = new Dictionary<Type, LocalData>();

        /// <summary>
        /// Total number of <see cref="Save{TData}"/> calls received since construction.
        /// </summary>
        internal int SaveCount { get; private set; }

        /// <summary>
        /// The last payload passed to <see cref="Save{TData}"/>, or <see langword="null"/>.
        /// </summary>
        internal LocalData LastSaved { get; private set; }

        /// <inheritdoc />
        public void Save<TData>(TData data) where TData : LocalData
        {
            if (data == null)
            {
                _store.Remove(typeof(TData));
            }
            else
            {
                _store[typeof(TData)] = data;
            }
            SaveCount++;
            LastSaved = data;
        }

        /// <inheritdoc />
        public bool TryLoad<TData>(out TData data) where TData : LocalData
        {
            LocalData stored;
            if (_store.TryGetValue(typeof(TData), out stored) && stored is TData typed)
            {
                data = typed;
                return true;
            }
            data = null;
            return false;
        }

        /// <inheritdoc />
        public void Delete<TData>() where TData : LocalData
        {
            _store.Remove(typeof(TData));
        }

        /// <inheritdoc />
        public void DeleteAll()
        {
            _store.Clear();
        }
    }
}
