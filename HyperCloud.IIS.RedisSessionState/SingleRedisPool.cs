using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack.Redis;

namespace HyperCloud.IIS.RedisSessionState
{
	internal class SingleRedisPool
	{
        public static ConnectionString ConnectionString { get; set; }

		private static volatile PooledRedisClientManager _pool;
        private static object _mutex = new object();

        private static PooledRedisClientManager GetPool()
        {
            if (_pool == null)
            {
                lock (_mutex)
                {
                    if (_pool == null)
                    {
                        RedisClientManagerConfig redisConfig = new RedisClientManagerConfig();
                        redisConfig.MaxReadPoolSize = 100;
                        redisConfig.MaxWritePoolSize = 100;
                        string[] redisWriteServers = new string[] { ConnectionString.Host };
                        string[] redisReadServers = new string[] { ConnectionString.Host };
                        _pool = new PooledRedisClientManager(redisWriteServers, redisReadServers, redisConfig, ConnectionString.DB);
                    }
                }
            }
            return _pool;
        }

		public static IRedisClient GetClient()
		{
            return GetPool().GetReadOnlyClient();
		}

        public static IRedisClient GetWriteClient()
        {
            return GetPool().GetClient();
        }
	}
}
