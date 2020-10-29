using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.ComponentModel;

namespace FileExplorer
{
    public class ObservableConcurrentDictionary<TKey, TValue> :
        ConcurrentDictionary<TKey, TValue>,
        IDictionary<TKey, TValue>,
        IEnumerable<KeyValuePair<TKey, TValue>>,
        INotifyCollectionChanged,
        INotifyPropertyChanged
    {
        public ObservableConcurrentDictionary() 
            : base() { }

        public ObservableConcurrentDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection) 
            : base(collection) { }


        public ObservableConcurrentDictionary(IEqualityComparer<TKey> comparer) 
            : base(comparer) { }

        public ObservableConcurrentDictionary(int concurrencyLevel, int capacity) 
            : base(concurrencyLevel, capacity) { }

        public ObservableConcurrentDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey> comparer)
            : base(collection, comparer) { }

        public ObservableConcurrentDictionary(int concurrencyLevel, int capacity, IEqualityComparer<TKey> comparer) 
            : base(concurrencyLevel, capacity, comparer) { }

        public ObservableConcurrentDictionary(int concurrencyLevel, IEnumerable<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey> comparer)
            : base(concurrencyLevel, collection, comparer) { }
        public ObservableConcurrentDictionary(Dictionary<TKey, TValue> collection) 
            : base(collection) { }

        public new TValue AddOrUpdate(TKey key, Func<TKey, TValue> addValueFactory, Func<TKey, TValue, TValue> updateValueFactory)
        {
            // Stores the value
            TValue value;
            // If key exists
            if (base.ContainsKey(key))
            {
                // Update value and raise event
                value = base.AddOrUpdate(key, addValueFactory, updateValueFactory);
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, value));
            }
            // Else if key does not exist
            else
            {
                // Add value and raise event
                value = base.AddOrUpdate(key, addValueFactory, updateValueFactory);
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, value));
            }
            // Returns the value
            return value;
        }

        public new void Clear()
        {
            // Clear dictionary
            base.Clear();
            // Raise event
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public new TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
        {
            // Stores the value
            TValue value;
            // If key exists
            if (base.ContainsKey(key))
                // Get value
                value = base.GetOrAdd(key, valueFactory);
            // Else if key does not exist
            else
            {
                // Add value and raise event
                value = base.GetOrAdd(key, valueFactory);
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, value));
            }
            // Return value
            return value;
        }

        public new TValue GetOrAdd(TKey key, TValue value)
        {
            // If key exists
            if (base.ContainsKey(key))
                // Get value
                base.GetOrAdd(key, value);
            // Else if key does not exist
            else
            {
                // Add value and raise event
                base.GetOrAdd(key, value);
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, value));
            }
            // Return value
            return value;
        }

        public new bool TryAdd(TKey key, TValue value)
        {
            // Stores tryAdd
            bool tryAdd;
            // If added
            if (tryAdd = base.TryAdd(key, value))
                // Raise event
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, value));
            // Return tryAdd
            return tryAdd;
        }

        public new bool TryRemove(TKey key, out TValue value)
        {
            // Stores tryRemove
            bool tryRemove;
            // If removed
            if (tryRemove = base.TryRemove(key, out value))
                // Raise event
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, value));
            // Return tryAdd
            return tryRemove;
        }

        public new bool TryUpdate(TKey key, TValue newValue, TValue comparisonValue)
        {
            // Stores tryUpdate
            bool tryUpdate;
            // If updated
            if (tryUpdate = base.TryUpdate(key, newValue, comparisonValue))
                // Raise event
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, newValue));
            // Return tryUpdate
            return tryUpdate;
        }

        private void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (CollectionChanged != null)
                CollectionChanged(this, e);
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public event PropertyChangedEventHandler PropertyChanged;

        public static implicit operator ObservableConcurrentDictionary<TKey, TValue>(Dictionary<TKey, TValue> v)
        {
            return new ObservableConcurrentDictionary<TKey, TValue>(v);
        }

        public static implicit operator Dictionary<TKey, TValue>(ObservableConcurrentDictionary<TKey, TValue> v)
        {
            return new Dictionary<TKey, TValue>(v);
        }
    }
}
