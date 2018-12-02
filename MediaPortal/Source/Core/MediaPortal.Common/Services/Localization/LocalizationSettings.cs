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

using MediaPortal.Common.Settings;

namespace MediaPortal.Common.Services.Localization
{
  public class LocalizationSettings
  {
    protected string _mediaDurationFormat = "{0}:{1:D2}";
    protected string _mediaDurationHourFormat = "{0}:{1:D2}:{2:D2}";

    /// <summary>
    /// Returns a format string to format a media duration timespan. The format string has two placeholders for
    /// the minutes and the seconds.
    /// </summary>
    /// <remarks>
    /// Typically, the returned format string is used like this:
    /// <code>
    /// LocalizationSettings ls = ...;
    /// TimeSpan duration = ...;
    /// string durationStr = string.Format(ls.MediaDurationFormat, duration.TotalMinutes, duration.Seconds)
    /// </code>
    /// </remarks>
    [Setting(SettingScope.User)]
    public string MediaDurationFormat
    {
      get { return _mediaDurationFormat; }
      set { _mediaDurationFormat = value; }
    }

    /// <summary>
    /// Returns a format string to format a media duration timespan that is greater than one hour. The format string has three placeholders for
    /// the hours, minutes and the seconds.
    /// </summary>
    /// <remarks>
    /// Typically, the returned format string is used like this:
    /// <code>
    /// LocalizationSettings ls = ...;
    /// TimeSpan duration = ...;
    /// string durationStr = string.Format(ls.MediaDurationHourFormat, duration.TotalHours, duration.Minutes, duration.Seconds)
    /// </code>
    /// </remarks>
    [Setting(SettingScope.User)]
    public string MediaDurationHourFormat
    {
      get { return _mediaDurationHourFormat; }
      set { _mediaDurationHourFormat = value; }
    }
  }
}