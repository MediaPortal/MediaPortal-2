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
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, ReturnType = typeof(WebBoolResult), Summary = "Create a new user for MP2Ext. To do so you must be login with an admin Account and Authentication must be enabled.")]
  [ApiFunctionParam(Name = "username", Type = typeof(string), Nullable = false)]
  [ApiFunctionParam(Name = "type", Type = typeof(string), Nullable = false)]
  [ApiFunctionParam(Name = "password", Type = typeof(string), Nullable = false)]
  internal class CreateUser : IRequestMicroModuleHandler
  {
    public dynamic Process(IHttpRequest request, IHttpSession session)
    {
      // Security
      if (!CheckRights.AccessAllowed(session, UserTypes.Admin, false, true))
        return new WebBoolResult { Result = false };

      HttpParam httpParam = request.Param;
      string username = httpParam["username"].Value;
      string type = httpParam["type"].Value;
      string password = httpParam["password"].Value;

      if (username == null)
        throw new BadRequestException("CreateUser: username is null");
      if (type == null)
        throw new BadRequestException("CreateUser: type is null");
      if (password == null)
        throw new BadRequestException("CreateUser: password is null");
      UserTypes typeEnum;
      if (!Enum.TryParse(type, out typeEnum))
        throw new BadRequestException("CreateUser: couldn't parse type to enum");

      if (MP2Extended.Users.Users.Exists(x => x.Name == username))
      {
        Logger.Info("CreateUser: A user with the name '{0}' already exists!", username);
        return new WebBoolResult { Result = false };
      }

      MP2Extended.Users.Users.Add(new MP2ExtendedUser
      {
        Name = username,
        Type = typeEnum,
        Password = password
      });

      return new WebBoolResult { Result = true };
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}