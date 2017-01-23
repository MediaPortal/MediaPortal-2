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

using System.Net;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Settings;
using MediaPortal.Utilities.Xml;

namespace MediaPortal.Extensions.ResourceProviders.NetworkNeighborhoodResourceProvider.Settings
{
  public class NetworkNeighborhoodResourceProviderSettings
  {
    public NetworkNeighborhoodResourceProviderSettings()
    {
      NetworkCredentials = new SerializableDictionary<string, SerializableNetworkCredential>();
    }

    /// <summary>
    /// Dictionary of <see cref="ResourcePath"/>-strings and the respective <see cref="NetworkCredential"/>s to access network resources.
    /// </summary>
    [Setting(SettingScope.Global)]
    public SerializableDictionary<string, SerializableNetworkCredential> NetworkCredentials { get; set; }
  }
}
