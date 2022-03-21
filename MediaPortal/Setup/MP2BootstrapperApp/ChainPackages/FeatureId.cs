#region Copyright (C) 2007-2021 Team MediaPortal

/*
    Copyright (C) 2007-2021 Team MediaPortal
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

namespace MP2BootstrapperApp.ChainPackages
{
  /// <summary>
  /// Ids of the features in the MediaPortal 2 package.
  /// </summary>
  public enum FeatureId
  {
    /// <summary>
    /// Id of the parent feature for all optional features.
    /// </summary>
    MediaPortal_2,

    /// <summary>
    /// Id of the client feature.
    /// </summary>
    Client,

    /// <summary>
    /// Id of the server feature.
    /// </summary>
    Server,

    /// <summary>
    /// Id of the ServiceMonitor feature.
    /// </summary>
    ServiceMonitor,

    /// <summary>
    /// Id of the log collector feature.
    /// </summary>
    LogCollector,

    /// <summary>
    /// Placeholder for unknown features.
    /// </summary>
    Unknown
  }
}
