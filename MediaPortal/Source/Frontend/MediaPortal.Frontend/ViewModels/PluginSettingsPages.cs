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
  /// The pages that stand behind a settings door whose answers are the server's: one page per section the
  /// server lists, asked every time the door opens.
  /// </summary>
  /// <remarks>
  /// <para>
  /// <b>One page, one section.</b> The reference's configuration tree hung one node per settings class under
  /// its branch - <c>Plugins_Weather</c> beside <c>Plugins_Trakt</c>, <c>Media_FanArtSettings</c> beside
  /// <c>Media_MovieMDESettings</c> (ServerSettings.Client/plugin.xml:93) - and a household walked a wall of
  /// plates to reach one of them and read one of them at a time. A screen that lists the server's sections in
  /// one long column therefore was never the reference's answer, and stopped being ours as soon as the hub
  /// grew walls: what a door opens is the wall, and what a plate opens is the section.
  /// </para>
  /// <para>
  /// <b>The server owns the table.</b> Nothing here remembers which sections there were. A section somebody
  /// opens in the server's catalog appears on the television by itself the next time a door opens, which is the
  /// same reason the plugin screen has always given: what a copy of the table can ever be is the table as it
  /// used to be.
  /// </para>
  /// <para>
  /// Both doors that lead to server sections are built here - <see cref="PagesAsync"/> for the plugin area and
  /// <see cref="Page"/> for one section of any area - so that a plate of either wall behaves like a plate of
  /// the other: same screen, same picture rule, same words under the name.
  /// </para>
  /// </remarks>
  public static class PluginSettingsPages
  {
    /// <summary>
    /// The wall behind the plugins door: one page per section the server says belongs to the plugin area, in
    /// the order the server lists them. No server in sight answers an empty wall, which the hub says in its
    /// status line rather than by standing a form in front of nothing.
    /// </summary>
    public static async Task<IReadOnlyList<SettingsSection>> PagesAsync(Func<IPluginSettingsApi?> apiProvider)
    {
      if (apiProvider == null)
        throw new ArgumentNullException(nameof(apiProvider));

      var pages = new List<SettingsSection>();

      IPluginSettingsApi? api = apiProvider();
      if (api == null)
        return pages;

      SettingSectionsAnswer sections =
          await api.GetSettingSectionsAsync(SettingSectionAreas.Plugin).ConfigureAwait(false);

      foreach (SettingSectionSummary summary in sections.Sections)
        pages.Add(Page(apiProvider, summary, "Plugins"));

      return pages;
    }

    /// <summary>
    /// One of the server's sections as a plate: the section's own title and sentence, and the screen that
    /// edits exactly that section - the wall outside has already listed the sections, so what a plate opens is
    /// the section and not a list of one.
    /// </summary>
    /// <param name="apiProvider">
    /// Where the server is reached, asked again every time the page opens; the viewer may point at a different
    /// server between visits, and the answer is the server's either way.
    /// </param>
    /// <param name="summary">The section as the server named it.</param>
    /// <param name="picture">
    /// The plate's picture, spelled the reference's way. Where the reference had several readers and one
    /// picture for each, the section's own key says which of them this is; see <see cref="PictureFor"/>.
    /// </param>
    /// <param name="icon">The band's icon, or the empty word to ask for the plate's own name.</param>
    public static SettingsSection Page(Func<IPluginSettingsApi?> apiProvider, SettingSectionSummary summary,
        string picture, string icon = "")
    {
      if (apiProvider == null)
        throw new ArgumentNullException(nameof(apiProvider));
      if (summary == null)
        throw new ArgumentNullException(nameof(summary));

      SettingSectionSummary door = summary;
      return new SettingsSection(door.Title, "Server", door.Description,
          delegate
          {
            PluginSettingsViewModel page = new PluginSettingsViewModel(apiProvider, door);
            _ = page.ReloadAsync();
            return new Views.PluginSettingsView { DataContext = page };
          }, picture, icon);
    }

    /// <summary>
    /// The plate a section of the media wall wears. The reference drew one picture per media branch node and
    /// named it after the node (<c>Media_FanArtSettings.jpg</c>, <c>Media_MovieMDESettings.jpg</c>,
    /// <c>Media_ImageMDESettings.jpg</c>), and the server's sections are the same questions under other names:
    /// the picture store is the reference's picture store, a reader of a .nfo file is that branch's reader, and
    /// what belongs on the shelves without standing on a share is the branch's own general answer. A section
    /// nobody drew a plate for wears the branch's picture, which is a plate of the same family rather than a
    /// hole in the wall.
    /// </summary>
    public static string PictureFor(string sectionKey) =>
        sectionKey switch
        {
          "fanart" => "Media_FanArtSettings",
          "certifications" => "Media_Certifications",
          "virtual_media" => "Media_General",
          "nfo_movie" => "Media_MovieMDESettings",
          "nfo_series" => "Media_SeriesMDESettings",
          "nfo_audio" => "Media_AudioMDESettings",
          "pictures_movie" or "pictures_series" or "pictures_music" => "Media_ImageMDESettings",
          _ => "Media"
        };
  }
}
