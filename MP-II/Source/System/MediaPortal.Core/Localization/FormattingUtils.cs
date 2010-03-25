#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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
using MediaPortal.Core.Services.Localization;
using MediaPortal.Core.Settings;

namespace MediaPortal.Core.Localization
{
  /// <summary>
  /// Utility class for formatting several things.
  /// </summary>
  public class FormattingUtils
  {
    /// <summary>
    /// Formats a media duration timespan (i.e. the duration of a media item, the current play time, ...) to a string.
    /// </summary>
    /// <param name="duration">Duration to format.</param>
    /// <returns>String containing the given <paramref name="duration"/>. The returned value will be a string similar to
    /// <c>5:41</c> or <c>5 min, 41 sec</c>, depending on the system setting
    /// <see cref="LocalizationSettings.MediaDurationFormat"/>.</returns>
    public static string FormatMediaDuration(TimeSpan duration)
    {
      LocalizationSettings settings = ServiceScope.Get<ISettingsManager>().Load<LocalizationSettings>();
      return string.Format(settings.MediaDurationFormat, duration.TotalMinutes, duration.Seconds);
    }
  }
}