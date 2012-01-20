using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Data;
using System.Web.SessionState;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using ServiceStack.Redis;
using System.Text;

using Newtonsoft.Json;

namespace HyperCloud.IIS.RedisSessionState
{
    // : NameObjectCollectionBase
    public sealed class RedisSessionItems : NameObjectCollectionBase, ISessionStateItemCollection
	{
        private JsonSerializerSettings _jss = new JsonSerializerSettings {
            TypeNameHandling = TypeNameHandling.All,
            TypeNameAssemblyFormat = FormatterAssemblyStyle.Simple
        };

        private string _key;
        private bool _allKeysLoaded = false;
        private HashSet<string> _nameCache = new HashSet<string>();
        private IValueSerializer _serializer = new ClrBinarySerializer();

        //public IList<Task> _tasks { get; private set; }

		private string _sessionId;
		private int _timeout;

        public RedisSessionItems(string sessionId, int timeout)
			: base()
		{
			_sessionId = sessionId;
            _key = "session:" + _sessionId;
			_timeout = timeout;

            //_tasks = new List<Task>();
		}

        #region ISessionStateItemCollection Members

        public void Clear()
        {
            using (var redis = SingleRedisPool.GetClient())
            {
                redis.Remove(_key);
            }
            BaseClear();
        }

        public bool Dirty
        {
            get { return false; }
            set { }
        }

        public System.Collections.Specialized.NameObjectCollectionBase.KeysCollection Keys
        {
            get
            {
                AddKeysToBase();
                return base.Keys;
            }
        }

        public void Remove(string name)
        {
            using (var redis = SingleRedisPool.GetClient())
            {
                redis.RemoveEntryFromHash(_key, name);
            }
            BaseRemove(name);
        }

        public void RemoveAt(int index)
        {
            throw new NotImplementedException("Please use Remove(string name) instead");
        }

        public object this[int index]
        {
            get
            {
                throw new NotImplementedException("Please use Session[string] instead");
            }
            set
            {
                throw new NotImplementedException("Please use Session[string] instead");
            }
        }

        public object this[string name]
        {
            get
            {
                return Get(name);
            }
            set
            {
                Set(name, value);
            }
        }

        #endregion

        #region ICollection Members

        public void CopyTo(Array array, int index)
        {
            throw new NotImplementedException();
        }

        public int Count
        {
            get {
                AddKeysToBase();
                return base.Count;
            }
        }

        public bool IsSynchronized
        {
            get { throw new NotImplementedException(); }
        }

        public object SyncRoot
        {
            get { throw new NotImplementedException(); }
        }

        #endregion

        #region IEnumerable Members

        public System.Collections.IEnumerator GetEnumerator()
        {
            throw new NotImplementedException();
            /*AddKeysToBase();
            using (var redis = SingleRedisPool.GetReadOnlyClient())
            {
                IDictionary<string, string> raw = redis.GetAllEntriesFromHash(_key);
                foreach (var item in raw)
                {
                    if (!_loaded.Contains(item.Key)) {
                        byte[] data = Encoding.UTF8.GetBytes(item.Value);
                        var value = _serializer.Deserialize(data);
                        BaseSet(item.Key, value);
                        _loaded.Add(item.Key);
                    }
                }
            }
            return base.GetEnumerator();*/
        }

        #endregion

        private void AddKeysToBase()
        {
            if (!_allKeysLoaded)
            {
                using (var redis = SingleRedisPool.GetReadOnlyClient())
                {
                    foreach (string name in redis.GetHashKeys(_key))
                    {
                        BaseAdd(name, null);
                    }
                }
            }
        }

		private object Get(string name)
		{
            if (!_nameCache.Contains(name)) {
                AddKeysToBase();
                using (var redis = SingleRedisPool.GetReadOnlyClient())
                {
                    // TODO: Maybe not need check if exists? (see BaseSet - ???)
                    //if (redis.HashContainsEntry(_key, name))
                    //{
                        //string raw = redis.GetValueFromHash(_key, name);
                        //byte[] data = Encoding.UTF8.GetBytes(raw);

                        //var value = _serializer.Deserialize(data);

                        //var value = JsonConvert.DeserializeObject(raw);

                        //byte[] data = Encoding.UTF8.GetBytes(raw);
                        var client = redis.GetTypedClient<byte[]>();
                        var hash = client.GetHash<string>(_key);
                        var data = hash[name];
                        object value = null;
                        if (data != null && data.Length > 0)
                        {
                            //var items = new SessionStateItemCollection();
                            using (var stream = new MemoryStream(data))
                            {
                                //if (stream.Length > 0)
                                //{
                                //    using (var reader = new BinaryReader(stream))
                                //    {
                                //        items = SessionStateItemCollection.Deserialize(reader);
                                //    }
                                //}
                                var formatter = new BinaryFormatter();
                                value = formatter.Deserialize(stream);
                                //value = items[name];
                            }
                        }
                        BaseSet(name, value);
                    //}
                    _nameCache.Add(name);
                }
            }
            object val = BaseGet(name);
            return val;
		}

        public void SaveAll()
        {
            foreach (var name in _nameCache)
            {
                var value = BaseGet(name);
                if (value != null)
                {
                    byte[] data = null;
                    using (var stream = new MemoryStream())
                    {
                        var formatter = new BinaryFormatter();

                        formatter.Serialize(stream, value);
                        data = stream.ToArray();
                    }
                    var raw = JsonConvert.SerializeObject(value, Formatting.None, _jss);

                    using (var redis = SingleRedisPool.GetClient())
                    {
                        var client = redis.GetTypedClient<byte[]>();
                        var hash = client.GetHash<string>(_key);
                        hash[name] = data;

                        var client1 = redis.GetTypedClient<string>();
                        var hash1 = client1.GetHash<string>(_key);
                        hash1[name + "_debug"] = raw;
                    }
                }
            }
        }

		private void Set(string name, object value)
		{
            AddKeysToBase();

            //var items = new SessionStateItemCollection();
            //items[name] = value;

            byte[] data = null;
            using (var stream = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(stream, value);
                //using (var writer = new BinaryWriter(stream))
                //{
                //    items.Serialize(writer);
                //    writer.Close();
                //}
                data = stream.ToArray();
            }
            var raw = JsonConvert.SerializeObject(value, Formatting.None, _jss);

            using (var redis = SingleRedisPool.GetClient())
            {
                var client = redis.GetTypedClient<byte[]>();
                var hash = client.GetHash<string>(_key);
                hash[name] = data;

                var client1 = redis.GetTypedClient<string>();
                var hash1 = client1.GetHash<string>(_key);
                hash1[name + "_debug"] = raw;
            }
            if (!_nameCache.Contains(name))
            {
                if (BaseGetAllKeys().Contains(name)) {
                    BaseSet(name, value);
                } else {
                    BaseAdd(name, value);
                }
                _nameCache.Add(name);
            }
            else
            {
                if (BaseGetAllKeys().Contains(name))
                {
                    BaseSet(name, value);
                }
                else
                {
                    BaseAdd(name, value);
                }
            }
		}
	}
}
