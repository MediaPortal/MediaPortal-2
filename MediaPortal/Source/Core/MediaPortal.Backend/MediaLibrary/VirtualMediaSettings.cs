#region Copyright (C) 2007-2026 Team MediaPortal

/*
    Copyright (C) 2007-2026 Team MediaPortal
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
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.Settings;

namespace MediaPortal.Backend.MediaLibrary
{
  /// <summary>
  /// Whether the titles the library knows about but cannot show are worth answering a browse with: the second
  /// film of a collection that stands on no share, the eleventh episode of a series whose tenth is still
  /// missing, the recordings of an album nobody has.
  /// </summary>
  /// <remarks>
  /// <para>
  /// MP2 has always been able to hold on to such a title. The importer decides whether the library does hold it
  /// (the extractors' <c>OnlyLocalMedia</c>, which is what the old configuration program called "download
  /// metadata for every film of a collection"), and every browse the library answers carries an
  /// <c>includeVirtual</c> argument that decides whether the ones it holds take part in the answer. The reference
  /// kept one switch per kind of media beside the browsing choices of its media component and handed exactly that
  /// to the argument, so this class carries the same three answers for the same reason: they are the only place
  /// where that argument can be steered from a screen.
  /// </para>
  /// <para>
  /// All three are off, as they were there, and off is the right answer for a room with a television in it. An
  /// invented title has nothing to play; a shelf full of covers that turn out to be promises is a shelf a
  /// household stops trusting. The ones who want it are the ones who import a series they are still collecting
  /// and would rather see the whole of it than the part they own.
  /// </para>
  /// <para>
  /// The reference stored these per signed-in person, beside the rest of its browsing choices. This server keeps
  /// one answer for the household, because a browse is answered before anybody is known - the person a list is
  /// read for arrives with the request, not with the setting - and because the endpoint that writes these answers
  /// writes the server's own file, not one person's.
  /// </para>
  /// </remarks>
  public class VirtualMediaSettings
  {
    /// <summary>
    /// Whether invented films belong in an answer about films.
    /// </summary>
    [Setting(SettingScope.Global, false)]
    public bool ShowVirtualMovieMedia { get; set; }

    /// <summary>
    /// Whether invented episodes, seasons and shows belong in an answer about series.
    /// </summary>
    [Setting(SettingScope.Global, false)]
    public bool ShowVirtualSeriesMedia { get; set; }

    /// <summary>
    /// Whether invented recordings and albums belong in an answer about music.
    /// </summary>
    [Setting(SettingScope.Global, false)]
    public bool ShowVirtualAudioMedia { get; set; }

    /// <summary>
    /// Whether a browse that asks about the given aspects should include the titles the library invented.
    /// </summary>
    /// <param name="aspectIds">The aspects the browse is necessary about, in any order.</param>
    /// <remarks>
    /// Which kind of media a browse is about is read off the aspects it asks about, because that is what the
    /// browse itself says; the same reading the reference used, in the same order of asking. A browse that is
    /// about none of the three - pictures on a share, a station's recordings - is not about invented titles at
    /// all, and gets a no.
    /// </remarks>
    public bool ShowsVirtualFor(IEnumerable<Guid> aspectIds)
    {
      if (aspectIds == null)
        return false;

      List<Guid> asked = new List<Guid>(aspectIds);
      if (asked.Contains(AudioAspect.ASPECT_ID) || asked.Contains(AudioAlbumAspect.ASPECT_ID))
        return ShowVirtualAudioMedia;
      if (asked.Contains(EpisodeAspect.ASPECT_ID) || asked.Contains(SeasonAspect.ASPECT_ID)
          || asked.Contains(SeriesAspect.ASPECT_ID))
        return ShowVirtualSeriesMedia;
      if (asked.Contains(MovieAspect.ASPECT_ID) || asked.Contains(MovieCollectionAspect.ASPECT_ID))
        return ShowVirtualMovieMedia;
      return false;
    }

    /// <summary>
    /// Whether the household lets invented titles into its screens at all, whatever kind of media they are.
    /// </summary>
    /// <remarks>
    /// A title looked up by its id cannot be sorted into a kind before it has been read, and reading it is the
    /// very question being asked. So a lookup that already knows which title is meant asks this question
    /// instead: if invented titles are welcome anywhere in this household, then a title somebody is standing in
    /// front of is found whatever it turned out to be. Without that, switching the answer on would put invented
    /// titles onto the shelves and refuse to open them.
    /// </remarks>
    public bool LetsVirtualTitlesIn
    {
      get { return ShowVirtualMovieMedia || ShowVirtualSeriesMedia || ShowVirtualAudioMedia; }
    }
  }
}
