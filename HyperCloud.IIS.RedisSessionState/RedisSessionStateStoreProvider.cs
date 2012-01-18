using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Web.SessionState;
using System.Web.Configuration;
using System.Reflection;
using System.IO;
using ServiceStack.Redis;

namespace HyperCloud.IIS
{
    using HyperCloud.IIS.RedisSessionState;
    using System.Runtime.Serialization.Formatters;

    [Serializable]
    internal class SessionItem
    {
        public int ActionFlag { get; set; }
        public int LockId { get; set; }
        public DateTime SetTime { get; set; }

        public TimeSpan LockAge
        {
            get { return DateTime.Now.Subtract(this.SetTime); }
        }

        public bool Locked { get; set; }
    }

    public sealed class RedisSessionStateStoreProvider : SessionStateStoreProviderBase
    {
        private static object _mutex = new object();
        private bool _initialized = false;
        private ISessionIDManager _sessionIDManager;
        private SessionStateSection _config;

        //private int _timeout = 30;
        private TimeSpan _timeout = new TimeSpan(0, 30, 0);

        public override void Initialize(string name, System.Collections.Specialized.NameValueCollection config)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }

            if (!_initialized)
            {
                lock (_mutex)
                {
                    if (!_initialized)
                    {
                        _config = (SessionStateSection)WebConfigurationManager.GetSection("system.web/sessionState");
                        SingleRedisPool.ConnectionString = new ConnectionString(_config.StateConnectionString);

                        _sessionIDManager = new SessionIDManager();
                        _sessionIDManager.Initialize();

                        base.Initialize(name, config);

                        _initialized = true;
                    }
                }
            }
        }

        public override void Dispose()
        {
            //_redis.Dispose();
        }

        public override SessionStateStoreData CreateNewStoreData(System.Web.HttpContext context, int timeout)
        {
            return new SessionStateStoreData(new SessionStateItemCollection(), SessionStateUtility.GetSessionStaticObjects(context), timeout);
            /*bool supportSessionIDReissue = true;

            _sessionIDManager.InitializeRequest(context, false, out supportSessionIDReissue);
            string sessionID = _sessionIDManager.GetSessionID(context);
            return new SessionStateStoreData(
                new RedisSessionItems(sessionID, timeout, _redis),
                SessionStateUtility.GetSessionStaticObjects(context), timeout);
            */
        }


        public override void CreateUninitializedItem(System.Web.HttpContext context, string id, int timeout)
        {
            using (var redis = SingleRedisPool.GetClient())
            {
                var data = new SessionItem
                {
                    //Content = null,
                    Locked = false,
                    SetTime = DateTime.Now,
                    LockId = 0,
                    ActionFlag = 1
                };

                redis.Set(GetKey(id), data, new TimeSpan(0, timeout, 0));
                redis.IncrementValue(GetKey(id) + "_counter");
                // TODO: initialize empty {key}_items
            }
        }

        public override void InitializeRequest(System.Web.HttpContext context)
        {
        }

        public override void EndRequest(System.Web.HttpContext context)
        {
        }

        public override SessionStateStoreData GetItem(System.Web.HttpContext context, string id, out bool locked, out TimeSpan lockAge, out object lockId, out SessionStateActions actionFlags)
        {
            SessionStateStoreData item = null;
            lockAge = TimeSpan.Zero;
            lockId = null;
            locked = false;
            actionFlags = SessionStateActions.None;

            /*
            return new SessionStateStoreData(
                new RedisSessionItems(id, _timeout, _redis),
                SessionStateUtility.GetSessionStaticObjects(context),
                _timeout);
            */

            using (var redis = SingleRedisPool.GetClient())
            {
                var key = this.GetKey(id);

                var data = redis.Get<SessionItem>(key);

                if (data != null)
                {
                    if (!data.Locked)
                    {
                        data.LockId++;
                        data.SetTime = DateTime.Now;

                        using (var writeRedis = SingleRedisPool.GetWriteClient())
                        {
                            writeRedis.Set(key, data, _timeout);
                        }

                        actionFlags = (SessionStateActions)data.ActionFlag;
                        lockId = data.LockId;
                        lockAge = data.LockAge;

                        if (actionFlags == SessionStateActions.InitializeItem)
                        {
                            item = this.CreateNewStoreData(context, (int)_timeout.TotalMinutes);
                        }
                        else
                        {
                            byte[] content = redis.Get<byte[]>(key + "_items");
                            item = this.Deserialize(context, content, (int)_timeout.TotalMinutes);
                        }

                        return item;
                    }
                    else
                    {
                        lockAge = data.LockAge;
                        locked = true;
                        lockId = data.LockId;
                        actionFlags = (SessionStateActions)data.ActionFlag;

                        byte[] content = redis.Get<byte[]>(key + "_items");
                        return this.Deserialize(context, content, (int)_timeout.TotalMinutes);
                    }
                }
                else
                {
                    return item;
                }
            }
        }

        public override SessionStateStoreData GetItemExclusive(System.Web.HttpContext context, string id, out bool locked, out TimeSpan lockAge, out object lockId, out SessionStateActions actionFlags)
        {
            //return this.GetItem(context, id, out locked, out lockAge, out lockId, out actions);

            SessionStateStoreData item = null;
            lockAge = TimeSpan.Zero;
            lockId = null;
            locked = false;
            actionFlags = 0;

            using (var redis = SingleRedisPool.GetClient())
            {
                var key = this.GetKey(id);

                var data = redis.Get<SessionItem>(key);

                if (data != null)
                {
                    if (!data.Locked)
                    {
                        data.LockId++;
                        data.SetTime = DateTime.Now;
                        data.Locked = true;

                        using (var writeRedis = SingleRedisPool.GetWriteClient())
                        {
                            writeRedis.Set(key, data, _timeout);
                            writeRedis.IncrementValue(GetKey(id) + "_counter");
                        }

                        actionFlags = (SessionStateActions)data.ActionFlag;
                        lockId = data.LockId;
                        lockAge = data.LockAge;

                        if (actionFlags == SessionStateActions.InitializeItem)
                        {
                            item = this.CreateNewStoreData(context, (int)_timeout.TotalMinutes);
                        }
                        else
                        {
                            byte[] content = redis.Get<byte[]>(key + "_items");
                            item = this.Deserialize(context, content, (int)_timeout.TotalMinutes);
                        }

                        return item;
                    }
                    else
                    {
                        lockAge = data.LockAge;
                        locked = true;
                        lockId = data.LockId;
                        actionFlags = (SessionStateActions)data.ActionFlag;

                        byte[] content = redis.Get<byte[]>(key + "_items");
                        return this.Deserialize(context, content, (int)_timeout.TotalMinutes);
                    }
                }
                else
                {
                    return item;
                }
            }
        }

        public override void ReleaseItemExclusive(System.Web.HttpContext context, string id, object lockId)
        {
            using (var redis = SingleRedisPool.GetClient())
            {
                var key = this.GetKey(id);

                var item = redis.Get<SessionItem>(key);

                if (item != null)
                {
                    item.Locked = false;
                    item.LockId = (int)lockId;

                    using (var writeRedis = SingleRedisPool.GetWriteClient())
                    {
                        writeRedis.Set(key, item);
                        writeRedis.IncrementValue(GetKey(id) + "_counter");
                    }
                }
            }
        }

        public override void RemoveItem(System.Web.HttpContext context, string id, object lockId, SessionStateStoreData item)
        {
            using (var redis = SingleRedisPool.GetWriteClient())
            {
                redis.Remove(this.GetKey(id));
            }
        }

        public override void ResetItemTimeout(System.Web.HttpContext context, string id)
        {
            using (var redis = SingleRedisPool.GetWriteClient())
            {
                var item = redis.GetValue(this.GetKey(id));
                if (!String.IsNullOrEmpty(item))
                {
                    redis.ExpireEntryIn(this.GetKey(id), new TimeSpan(0, 1, 0));
                }
            }
        }

        public override void SetAndReleaseItemExclusive(System.Web.HttpContext context, string id, SessionStateStoreData item, object lockId, bool newItem)
        {
            using (var redis = SingleRedisPool.GetWriteClient())
            {
                var data = new SessionItem
                {
                    Locked = false,
                    SetTime = DateTime.Now,
                    LockId = 0,
                    ActionFlag = 0
                };
                string key = GetKey(id);
                redis.Set(key, data, new TimeSpan(0, item.Timeout, 0));
                redis.Set(key + "_items", Serialize((SessionStateItemCollection)item.Items));
                redis.IncrementValue(GetKey(id) + "_counter");
            }
        }

        public override bool SetItemExpireCallback(SessionStateItemExpireCallback expireCallback)
        {
            return false;
        }

        private string GetKey(string sessionId, string key = "")
        {
            string value = "session:" + sessionId;
            if (key != "")
            {
                value = value + ":" + key;
            }
            return value;
        }

        private byte[] Serialize(SessionStateItemCollection items)
        {
            using (var stream = new MemoryStream())
            {
                using (var writer = new BinaryWriter(stream))
                {
                    if (items != null)
                    {
                        items.Serialize(writer);
                    }
                    writer.Close();
                }
                return stream.ToArray();
            }
        }

        private SessionStateStoreData Deserialize(System.Web.HttpContext context, byte[] serializedItems, int timeout)
        {
            var sessionItems = new SessionStateItemCollection();
            if (serializedItems != null && serializedItems.Length > 0)
            {
                using (var stream = new MemoryStream(serializedItems))
                {
                    if (stream.Length > 0)
                    {
                        using (var reader = new BinaryReader(stream))
                        {
                            sessionItems = SessionStateItemCollection.Deserialize(reader);
                        }
                    }
                }
            }
            return new SessionStateStoreData(sessionItems, SessionStateUtility.GetSessionStaticObjects(context), timeout);
        }
    }
}
