using System;
using HttpServer;
using HttpServer.HttpModules;

namespace HttpServer.Controllers
{
    /// <summary>
    /// Method marked with this attribute determines if authentication is required.
    /// </summary>
    /// <seealso cref="ControllerModule"/>
    /// <seealso cref="HttpServer"/>
    /// <seealso cref="AuthRequiredAttribute"/>
    /// <seealso cref="WebSiteModule"/>
    /// <remarks>
    /// <para>The method should take one parameter (int level), return a bool and be protected/private.</para>
    /// <para>You should throw UnauthorizedException if you are using HTTP authentication.</para>
    /// </remarks>
    /// <example>
    /// <![CDATA[
    /// public enum UserLevel
    /// {
    ///   Guest,
    ///   User,
    ///   Admin,
    ///   SuperAdmin
    /// }
    /// public class UserController : RequestController
    /// {
    ///   [AuthRequired(UserLevel.User)]
    ///   public string View()
    ///   {
    ///     return "Can also be viewed by users";
    ///   }
    /// 
    ///   [AuthValidatorAttribute]
    ///   public bool ValidateUser(int level)
    ///   {
    ///      (User)user = Session["user"];
    ///      return user != null && user.Status >= level;
    ///   }
    /// }
    /// ]]>
    /// </example>
    public class AuthValidatorAttribute : Attribute
    {
    }
}
