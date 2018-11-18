#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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

using MediaPortal.UI.Presentation.DataObjects;
using System;
using System.Threading.Tasks;

namespace MediaPortal.UiComponents.Media.MediaLists
{
  [Flags]
  public enum UpdateReason
  {
    None = 0,
    Forced = 1,
    PeriodicMinute = 2,
    ImportComplete = 4,
    PlaybackComplete = 8
  }

  public interface IMediaListProvider
  {
    /// <summary>
    /// List of all the items found by this provider
    /// </summary>
    ItemsList AllItems { get; }

    /// <summary>
    /// Update the list
    /// </summary>
    /// <returns></returns>
    Task<bool> UpdateItemsAsync(int maxItems, UpdateReason updateReason);
  }
}
