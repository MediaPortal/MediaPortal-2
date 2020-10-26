#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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
using System.Collections.Generic;

namespace MediaPortal.Extensions.OnlineLibraries.Wrappers
{
  public enum UpdateType
  {
    Series,
    Season,
    Episode,
    Movie,
    MovieCollection,
    Audio,
    AudioAlbum,
    Person,
    Actor,
    Director,
    Writer,
    Artist,
    Composer,
    Company,
    TVNetwork,
    MusicLabel
  };

  /// <summary>
  /// EventArgs used when an update has finished, contains start date, end date and 
  /// an overview of all updated content
  /// </summary>
  public class UpdateFinishedEventArgs : EventArgs
  {
    /// <summary>
    /// Constructor for UpdateFinishedEventArgs
    /// </summary>
    /// <param name="started">When did the update start</param>
    /// <param name="ended">When did the update finish</param>
    /// <param name="updateType">The type items that were updated</param>
    /// <param name="updatedItems">List of all items (ids) that were updated</param>
    public UpdateFinishedEventArgs(DateTime started, DateTime ended, UpdateType updateType, List<string> updatedItems)
    {
      UpdateStarted = started;
      UpdateFinished = ended;
      UpdatedItemType = updateType;
      UpdatedItems = updatedItems;
    }
    /// <summary>
    /// When did the update start
    /// </summary>
    public DateTime UpdateStarted { get; set; }

    /// <summary>
    /// When did the update finish
    /// </summary>
    public DateTime UpdateFinished { get; set; }

    /// <summary>
    /// The type of items updated
    /// </summary>
    public UpdateType UpdatedItemType { get; set; }

    /// <summary>
    /// List of all items (ids) that were updated
    /// </summary>
    public List<string> UpdatedItems { get; set; }
  }
}
