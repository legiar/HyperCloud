using System;
using System.Collections.Generic;

namespace HyperCloud.IIS.RedisSessionState
{
    public class ConnectionString
    {
        private readonly IDictionary<string, string> parameters = new Dictionary<string, string>();

        public ConnectionString(string connectionString)
        {
            if (connectionString == null)
            {
                throw new ArgumentNullException("connectionString");
            }

            var pairs = connectionString.Split(';');

            foreach (var key_value in pairs)
            {
                if (key_value == null || string.IsNullOrEmpty(key_value.Trim()))
                {
                    continue;
                }
                var parts = key_value.Trim().Split('=');
                if (parts.Length != 2)
                {
                    // TODO: Own exception class
                    throw new Exception(String.Format("Invalid connection string element: '{0}' should be 'key=value'", key_value.Trim()));
                }

                parameters.Add(parts[0].Trim().ToLower(), parts[1].Trim());
            }
        }

        public string Host
        {
            get { return GetValue("host", "localhost"); }
        }

        public int Port
        {
            get
            {
                int port;
                if (!int.TryParse(GetValue("port", "6379"), out port))
                {
                    port = 6379;
                }
                return port;
            }
        }

        public int DB
        {
            get {
                int db;
                if (!int.TryParse(GetValue("db", "0"), out db))
                {
                    db = 0;
                }
                return db;
            }
        }

        public string GetValue(string key)
        {
            if (!parameters.ContainsKey(key))
            {
                // TODO: Own exception class
                throw new Exception(String.Format("No value with key '{0}' exists", key));
            }
            return parameters[key];
        }

        public string GetValue(string key, string defaultValue)
        {
            return parameters.ContainsKey(key) ? parameters[key] : defaultValue;
        }
    }
}