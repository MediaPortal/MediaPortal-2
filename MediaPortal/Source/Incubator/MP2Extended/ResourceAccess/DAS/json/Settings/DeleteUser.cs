using System;
using HttpServer;
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
  internal class DeleteUser
  {
    public WebBoolResult Process(Guid id)
    {
      // Security
      // TODO: Readd security
      /*if (!CheckRights.AccessAllowed(session, UserTypes.Admin, false, true))
        return new WebBoolResult { Result = false };*/

      if (!MP2Extended.Users.Users.Exists(x => x.Id == id))
      {
        Logger.Info("DeleteUser: A user with the Id '{0}' wasn't found!", id);
        return new WebBoolResult { Result = false };
      }

      MP2Extended.Users.Users.RemoveAll(x => x.Id == id);

      return new WebBoolResult { Result = true };
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}