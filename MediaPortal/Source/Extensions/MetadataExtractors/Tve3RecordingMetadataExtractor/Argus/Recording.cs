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

#region Original Copyright

/*
 *	Copyright (C) 2007-2014 ARGUS TV
 *	http://www.argus-tv.com
 *
 *  This Program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation; either version 2, or (at your option)
 *  any later version.
 *
 *  This Program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with GNU Make; see the file COPYING.  If not, write to
 *  the Free Software Foundation, 675 Mass Ave, Cambridge, MA 02139, USA.
 *  http://www.gnu.org/copyleft/gpl.html
 *
 */

#endregion

using System;

namespace MediaPortal.Extensions.MetadataExtractors.Argus
{
  /// <summary>
  /// A recorded program.
  /// Note: Non-serialized and special typed properties where removed here. If they will be needed in future, they have to be
  /// uncommented and the related types to be added.
  /// <see cref="https://github.com/ARGUS-TV/ARGUS-TV/blob/master/ArgusTV.DataContracts/Recording.cs"/> for original version.
  /// </summary>
  public class Recording
  {
    /// <summary>
    /// The unique ID of the recording.
    /// </summary>
    public Guid RecordingId { get; set; }

    /// <summary>
    /// The unique integer ID of the recording.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The original schedule ID (may no longer exist).
    /// </summary>
    public Guid ScheduleId { get; set; }

    /// <summary>
    /// The original schedule name.
    /// </summary>
    public string ScheduleName { get; set; }

    /// <summary>
    /// The original schedule priority.
    /// </summary>
    //public SchedulePriority SchedulePriority { get; set; }

    /// <summary>
    /// Is this recording part of a series?
    /// </summary>
    public bool IsPartOfSeries { get; set; }

    /// <summary>
    /// Defines how long to keep this recording before deleting it.
    /// </summary>
    //public KeepUntilMode KeepUntilMode { get; set; }

    /// <summary>
    /// Defines how long to keep this recording before deleting it (see KeepUntilMode).
    /// </summary>
    public int? KeepUntilValue { get; set; }

    /// <summary>
    /// The last time this recording was watched or null if it was never watched.
    /// </summary>
    public DateTime? LastWatchedTime { get; set; }

    /// <summary>
    /// The position until where the recording was last watched (in seconds), or null if it was never watched.
    /// </summary>
    public int? LastWatchedPosition { get; set; }

    /// <summary>
    /// Has this recording been fully watched (by default this means 90% of the program was watched)?
    /// </summary>
    public bool IsFullyWatched { get; set; }

    /// <summary>
    /// Holds the number of times this recording was fully watched.
    /// </summary>
    public int FullyWatchedCount { get; set; }

    /// <summary>
    /// The channel ID the program was recorded on (may not exist anymore).
    /// </summary>
    public Guid ChannelId { get; set; }

    /// <summary>
    /// The channel display name the program was recorded on.
    /// </summary>
    public string ChannelDisplayName { get; set; }

    /// <summary>
    /// The type of the channel the program was recorded on.
    /// </summary>
    //public ChannelType ChannelType { get; set; }

    /// <summary>
    /// The actual start time of the recording.
    /// </summary>
    public DateTime RecordingStartTime { get; set; }

    /// <summary>
    /// The actual stop time of the recording.
    /// </summary>
    public DateTime? RecordingStopTime { get; set; }

    /// <summary>
    /// The actual start time of the recording (UTC).
    /// </summary>
    public DateTime RecordingStartTimeUtc { get; set; }

    /// <summary>
    /// The actual stop time of the recording (UTC).
    /// </summary>
    public DateTime? RecordingStopTimeUtc { get; set; }

    /// <summary>
    /// The filename of the recording.
    /// </summary>
    [System.Xml.Serialization.XmlIgnore]
    public string RecordingFileName { get; set; }

    /// <summary>
    /// Is this a partial recording?
    /// </summary>
    public bool IsPartialRecording { get; set; }

    /// <summary>
    /// The start time of the recorded program.
    /// </summary>
    public DateTime ProgramStartTime { get; set; }

    /// <summary>
    /// The stop time of the recorded program.
    /// </summary>
    public DateTime ProgramStopTime { get; set; }

    /// <summary>
    /// The start time of the recorded program (UTC).
    /// </summary>
    public DateTime ProgramStartTimeUtc { get; set; }

    /// <summary>
    /// The stop time of the recorded program (UTC).
    /// </summary>
    public DateTime ProgramStopTimeUtc { get; set; }

    /// <summary>
    /// The title of the recorded program.
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// The episode title of the recorded program.
    /// </summary>
    public string SubTitle { get; set; }

    /// <summary>
    /// The description of the recorded program.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// The category of the recorded program.
    /// </summary>
    public string Category { get; set; }

    /// <summary>
    /// Was the recorded program a repeat?
    /// </summary>
    public bool IsRepeat { get; set; }

    /// <summary>
    /// Was the recorded program a premiere?
    /// </summary>
    public bool IsPremiere { get; set; }

    /// <summary>
    /// The program's flags defining things like aspect ratio, SD or HD,...
    /// </summary>
    //public GuideProgramFlags Flags { get; set; }

    /// <summary>
    /// If known, the series number the recorded program belongs to.
    /// </summary>
    public int? SeriesNumber { get; set; }

    /// <summary>
    /// A string to display the episode number in a UI.
    /// </summary>
    public string EpisodeNumberDisplay { get; set; }

    /// <summary>
    /// If known, the episode number of the recorded program.
    /// </summary>
    public int? EpisodeNumber { get; set; }

    /// <summary>
    /// If known, the total number of episodes in the current series.
    /// </summary>
    public int? EpisodeNumberTotal { get; set; }

    /// <summary>
    /// If known and if applicable, the episode part number of the recorded program.
    /// </summary>
    public int? EpisodePart { get; set; }

    /// <summary>
    /// If known and if applicable, the total number of parts.
    /// </summary>
    public int? EpisodePartTotal { get; set; }

    /// <summary>
    /// The parental rating of the program.
    /// </summary>
    public string Rating { get; set; }

    /// <summary>
    /// If set, a star-rating of the program, normalized to a value between 0 and 1.
    /// </summary>
    public double? StarRating { get; set; }

    /// <summary>
    /// The director of the program.
    /// </summary>
    public string Director { get; set; }

    /// <summary>
    /// The actors appearing in the program.
    /// </summary>
    public string Actors { get; set; }
  }
}
