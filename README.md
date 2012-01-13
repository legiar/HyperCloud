HyperCloud
----------

### Redis Session State HttpModule (`HyperCloud.IIS.RedisSessionStateModule`)

`RedisSessionStateModule` is a IHttpModule that can replace ASP.NET's default Session module. It has the following 
features/differences:

* Session data is stored in Redis
* This module does NOT do the per request locking that the default module does (see: http://msdn.microsoft.com/en-us/library/ms178587.aspx ),
  which means that multiple request under the same SessionId can be processed concurrently.
* Session items are stored and accessed independently from items in a Redis Hash. So when session is saved at the end 
  of a request, only the session items that were modified during that request need to be persisted to Redis.

To use with Integrated Pipeline mode:
Create a `remove` then an `add` in the `modules` element inside the `system.webServer` element in your web.config like so:

```xml
<system.webServer>
  <modules>
    <remove name="Session" />
    <add name="Session" type="HyperCloud.IIS.RedisSessionStateModule" />
  </modules>
<system.webServer>
```
Also, set connection string inside `sessionState` element in your web.config like so:

```xml
<system.web>
  <sessionState stateConnectionString="tcp=localhost:6379" />
</system.web>
```

### TODO:

*  add option to use different serializers (JSON, XML etc.)
*  create test web-site
*  create a quick benchmark program

