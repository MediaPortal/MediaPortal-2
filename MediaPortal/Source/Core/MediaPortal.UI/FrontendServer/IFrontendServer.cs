#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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

using UPnP.Infrastructure.Dv;

namespace MediaPortal.UI.FrontendServer
{
  /// <summary>
  /// Control interface for the frontend's server components - UPnP, ...
  /// </summary>
  public interface IFrontendServer
  {
    /// <summary>
    /// Returns the instance of the UPnP frontend server instance. Plugins may change that instance.
    /// </summary>
    UPnPServer UPnPFrontendServer { get; }

    /// <summary>
    /// Starts the frontend server.
    /// </summary>
    void Startup();

    /// <summary>
    /// Shuts the frontend server down.
    /// </summary>
    void Shutdown();

    /// <summary>
    /// Updates the UPnP device configuration. That re-advertises the UPnP device in the network
    /// with the changed configuration. That must be done if the name of the client's UPnP service
    /// was changed, for example.
    /// </summary>
    void UpdateUPnPConfiguration();
  }
}