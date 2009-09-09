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

using UPnP.Infrastructure.Dv.DeviceTree;

namespace MediaPortal.Services.UPnP
{
  /// <summary>
  /// Encapsulates the UPnP service for the MediaPortal-II content directory.
  /// </summary>
  /// <remarks>
  /// This service works similar to the ContentDirectory service of the UPnP standard MediaServer device, but it uses a bit
  /// different data structure for media items, so it isn't compatible with the standard ContentDirectory service. It also
  /// provides special actions to manage shares and media item aspect metadata schemas.
  /// </remarks>
  public class UPnPContentDirectoryService : DvService
  {
    public const string SERVICE_TYPE = "schemas-team-mediaportal-com:service:ContentDirectory";
    public const int SERVICE_TYPE_VERSION = 1;
    public const string SERVICE_ID = "urn:team-mediaportal-com:serviceId:ContentDirectory";

    public UPnPContentDirectoryService() : base(SERVICE_TYPE, SERVICE_TYPE_VERSION, SERVICE_ID)
    {
      // TODO: Add state variables and actions
    }
  }
}
