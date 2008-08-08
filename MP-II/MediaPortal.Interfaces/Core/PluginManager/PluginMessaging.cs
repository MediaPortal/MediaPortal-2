#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using System.Text;

namespace MediaPortal.Interfaces.Core.PluginManager
{
  /// <summary>
  /// PluginMessaging class provides an interface for the messages used by to Plugin Manager.
  /// OnPluginStartupFinished is send by the PluginManager when once all plugins have been loaded.
  /// OnPluginInitialised is send for each plugin after the IPlugin Initialise method has been run.
  /// </summary>
  public class PluginMessaging
  {
    // Message Queue name
    public const string Queue = "Plugin";

    // Metadata
    public const string PluginName = "Name"; // PluginName stored as string
    public const string Notification = "Notification"; // Notification stored as NotificationType

    public enum NotificationType
    {
      OnPluginStartupFinished,
      OnPluginInitialise
    }
  }
}
