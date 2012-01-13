using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.SessionState;
using System.Web;

namespace HyperCloud.IIS.RedisSessionState
{
    public class RedisHttpSessionStateContainer : HttpSessionStateContainer
	{
		public RedisSessionItemHash SessionItems {
            get; 
            private set;
        }

		public RedisHttpSessionStateContainer(string id, RedisSessionItemHash sessionItems,
            HttpStaticObjectsCollection staticObjects, int timeout, bool newSession, HttpCookieMode cookieMode,
            SessionStateMode mode, bool isReadonly)
			: base(id, sessionItems, staticObjects, timeout, newSession, cookieMode, mode, isReadonly)
		{
			SessionItems = sessionItems;
		}
	}
}
