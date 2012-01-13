using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HyperCloud.IIS.RedisSessionState
{
    public interface IValueSerializer
    {
        byte[] Serialize(object value);
        object Deserialize(byte[] bytes);
    }
}
