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

using DirectShow;

namespace MediaPortal.UI.Players.Video.Tools
{
  /// <summary>
  /// StreamInfo class holds information about available StreamSelectors, Indexes and Names.
  /// </summary>
  public class StreamInfo
  {
    #region Contructor

    public StreamInfo(IAMStreamSelect streamSelector, int streamIndex, string name, int lcid)
    {
      StreamSelector = streamSelector;
      StreamIndex = streamIndex;
      Name = name;
      LCID = lcid;
      IsAutoSubtitle = false;
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets or Sets the stream index.
    /// </summary>
    public int StreamIndex { get; set; }

    /// <summary>
    /// Gets or Sets the stream name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or Sets reference to the IAMStreamSelect instance, that provided the information.
    /// </summary>
    public IAMStreamSelect StreamSelector { get; set; }

    /// <summary>
    /// Gets or Sets the LCID (Locale ID).
    /// </summary>
    public int LCID { get; set; }

    /// <summary>
    /// Gets or Sets the channel count. This property is used only for audio streams.
    /// </summary>
    public int ChannelCount { get; set; }

    /// <summary>
    /// Gets or Sets, if the current subtitle-stream is a standard-subtitle or an auto-subtitle. This property is used only for subtitle streams.
    /// </summary>
    public bool IsAutoSubtitle { get; set; }

    #endregion

    #region Overrides

    public override string ToString()
    {
      return Name;
    }

    #endregion
  }
}