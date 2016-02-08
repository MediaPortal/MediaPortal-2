using System.Diagnostics;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.MP2Extended.Settings;
using HttpServer.Authentication;
using HttpServer.Sessions;

namespace MediaPortal.Plugins.MP2Extended.Authentication
{
  static class CheckRights
  {
    /// <summary>
    /// Returns true if the access to the page is allowed
    /// </summary>
    /// <param name="session">The session provided by the HTTP Server</param>
    /// <param name="userType">The user type the user should have. Should he be admin? Or is a normal user sufficient?</param>
    /// <param name="throwEx">if true an UnauthorizedException will be thrown instead of false. Default is false.</param>
    /// <param name="loginRequired">If true the Server option UseAuth must be set to true in order to gain access. Default ist false.</param>
    /// <returns>true = allowed; false or exception = not allowed</returns>
    internal static bool AccessAllowed(IHttpSession session, UserTypes userType, bool throwEx = false, bool loginRequired = false)
    {
      if (MP2Extended.Settings.UseAuth == false && loginRequired == false)
      {
        Logger.Debug("CheckRights: Access allowed because UseAuth: {0} and login Required: {1}", MP2Extended.Settings.UseAuth.ToString(), loginRequired.ToString());
        return true;
      }

      if (session != null && session[AuthenticationModule.AuthenticationTag] != null)
      {
        User user = session[AuthenticationModule.AuthenticationTag] as User;
        if (user != null && (int)user.Type >= (int)userType) // see userTypes Enum for more information
        {
          Logger.Debug("CheckRights: Access allowed because user != null and {0} >= {1} (user Type ({2}) >= reqired Type ({3}))", (int)user.Type, (int)userType, user.Type.ToString(), userType.ToString());
          return true;
        }
      }

      if (throwEx)
        throw new UnauthorizedException("You don't have enough rights to view this content");

      var mth = new StackTrace().GetFrame(1).GetMethod();
      var callingName = mth.ReflectedType == null ? mth.Name : mth.ReflectedType.Name;

      Logger.Debug("CheckRights: Access denied! Caller: {0}", callingName);

      return false;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
