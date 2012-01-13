using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.SessionState;
using BookSleeve;
using System.Threading;
using System.Collections.Specialized;
using System.IO;
using System.Threading.Tasks;

namespace HyperCloud.IIS.RedisSessionState
{
	public sealed class RedisSessionItemHash : NameObjectCollectionBase,  ISessionStateItemCollection
	{
		private RedisConnection _redis;
		private string _sessionId;
		private int _timeout;

		private IValueSerializer _serializer = new ClrBinarySerializer();

		private IDictionary<string, object> _persistentValues = new Dictionary<string, object>();
		private object _mutex = new object();

		private const string TYPE_PREFIX = "__CLR_TYPE__";
		private const string VALUE_PREFIX = "val:";

        private Dictionary<string, byte[]> _rawItems;
        private bool _keysAdded;
        private HashSet<string> _namesAdded = new HashSet<string>();
        private bool _timeoutReset;

		public RedisSessionItemHash(string sessionId, int timeout, RedisConnection redis)
			: base()
		{
			this._sessionId = sessionId;
			this._timeout = timeout;
			this._redis = redis;

			SetTasks = new List<Task>();
		}

		private string GetKeyForSession()
		{
			return "sess:" + _sessionId;
		}

		private Dictionary<string, byte[]> GetRawItems()
		{
			if (_rawItems == null)
            {
				_rawItems = _redis.Hashes.GetAll(0, GetKeyForSession()).Result;
				OneTimeResetTimeout();
			}
			return _rawItems;
		}

		private void AddKeysToBase()
		{
			if (!_keysAdded)
            {
				string dePrefixedName;
				foreach (var name in GetRawItems().Keys)
                {
					if (!name.StartsWith(TYPE_PREFIX))
                    {
						dePrefixedName = name.Substring(VALUE_PREFIX.Length);
						BaseAdd(dePrefixedName, null);
					}
				}
				_keysAdded = true;
			}
		}

		private void AddFieldToBaseFromRaw(string name)
		{
			AddKeysToBase();
			lock (_mutex)
            {
				if (!GetRawItems().ContainsKey(VALUE_PREFIX + name))
                {
                    return;
                }

				var bytes = GetRawItems()[VALUE_PREFIX + name];

				var valueToAdd = _serializer.Deserialize(bytes);
				var persistentCopy = _serializer.Deserialize(bytes);

				BaseSet(name, valueToAdd);
				_persistentValues.Add(name, persistentCopy);
			}
		}

		private void AddAllFieldsToBaseFromRaw() 
		{
			AddKeysToBase();
			lock (_mutex)
            {
				foreach (var name in BaseGetAllKeys())
                {
					Get(name);
				}
			}
		}
		
		private object Get(string name)
		{
			if (!_namesAdded.Contains(name))
            {
				AddFieldToBaseFromRaw(name);
				_namesAdded.Add(name);
			}
			return BaseGet(name);
		}

		private void Set(string name, object value)
		{
			if (value != null && _namesAdded.Contains(name))
            {
				if (value.Equals(_persistentValues.ContainsKey(name) ? _persistentValues[name] : null))
                {
					return;
				}
			}
			var bytes = _serializer.Serialize(value);

			byte[] storedBytes;
			if (GetRawItems().TryGetValue(VALUE_PREFIX + name, out storedBytes) && bytes.SequenceEqual(storedBytes))
            {
			    return;
			}

			var itemsToSet = new Dictionary<string, byte[]>(1);
			itemsToSet.Add(VALUE_PREFIX+name, bytes);
			var setTask = _redis.Hashes.Set(0, GetKeyForSession(), itemsToSet);
			SetTasks.Add(setTask);

			OneTimeResetTimeout();

			if (_rawItems.ContainsKey(VALUE_PREFIX + name))
            {
				_rawItems[VALUE_PREFIX + name] = bytes;
			}
			else
            {
				_rawItems.Add(VALUE_PREFIX + name, bytes);
			}

			if (_persistentValues.ContainsKey(name))
            {
				_persistentValues.Remove(name);
			}
			var persistentCopy = _serializer.Deserialize(bytes);
			_persistentValues.Add(name, persistentCopy);

			if (!_namesAdded.Contains(name))
            {
				_namesAdded.Add(name);
				if (_keysAdded && BaseGetAllKeys().Contains(name))
                {
					BaseSet(name, value);
				}
				else
                {
					BaseAdd(name, value);
				}
			}
			else
            {
				BaseSet(name, value);
			}
		}

		internal void PersistChangedReferences()
		{
			var itemsToTryPersist = new Dictionary<string, object>();
			foreach (var name in BaseGetAllKeys())
            {
				if (_namesAdded.Contains(name))
                {
					var item = BaseGet(name);
					if (item is ValueType)
                    {
                        continue;
                    }
					itemsToTryPersist.Add(name, item);
				}
			}
			foreach (var pair in itemsToTryPersist)
            {
				Set(pair.Key, pair.Value);
			}
		}

		private void OneTimeResetTimeout()
		{
			if (!_timeoutReset)
            {
				_redis.Keys.Expire(0, GetKeyForSession(), _timeout * 60);
				_timeoutReset = true;
			}
		}

		public object this[string name]
		{
			get {
				return Get(name);
			}
			set {
				Set(name, value);
			}
		}

		public void Clear()
		{
			_redis.Keys.Remove(0, GetKeyForSession());
			BaseClear();
		}

		public void Remove(string name)
		{
			_redis.Hashes.Remove(0, GetKeyForSession(), name);
			BaseRemove(name);
		}

		public override int Count
		{
			get {
				AddKeysToBase();
				return base.Count;
			}
		}

		public override NameObjectCollectionBase.KeysCollection Keys
		{
			get {
				AddKeysToBase();
				return base.Keys;
			}
		}

		public override IEnumerator GetEnumerator()
		{
			AddAllFieldsToBaseFromRaw();
			return base.GetEnumerator();
		}

		public bool Dirty
		{
			get { return false; }
			set { }
		}

		public IList<Task> SetTasks { get; private set; }

		public void RemoveAt(int index)
		{
			throw new NotImplementedException();
		}

		public object this[int index]
		{
			get
			{
				throw new NotImplementedException();
			}
			set
			{
				throw new NotImplementedException();
			}
		}
	}
}
