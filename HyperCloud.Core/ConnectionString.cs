using System;
using System.Collections.Generic;

namespace HyperCloud
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
                    throw new HyperCloudException("Invalid connection string element: '{0}' should be 'key=value'", key_value.Trim());
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
                if (!int.TryParse(GetValue("port", "5672"), out port))
                {
                    port = 5672;
                }
                return port;
            }
        }

        public string VirtualHost
        {
            get { return GetValue("virtualHost", "/"); }
        }

        public string UserName
        {
            get { return GetValue("username", "guest"); }
        }

        public string Password
        {
            get { return GetValue("password", "guest"); }
        }

        public bool Ssl
        {
            get { return GetValue("ssl", "true").ToLower() == "true"; }
        }

        public string GetValue(string key)
        {
            if (!parameters.ContainsKey(key))
            {
                throw new HyperCloudException("No value with key '{0}' exists", key);
            }
            return parameters[key];
        }

        public string GetValue(string key, string defaultValue)
        {
            return parameters.ContainsKey(key) ? parameters[key] : defaultValue;
        }
    }
}