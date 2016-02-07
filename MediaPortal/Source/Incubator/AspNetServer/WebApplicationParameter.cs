#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using Microsoft.AspNet.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace MediaPortal.Plugins.AspNetServer
{
  /// <summary>
  /// Stores all necessary parameters to start (or stop) a WebApplication
  /// </summary>
  /// <remarks>
  /// If this class is used to stop a WebApplication, it is sufficient to pass a webApplicationName to the constructor, which is not null.
  /// If this class is used to start a WebApplication, <see cref="TryInitialize"/> must additionally be called with valid parameters.
  /// Two instances of this class are considered equal, if (ignoring case) the <see cref="WebApplicationName"/>s are equal.
  /// </remarks>
  public class WebApplicationParameter
  {
    #region Public properties

    /// <summary>
    /// Unique name to identify the WebApplication
    /// </summary>
    /// <remarks>Constructor ensures that this is never null</remarks>
    public string WebApplicationName { get; }

    /// <summary>
    /// Indicates if <see cref="TryInitialize"/> has been called with valid parameters
    /// </summary>
    /// <remarks>
    /// <c>true</c> if <see cref="TryInitialize"/> has been called with valid parameters
    /// <c>false</c> if <see cref="TryInitialize"/> has not yet been called or the parameters were not valid
    /// </remarks>
    public bool Initialized { get; private set; }

    /// <summary>
    /// Action that uses the <see cref="IServiceCollection"/> parameter to configure the dependencies
    /// </summary>
    /// <remarks>Only valid if <see cref="Initialized"/> is <c>true</c></remarks>
    public Action<IServiceCollection> ConfigureServices { get; private set; }

    /// <summary>
    /// Action that uses the <see cref="IApplicationBuilder"/> parameter to configure the WebApplication
    /// </summary>
    /// <remarks>Only valid if <see cref="Initialized"/> is <c>true</c></remarks>
    public Action<IApplicationBuilder> ConfigureApp { get; private set; }

    /// <summary>
    /// TCP port on which the WebApplication is supposed to listen
    /// </summary>
    /// <remarks>Only valid if <see cref="Initialized"/> is <c>true</c></remarks>
    public int Port { get; private set; }

    /// <summary>
    /// Base path on which the WebApplication is supposed to listen
    /// </summary>
    /// <remarks>Only valid if <see cref="Initialized"/> is <c>true</c></remarks>
    public string BasePath { get; private set; }

    /// <summary>
    /// Http-Url valid for the given <see cref="Port"/> and <see cref="BasePath"/> on any local hostname
    /// </summary>
    /// <remarks>Must only be called after <see cref="TryInitialize"/> has been called successfully before</remarks>
    /// <exception cref="InvalidOperationException"><see cref="TryInitialize"/> was not successfully called before</exception>
    public string Url
    {
      get
      {
        if (!Initialized)
          throw new InvalidOperationException("WebApplicationParameter not initialized.");
        return $"http://*:{Port}{BasePath}";
      }
    }

    #endregion

    #region Constructor

    /// <summary>
    /// Creates an instance of this class and ensures that <paramref name="webApplicationName"/> is not null
    /// </summary>
    /// <param name="webApplicationName">Unique name to identify the WebApplication</param>
    /// <exception cref="ArgumentNullException"><paramref name="webApplicationName"/> was null</exception>
    public WebApplicationParameter(string webApplicationName)
    {
      if (webApplicationName == null)
        throw new ArgumentNullException(nameof(webApplicationName));
      WebApplicationName = webApplicationName;
      Initialized = false;
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Checks if all parameters are valid and, if so, initializes the respective properties and sets <see cref="Initialized"/> to true
    /// </summary>
    /// <param name="configureServices">Action that uses the <see cref="IServiceCollection"/> parameter to configure the dependencies</param>
    /// <param name="configureApp">Action that uses the <see cref="IApplicationBuilder"/> parameter to configure the WebApplication</param>
    /// <param name="port">TCP port on which the WebApplication is supposed to listen</param>
    /// <param name="basePath">Base path on which the WebApplication is supposed to listen</param>
    /// <returns><c>true</c> if all parameters were valid; else <c>false</c></returns>
    public bool TryInitialize(Action<IServiceCollection> configureServices, Action<IApplicationBuilder> configureApp, int port, string basePath)
    {
      if (configureServices == null)
      {
        ServiceRegistration.Get<ILogger>().Warn("AspNetServerService: Cannot start {0}. ConfigureServices was null.", this);
        return false;
      }
      ConfigureServices = configureServices;

      if (configureApp == null)
      {
        ServiceRegistration.Get<ILogger>().Warn("AspNetServerService: Cannot start {0}. ConfigureApp was null.", this);
        return false;
      }
      ConfigureApp = configureApp;

      if (!ValidatePort(port))
      {
        ServiceRegistration.Get<ILogger>().Warn("AspNetServerService: Cannot start {0}. Port {1} is invalid.", this, port);
        return false;
      }
      Port = port;

      if (!ValidateBasePath(ref basePath))
      {
        ServiceRegistration.Get<ILogger>().Warn("AspNetServerService: Cannot start {0}. BasePath {1} is invalid.", this, basePath);
        return false;
      }
      BasePath = basePath;

      Initialized = true;
      return true;
    }

    /// <summary>
    /// Checks if the <see cref="BasePath"/> of this <see cref="WebApplicationParameter"/> overlaps with <paramref name="other.BasePath"/>
    /// </summary>
    /// <param name="other"><see cref="WebApplicationParameter"/> to check</param>
    /// <remarks>Must only be called after <see cref="TryInitialize"/> has been called successfully before</remarks>
    /// <exception cref="InvalidOperationException"><see cref="TryInitialize"/> was not successfully called before</exception>
    /// <returns><c>true</c> if there is an overlap; else <c>false</c></returns>
    public bool BasePathOverlapsWith(WebApplicationParameter other)
    {
      if (!Initialized)
        throw new InvalidOperationException("WebApplicationParameter not initialized.");
      return Url.StartsWith(other.Url, StringComparison.OrdinalIgnoreCase) ||
             other.Url.StartsWith(Url, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Validates a given base path
    /// </summary>
    /// <param name="basePath">Base path to validate</param>
    /// <returns><c>true</c> if <param name="basePath"></param> is valid, else <c>false</c></returns>
    /// <remarks>
    /// A base path must
    ///   - start with a '/' and end with a '/'
    ///   - consist of any number of path segments separated by '/'
    ///   - not contain characters other than 'a'-'z', 'A'-'Z', '0'-'9', '/', '-' or '_'
    /// If <param name="basePath"></param> is null or an empty string, it is modified to "/" and considered valid
    /// If <param name="basePath"></param> does not end with '/' but is otherwise valid, an '/' is appended.
    /// </remarks>
    private bool ValidateBasePath(ref string basePath)
    {
      if (string.IsNullOrEmpty(basePath))
      {
        basePath = "/";
        return true;
      }

      if (basePath[0] != '/')
        return false;

      for (var i = 1; i < basePath.Length; i++)
      {
        var c = basePath[i];
        // See http://www.ietf.org/rfc/rfc3986.txt
        var safe = (('a' <= c && c <= 'z')
                    || ('A' <= c && c <= 'Z')
                    || ('0' <= c && c <= '9')
                    || c == '/' || c == '-' || c == '_');
        if (!safe)
          return false;
      }

      if (basePath[basePath.Length - 1] != '/')
        basePath += '/';

      return true;
    }

    /// <summary>
    /// Validates a given port number
    /// </summary>
    /// <param name="port">Port number to validate</param>
    /// <returns><c>true</c> if <param name="port"></param> is valid, else <c>false</c></returns>
    private bool ValidatePort(int port)
    {
      return (port > 0 && port < 65536);
    }

    #endregion

    #region Base overrides

    /// <summary>
    /// Turns this <see cref="WebApplicationParameter"/> into a human readable string
    /// </summary>
    /// <returns>A human readable string representing this <see cref="WebApplicationParameter"/></returns>
    public override string ToString()
    {
      return !Initialized ? $"WebApplication '{WebApplicationName}'" : $"WebApplication '{WebApplicationName}' (URL '{Url}')";
    }

    /// <summary>
    /// Checks this <see cref="WebApplicationParameter"/> for equality
    /// </summary>
    /// <param name="obj">Object to compare with</param>
    /// <returns><c>true</c> if the <see cref="WebApplicationName"/>s (ignoring case) are equal; else <c>false</c></returns>
    public override bool Equals(object obj)
    {
      var webApplicationWrapper = obj as WebApplicationParameter;
      return webApplicationWrapper != null && WebApplicationName.Equals(webApplicationWrapper.WebApplicationName, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Creates a HashCode for this <see cref="WebApplicationParameter"/>
    /// </summary>
    /// <returns>HashCode base on the lower case representation of the <see cref="WebApplicationName"/></returns>
    public override int GetHashCode()
    {
      return WebApplicationName.ToLower().GetHashCode();
    }

    #endregion
  }
}
