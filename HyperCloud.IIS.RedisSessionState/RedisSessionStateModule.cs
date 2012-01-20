﻿using System;
using System.Linq;
using System.Web;
using System.Web.SessionState;
using System.Collections;
using System.Threading;
using System.Web.Configuration;
using System.Configuration;
using System.Threading.Tasks;

using ServiceStack.Redis;

namespace HyperCloud.IIS
{
    using HyperCloud.IIS.RedisSessionState;

	public sealed class RedisSessionStateModule : IHttpModule, IDisposable
	{
		private bool _initialized = false;
        private static object _mutex = new object();

        private ISessionIDManager _sessionIDManager;
        private SessionStateSection _config;
        private HttpCookieMode _cookieMode = HttpCookieMode.UseCookies;
        private int _timeout;

		private bool _releaseCalled = false;

        public event EventHandler Start;

        public RedisSessionStateModule()
        {
        }

        // IHttpModule.Init
		public void Init(HttpApplication app)
		{
			if (!_initialized)
            {
				lock (_mutex)
                {
					if (!_initialized)
                    {
						// Add event handlers.
						app.AcquireRequestState += new EventHandler(this.OnAcquireRequestState);
						app.ReleaseRequestState += new EventHandler(this.OnReleaseRequestState);
						app.EndRequest += new EventHandler(this.OnEndRequest);

						// Create a SessionIDManager.
						_sessionIDManager = new SessionIDManager();
						_sessionIDManager.Initialize();
						
						// Get the configuration section and set timeout and CookieMode values.
						Configuration webConfig = 
						    WebConfigurationManager.OpenWebConfiguration(System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath);
						_config = (SessionStateSection)webConfig.GetSection("system.web/sessionState");

                        SingleRedisPool.ConnectionString = new ConnectionString(_config.StateConnectionString);

						_timeout = (int)_config.Timeout.TotalMinutes;
						_cookieMode = _config.Cookieless;

						_initialized = true;
					}
				}
			}
		}

		public void Dispose()
		{
		}

		private bool RequiresSessionState(HttpContextBase context)
		{
			if (context.Session != null && (context.Session.Mode == null || context.Session.Mode == SessionStateMode.Off))
            {
				return false;
			}
			return (context.Handler is IRequiresSessionState || context.Handler is IReadOnlySessionState);
		}

		private void OnAcquireRequestState(object source, EventArgs args)
		{
			HttpApplication app = (HttpApplication)source;
			HttpContext context = app.Context;
			bool isNew = false;
			string sessionId;

            RedisSessionItems sessionItems = null;
			bool supportSessionIDReissue = true;

			_sessionIDManager.InitializeRequest(context, false, out supportSessionIDReissue);
			sessionId = _sessionIDManager.GetSessionID(context);

			if (sessionId == null)
            {
				bool redirected, cookieAdded;

				sessionId = _sessionIDManager.CreateSessionID(context);
				_sessionIDManager.SaveSessionID(context, sessionId, out redirected, out cookieAdded);

				isNew = true;
                if (redirected)
                {
                    return;
                }
			}

			if (!RequiresSessionState(new HttpContextWrapper(context)))
            {
                return;
            }

			_releaseCalled = false;

            sessionItems = new RedisSessionItems(sessionId, _timeout);

            if (sessionItems.Count == 0)
            {
				isNew = true;
			}
			
			// Add the session data to the current HttpContext.
			SessionStateUtility.AddHttpSessionStateToContext(context, new RedisHttpSessionStateContainer(sessionId, sessionItems, 
                SessionStateUtility.GetSessionStaticObjects(context), _timeout,
	            isNew, _cookieMode, SessionStateMode.Custom, false));

			// Execute the Session_OnStart event for a new session.
			if (isNew && Start != null)
            {
				Start(this, EventArgs.Empty);
			}
		}

		private void OnReleaseRequestState(object source, EventArgs args)
		{
			HttpApplication app = (HttpApplication)source;
			HttpContext context = app.Context;

			if (context == null || context.Session == null)
            {
                return;
            }

			_releaseCalled = true;

			// Read the session state from the context
			var stateContainer = (RedisHttpSessionStateContainer)(SessionStateUtility.GetHttpSessionStateFromContext(context));

			// If Session.Abandon() was called, remove the session data from the local Hashtable
			// and execute the Session_OnEnd event from the Global.asax file.
			if (stateContainer.IsAbandoned)
            {
				stateContainer.Clear();
				SessionStateUtility.RaiseSessionEnd(stateContainer, this, EventArgs.Empty);
			}
			else
            {
				//stateContainer.SessionItems.PersistChangedReferences();
			}
            stateContainer.SessionItems.SaveAll();

			//Task.WaitAll(stateContainer.SessionItems.SetTasks.ToArray(), 1500);
			SessionStateUtility.RemoveHttpSessionStateFromContext(context);
		}

		private void OnEndRequest(object source, EventArgs eventArgs)
		{
			if (!_releaseCalled)
            {
				OnReleaseRequestState(source, eventArgs);
			}
		}
	}
}