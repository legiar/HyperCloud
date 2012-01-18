using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Web.SessionState;
using ServiceStack.Redis;
//using Newtonsoft.Json;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace HyperCloud.IIS.RedisSessionState
{
    // : NameObjectCollectionBase
	public sealed class RedisSessionItems : ISessionStateItemCollection
	{
		private PooledRedisClientManager _redis;
		private string _sessionId;
		private int _timeout;
        private bool _isDirty;

        private ClrBinarySerializer serializer;

        public RedisSessionItems(string sessionId, int timeout, PooledRedisClientManager redis)
			: base()
		{
			this._sessionId = sessionId;
			this._timeout = timeout;
			this._redis = redis;

            this._isDirty = false;

            serializer = new ClrBinarySerializer();
		}

        #region ISessionStateItemCollection Members

        public void Clear()
        {
        }

        public bool Dirty
        {
            get { return _isDirty; }
            set { _isDirty = value; }
        }

        public System.Collections.Specialized.NameObjectCollectionBase.KeysCollection Keys
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public void Remove(string name)
        {
            string key = GetKey(name);
            _redis.Remove(name);
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
            get { throw new NotImplementedException(); }
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
        }

        #endregion

        private string GetKey(string key)
        {
            return "session:" + _sessionId + ":" + key;
        }

		private object Get(string name)
		{
            string key = GetKey(name);
            object value = null;
            using (var client = _redis.GetReadOnlyClient())
            {
                //var json = client.GetValue(key);
                //if (json != null && json != "")
                //{
                //    value = JsonConvert.DeserializeObject(json, _jss);
                //}
                //byte[] bytes = client.Get<byte[]>(key);
                //if (bytes != null && bytes.Length != 0)
                //{
                    //value = serializer.Deserialize(bytes);
                    //var formatter = new BinaryFormatter();
                    //using (var stream = new MemoryStream(bytes))
                    //{
                    //    value = formatter.Deserialize(stream);
                    //}
                //}
            }
            return value;
		}

		private void Set(string name, object value)
		{
            string key = GetKey(name);
            using (var client = _redis.GetClient()) {
                //var json = JsonConvert.SerializeObject(value, Formatting.None, _jss);
                //client.SetEntry(key, json);
                //byte[] bytes = serializer.Serialize(value);

                var formatter = new BinaryFormatter();
                using (var stream = new MemoryStream())
                {
                    formatter.Serialize(stream, value);
                    byte[] bytes = stream.GetBuffer();
                    client.Set<byte[]>(key, bytes);
                }
            }
		}
	}
}
