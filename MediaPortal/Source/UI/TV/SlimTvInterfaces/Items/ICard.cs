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
using System.Collections.Generic;
using System.ComponentModel;

namespace MediaPortal.Plugins.SlimTv.Interfaces.Items
{

  public enum SlimTvCamType
  {
    Default = 0,
    Astoncrypt2 = 1
  }

  /// <summary>
  /// ICard represents a card.
  /// </summary>
  public interface ICard
  {
    /// <summary>
    /// Gets or Sets the Channel ID.
    /// </summary>    
    int CardId { get; set; }

    /// <summary>
    /// Gets or Sets the Name.
    /// </summary>      
    string Name { get; set; }

    /// <summary>
    /// Gets or Sets if EPG is grabbing on this card.
    /// </summary>
    bool EpgIsGrabbing { get; set; }

    /// <summary>
    /// Gets or Sets if the Card has a cam
    /// </summary>
    bool HasCam { get; set; }

    /// <summary>
    /// Gets or Sets the Cam Type
    /// </summary>
    SlimTvCamType CamType { get; set; }

    /// <summary>
    /// Gets or Sets the Decrypt Limit
    /// </summary>
    int DecryptLimit { get; set; }

    /// <summary>
    /// Gets or Sets the Device Path
    /// </summary>
    string DevicePath { get; set; }

    /// <summary>
    /// Gets or Sets if the Card is enabled
    /// </summary>
    bool Enabled { get; set; }

    /// <summary>
    /// Gets or Sets the Recording Folder
    /// </summary>
    string RecordingFolder { get; set; }

    /// <summary>
    /// Gets or Sets the Recording format
    /// </summary>
    int RecordingFormat { get; set; }

    /// <summary>
    /// Gets or Sets the Timeshifting Folder
    /// </summary>
    string TimeshiftFolder { get; set; }

    /// <summary>
    /// Gets or Sets the Card priority
    /// </summary>
    int Priority { get; set; }

    /// <summary>
    /// Gets or Sets if the Card is preloaded
    /// </summary>
    bool PreloadCard { get; set; }

    /// <summary>
    /// Gets or Sets if Subchannels are supported
    /// </summary>
    bool SupportSubChannels { get; set; }
  }
}
