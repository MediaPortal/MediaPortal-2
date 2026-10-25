/*
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

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediaPortal.Frontend.Api;

namespace MediaPortal.Frontend.ViewModels
{
  /// <summary>
  /// The pages that stand behind the settings hub's media door.
  /// </summary>
  /// <remarks>
  /// <para>
  /// <b>Why a door with pages instead of a page with rows.</b> The reference's configuration tree knew
  /// <c>/Configuration/Settings</c> with two children, <c>Shares</c> and <c>Media</c>, and <c>Media</c> with
  /// eight children of its own - the audio, video, film, series, image and subtitle readers, the picture store
  /// and the languages (ServerSettings.Client/plugin.xml:93). A household walked a wall of plates to a page and
  /// read one page at a time; it never saw thirty fields at once. That is what a door with pages gives, and it
  /// is why the answers are not collected into one long list any more.
  /// </para>
  /// <para>
  /// <b>The server owns the table.</b> The pages behind the door are the sections the server lists for the media
  /// area, asked every time the door opens and never kept here: the day somebody opens another section in the
  /// server's catalog it stands on the television by itself, which is the same reason the plugin screen gives
  /// for the same rule. What a copy of the table would ever be is the table as it used to be.
  /// </para>
  /// <para>
  /// The caller hands over the machine's own page rather than having it built here, because only the
  /// application knows what that page needs - the shelf in use, the file the answers are saved to - and this
  /// only knows how a door's pages are assembled.
  /// </para>
  /// </remarks>
  public static class MediaSettingsPages
  {
    /// <summary>
    /// The wall behind the media door: the machine's own page first, then one page per section the server says
    /// belongs to the media area, in the order the server lists them.
    /// </summary>
    /// <param name="apiProvider">
    /// Where the server is reached, asked again every time the door opens - a viewer may point at a different
    /// server between visits, and the answer is the server's either way.
    /// </param>
    /// <param name="machine">
    /// The page of the answers that belong to this box. First of the wall, because a viewer who came for "my
    /// disk" should not have to walk past the server's answers to reach it.
    /// </param>
    public static async Task<IReadOnlyList<SettingsSection>> PagesAsync(Func<IPluginSettingsApi?> apiProvider,
        SettingsSection machine)
    {
      if (apiProvider == null)
        throw new ArgumentNullException(nameof(apiProvider));
      if (machine == null)
        throw new ArgumentNullException(nameof(machine));

      var pages = new List<SettingsSection> { machine };

      IPluginSettingsApi? api = apiProvider();
      if (api == null)
      {
        // A machine with no server has only its own answers, and says so by standing one plate wide rather
        // than by standing a page in front of a sentence.
        return pages;
      }

      SettingSectionsAnswer sections =
          await api.GetSettingSectionsAsync(SettingSectionAreas.Media).ConfigureAwait(false);

      foreach (SettingSectionSummary summary in sections.Sections)
      {
        // The same plate the plugin wall stands on - the same screen, the same rule for its picture - so that
        // the two walls that lead to the server's sections are one wall in everything but which branch they
        // answer for.
        pages.Add(PluginSettingsPages.Page(apiProvider, summary, PluginSettingsPages.PictureFor(summary.Key)));
      }

      return pages;
    }
  }
}
