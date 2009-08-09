using System;

namespace HttpServer.Controllers
{
    /// <summary>
    /// Marks methods to let framework know that the method is protected
    /// </summary>
    /// <seealso cref="AuthValidatorAttribute"/>
    /// <seealso cref="RequestController"/>
    public class AuthRequiredAttribute : Attribute
    {
        private readonly int _level;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthRequiredAttribute"/> class.
        /// </summary>
        public AuthRequiredAttribute()
        {
            _level = 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="level">
        /// Level is a value defined by you. It should be used to determine the users
        /// that can access the method tagged with the AuthRequired attribute.
        /// </param>
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
        public AuthRequiredAttribute(int level)
        {
            _level = level;
        }

        /// <summary>
        /// Level is a value defined by you. It should be used to determine the users
        /// that can access the method tagged with the AuthRequired attribute.
        /// </summary>
        public int Level
        {
            get { return _level; }
        }
    }
}
