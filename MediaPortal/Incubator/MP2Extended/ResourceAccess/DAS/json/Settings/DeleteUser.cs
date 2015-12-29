using System;
using HttpServer;
using HttpServer.Exceptions;
using HttpServer.Sessions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Authentication;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.Settings;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.DAS.json.Settings
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, ReturnType = typeof(WebBoolResult), Summary = "Deletes a MP2Ext user by passing the user id.")]
  [ApiFunctionParam(Name = "id", Type = typeof(string), Nullable = false)]
  internal class DeleteUser : IRequestMicroModuleHandler
  {
    public dynamic Process(IHttpRequest request, IHttpSession session)
    {
      // Security
      if (!CheckRights.AccessAllowed(session, UserTypes.Admin, false, true))
        return new WebBoolResult { Result = false };

      HttpParam httpParam = request.Param;
      string id = httpParam["id"].Value;

      if (id == null)
      {
        Logger.Warn("DeleteUser: id is null");
        return new WebBoolResult { Result = false };
      }
      Guid userGuid;
      if (!Guid.TryParse(id, out userGuid))
      {
        Logger.Warn("DeleteUser: couldn't parse id to Guid: {0}", id);
        return new WebBoolResult { Result = false };
      }

      if (!MP2Extended.Users.Users.Exists(x => x.Id == userGuid))
      {
        Logger.Info("DeleteUser: A user with the Id '{0}' wasn't found!", userGuid);
        return new WebBoolResult { Result = false };
      }

      MP2Extended.Users.Users.RemoveAll(x => x.Id == userGuid);

      return new WebBoolResult { Result = true };
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}