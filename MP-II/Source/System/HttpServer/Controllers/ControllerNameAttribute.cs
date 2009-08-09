using System;

namespace HttpServer.Controllers
{
    /// <summary>
    /// This attribute can be used to map a controller to a specific url without using
    /// the class name.
    /// </summary>
    public class ControllerNameAttribute : Attribute
    {
        private readonly string _name;

        /// <summary>
        /// Maps a controller to a url without using the controller name.
        /// </summary>
        /// <remarks>
        /// <para>Per default the class name is used to determine which url to handle.
        /// For instance, "class UserController" or "class User" tells the framework that
        /// the urls that starts with "/user" should be handled by that controller.</para>
        /// <para>This attribute can be used to circumvent  that.</para>
        /// </remarks>
        /// <param name="name">The name.</param>
        public ControllerNameAttribute(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException("name");

            _name = name;
        }

        /// <summary>
        /// The name that the controller should use
        /// </summary>
        public string Name
        {
            get { return _name; }
        }
    }
}
