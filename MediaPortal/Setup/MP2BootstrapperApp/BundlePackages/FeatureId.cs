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

namespace MP2BootstrapperApp.BundlePackages
{
  /// <summary>
  /// Ids of the features in the MediaPortal 2 package.
  /// </summary>
  public static class FeatureId
  {
    /// <summary>
    /// Id of the parent feature for all optional features.
    /// </summary>
    public const string MediaPortal_2 = "MediaPortal_2";

    /// <summary>
    /// Id of the client feature.
    /// </summary>
    public const string Client = "Client";

    /// <summary>
    /// Id of the server feature.
    /// </summary>
    public const string Server = "Server";

    /// <summary>
    /// Id of the ServiceMonitor feature.
    /// </summary>
    public const string ServiceMonitor = "ServiceMonitor";

    /// <summary>
    /// Id of the log collector feature.
    /// </summary>
    public const string LogCollector = "LogCollector";

    /// <summary>
    /// Id of the TV Service client feature.
    /// </summary>
    public const string SlimTvServiceClient = "SlimTv.ServiceClient";

    /// <summary>
    /// Id of the TV Service 3 feature
    /// </summary>
    public const string SlimTvService3 = "SlimTv.Service3";

    /// <summary>
    /// Id of the TV Service 3.5 feature
    /// </summary>
    public const string SlimTvService35 = "SlimTv.Service35";

  }
}
