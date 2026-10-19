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

using System.Text.Json.Serialization;

namespace MediaPortal.Frontend.Settings;

/// <summary>
/// Everything the WMC skin lets its viewer change, property for property as the reference filed them in
/// <c>Source\UI\Skins\WMC\Settings\WMCSkinSettings.cs</c>. Eleven, and exactly eleven: the reference gave
/// WMC no colour switches, no scroll group and no header group, and this class does not either - the
/// absence is the fact a settings screen has to be able to show.
/// </summary>
public sealed class WMCSkinSettings : SkinSettingsBase
{
  public const string SKIN_NAME = "wmc";

  public WMCSkinSettings()
  {
    FanartOverlayOpacity = 1.0;
  }

  public override string SkinName => SKIN_NAME;

  public static WMCSkinSettings Load(string? path, out string? problem)
  {
    return Read<WMCSkinSettings>(path, out problem);
  }

  /// <summary>WMC's home content: the panels behind the home menu that made the skin what it was.</summary>
  [JsonPropertyName("enableHomeContent")]
  public bool EnableHomeContent { get; set; } = true;
}
