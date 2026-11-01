using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using LocalDataStorage;

namespace Audio.Tests.EditMode
{
    /// <summary>
    /// In-memory <see cref="ILocalDataStorage"/> fake for EditMode tests. Keeps a per-(type, slot)
    /// entry and exposes call counters and the last saved payload for assertions. Supports the full
    /// v2 interface — slots, async, events — through a single map.
    /// </summary>
    internal sealed class InMemoryLocalDataStorage : ILocalDataStorage
    {
        private readonly Dictionary<(Type Type, string Slot), LocalData> _store = new();

        /// <summary>Total number of <see cref="Save{TData}(TData)"/> calls received since construction.</summary>
        internal int SaveCount { get; private set; }

        /// <summary>The last payload passed to <see cref="Save{TData}(TData)"/>, or <see langword="null"/>.</summary>
        internal LocalData LastSaved { get; private set; }

        /// <inheritdoc />
        public event Action<Type> DataSaved;
        /// <inheritdoc />
        public event Action<Type> DataDeleted;
        /// <inheritdoc />
        public event Action AllDeleted;

        /// <inheritdoc />
        public void Save<TData>(TData data) where TData : LocalData => Save("", data);

        /// <inheritdoc />
        public void Save<TData>(string slot, TData data) where TData : LocalData
        {
            if (data == null)
            {
                _store.Remove((typeof(TData), slot ?? string.Empty));
                DataDeleted?.Invoke(typeof(TData));
                return;
            }

            _store[(typeof(TData), slot ?? string.Empty)] = data;
            SaveCount++;
            LastSaved = data;
            DataSaved?.Invoke(typeof(TData));
        }

        /// <inheritdoc />
        public bool TryLoad<TData>(out TData data) where TData : LocalData => TryLoad("", out data);

        /// <inheritdoc />
        public bool TryLoad<TData>(string slot, out TData data) where TData : LocalData
        {
            if (_store.TryGetValue((typeof(TData), slot ?? string.Empty), out LocalData stored) && stored is TData typed)
            {
                data = typed;
                return true;
            }

            data = null;
            return false;
        }

        /// <inheritdoc />
        public void Delete<TData>() where TData : LocalData => Delete<TData>("");

        /// <inheritdoc />
        public void Delete<TData>(string slot) where TData : LocalData
        {
            _store.Remove((typeof(TData), slot ?? string.Empty));
            DataDeleted?.Invoke(typeof(TData));
        }

        /// <inheritdoc />
        public IEnumerable<string> ListSlots<TData>() where TData : LocalData
        {
            foreach (var pair in _store)
            {
                if (pair.Key.Type == typeof(TData))
                    yield return pair.Key.Slot;
            }
        }

        /// <inheritdoc />
        public void DeleteAll()
        {
            _store.Clear();
            AllDeleted?.Invoke();
        }

        /// <inheritdoc />
        public UniTask SaveAsync<TData>(TData data, CancellationToken cancellationToken = default) where TData : LocalData
        {
            Save(data);
            return UniTask.CompletedTask;
        }

        /// <inheritdoc />
        public UniTask SaveAsync<TData>(string slot, TData data, CancellationToken cancellationToken = default) where TData : LocalData
        {
            Save(slot, data);
            return UniTask.CompletedTask;
        }

        /// <inheritdoc />
        public UniTask<(bool Found, TData Data)> TryLoadAsync<TData>(CancellationToken cancellationToken = default) where TData : LocalData
        {
            bool found = TryLoad(out TData data);
            return UniTask.FromResult((found, data));
        }

        /// <inheritdoc />
        public UniTask<(bool Found, TData Data)> TryLoadAsync<TData>(string slot, CancellationToken cancellationToken = default) where TData : LocalData
        {
            bool found = TryLoad(slot, out TData data);
            return UniTask.FromResult((found, data));
        }

        /// <inheritdoc />
        public UniTask DeleteAsync<TData>(string slot = "", CancellationToken cancellationToken = default) where TData : LocalData
        {
            Delete<TData>(slot);
            return UniTask.CompletedTask;
        }

        /// <inheritdoc />
        public UniTask DeleteAllAsync(CancellationToken cancellationToken = default)
        {
            DeleteAll();
            return UniTask.CompletedTask;
        }
    }
}
