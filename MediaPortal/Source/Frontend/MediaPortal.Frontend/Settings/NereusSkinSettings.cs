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
using System.Text.Json.Serialization;

namespace MediaPortal.Frontend.Settings;

/// <summary>
/// Everything Nereus lets its viewer change, property for property as the reference filed them in
/// <c>Source\UI\Skins\Nereus\Settings\NereusSkinSettings.cs</c> - the names and the defaults are the
/// reference's, so a screen, a dialog or a translation written against one reads the same as the other.
/// </summary>
/// <remarks>
/// The eight colour bools are the reference's colour choice itself: exactly one skin accent is on,
/// turquoise by default, and the colour dialog of phase C2 will be the thing that moves them - they are
/// deliberately not derived from anything here, because in the reference they were the question, not an
/// answer to one. The scroll, header and dialog groups are the reference's custom dialogs (scrolling,
/// header, dialog opacity); screens act on them as their files did.
/// </remarks>
public sealed class NereusSkinSettings : SkinSettingsBase
{
  public const string SKIN_NAME = "nereus";

  public NereusSkinSettings()
  {
    FanartOverlayOpacity = 0.85;
  }

  public override string SkinName => SKIN_NAME;

  public static NereusSkinSettings Load(string? path, out string? problem)
  {
    return Read<NereusSkinSettings>(path, out problem);
  }

  [JsonPropertyName("enableTouchDisplay")]
  public bool EnableTouchDisplay { get; set; }

  [JsonPropertyName("enableCircleMenuImages")]
  public bool EnableCircleMenuImages { get; set; }

  [JsonPropertyName("enableMenuSelection")]
  public bool EnableMenuSelection { get; set; }

  [JsonPropertyName("enableGridDetails")]
  public bool EnableGridDetails { get; set; } = true;

  [JsonPropertyName("enableCoverDetails")]
  public bool EnableCoverDetails { get; set; } = true;

  // The two dormant dials now speak: a shelf that opens in the grid or the cover takes its details
  // answer from these, exactly as the reference's grid and cover masters bound their details pane to
  // extEnableGridDetails and extEnableCoverDetails.
  [JsonIgnore]
  public override bool EnableGridDetailsView => EnableGridDetails;

  [JsonIgnore]
  public override bool EnableCoverDetailsView => EnableCoverDetails;

  [JsonPropertyName("enableAutoScrolling")]
  public bool EnableAutoScrolling { get; set; } = true;

  [JsonPropertyName("autoScrollSpeed")]
  public double AutoScrollSpeed { get; set; } = 20;

  [JsonPropertyName("autoScrollDelay")]
  public double AutoScrollDelay { get; set; } = 2;

  [JsonPropertyName("enableLoopScrolling")]
  public bool EnableLoopScrolling { get; set; } = true;

  // The header group: what the master's head line paints and in what tint it paints it.
  [JsonPropertyName("useTransparency")]
  public bool UseTransparency { get; set; } = true;

  [JsonPropertyName("useNoColor")]
  public bool UseNoColor { get; set; }

  [JsonPropertyName("useWhiteColor")]
  public bool UseWhiteColor { get; set; }

  [JsonPropertyName("useFocusColor")]
  public bool UseFocusColor { get; set; } = true;

  [JsonPropertyName("showTime")]
  public bool ShowTime { get; set; } = true;

  [JsonPropertyName("showDate")]
  public bool ShowDate { get; set; } = true;

  [JsonPropertyName("showTemperature")]
  public bool ShowTemperature { get; set; } = true;

  [JsonPropertyName("showWeatherCondition")]
  public bool ShowWeatherCondition { get; set; } = true;

  // The dialog group.
  [JsonPropertyName("dialogBackgroundOpacity")]
  public double DialogBackgroundOpacity { get; set; } = 0.85;

  [JsonPropertyName("useRoundedDialogCorners")]
  public bool UseRoundedDialogCorners { get; set; }

  // The colour choice, exactly as the reference filed it: eight mutually exclusive bools, turquoise worn.
  [JsonPropertyName("useTorquoise")]
  public bool UseTorquoise { get; set; } = true;

  [JsonPropertyName("useYellow")]
  public bool UseYellow { get; set; }

  [JsonPropertyName("useOrange")]
  public bool UseOrange { get; set; }

  [JsonPropertyName("useRed")]
  public bool UseRed { get; set; }

  [JsonPropertyName("usePurple")]
  public bool UsePurple { get; set; }

  [JsonPropertyName("useGreen")]
  public bool UseGreen { get; set; }

  [JsonPropertyName("useBlue")]
  public bool UseBlue { get; set; }

  [JsonPropertyName("useGrey")]
  public bool UseGrey { get; set; }

  /// <summary>
  /// The eight families the reference's Colors.xaml spells out, in its order: the family's name and the
  /// light, medium and dark colours it names for it. The hex values are copied from
  /// <c>NereusColorLight_*</c>, <c>NereusColorMedium_*</c> and <c>NereusColorDark_*</c> line for line.
  /// </summary>
  public static readonly (string Name, string Light, string Medium, string Dark)[] COLOUR_FAMILIES =
  {
    ("Turquoise", "#FF009592", "#FF175F5D", "#FF244043"),
    ("Yellow", "#FFD3AC1B", "#FFAE8C0E", "#FF7F6000"),
    ("Orange", "#FFED7D31", "#FFC55A11", "#FF843C0C"),
    ("Red", "#FFCC5350", "#FFA03830", "#FF68150C"),
    ("Purple", "#FF8758CC", "#FF602FA1", "#FF3B1E66"),
    ("Green", "#FF70AD47", "#FF548235", "#FF385723"),
    ("Blue", "#FF4472C4", "#FF2F5597", "#FF203864"),
    ("Grey", "#FF9D9D9D", "#FF717171", "#FF474747")
  };

  /// <summary>
  /// Which family the eight switches point at: the first family whose switch is on, and turquoise when
  /// none is - the settings dialog keeps exactly one on, but a hand-edited file must not leave the skin
  /// without a colour, and the reference defaulted to turquoise too.
  /// </summary>
  public (string Name, string Light, string Medium, string Dark) ChosenColour
  {
    get
    {
      if (UseYellow) return COLOUR_FAMILIES[1];
      if (UseOrange) return COLOUR_FAMILIES[2];
      if (UseRed) return COLOUR_FAMILIES[3];
      if (UsePurple) return COLOUR_FAMILIES[4];
      if (UseGreen) return COLOUR_FAMILIES[5];
      if (UseBlue) return COLOUR_FAMILIES[6];
      if (UseGrey) return COLOUR_FAMILIES[7];
      return COLOUR_FAMILIES[0];
    }
  }

  [JsonPropertyName("enableHelpTexts")]
  public bool EnableHelpTexts { get; set; } = true;

  /// <summary>
  /// The home menu's tiles, in the order the viewer put them there. The shipped order is the reference's
  /// thirteen - the same workflow actions the home menu names its tiles by (B2's mapping) - and an empty
  /// or missing file answers with it, as the reference's getter did.
  /// </summary>
  [JsonPropertyName("homeMenuActionIds")]
  public Guid[] HomeMenuActionIds
  {
    get
    {
      Guid[] actionIds = _homeMenuActionIds ?? Array.Empty<Guid>();
      if (actionIds.Length == 0)
        _homeMenuActionIds = actionIds = DefaultHomeMenuActionIds;
      return actionIds;
    }
    set => _homeMenuActionIds = value;
  }

  private Guid[]? _homeMenuActionIds;

  /// <summary>
  /// The media lists the home menu's tiles pull their previews from, tile by tile; the reference defaulted
  /// to none and its home menu editor fills them in.
  /// </summary>
  [JsonPropertyName("homeMenuActionMediaLists")]
  public string[] HomeMenuActionMediaLists
  {
    get => _homeMenuMediaLists ?? Array.Empty<string>();
    set => _homeMenuMediaLists = value;
  }

  private string[]? _homeMenuMediaLists;

  [JsonPropertyName("lastSelectedHomeMenuActionId")]
  public string? LastSelectedHomeMenuActionId { get; set; }

  /// <summary>The reference's shipped thirteen, in its order, with its comments kept.</summary>
  public static readonly Guid[] DefaultHomeMenuActionIds =
  {
    new Guid("30715d73-4205-417f-80aa-e82f0834171f"), // Audio
    new Guid("80d2e2cc-baaa-4750-807b-f37714153751"), // Movies
    new Guid("30f57cba-459c-4202-a587-09fff5098251"), // Series
    new Guid("a4df2df6-8d66-479a-9930-d7106525eb07"), // Videos
    new Guid("c33E39cc-910e-41c8-bffd-9eccd340b569"), // OnlineVideos
    new Guid("55556593-9fe9-436c-a3b6-a971e10c9d44"), // Images
    new Guid("b4a9199f-6dd4-4bda-a077-de9c081f7703"), // TV
    new Guid("e3bbc989-99db-40e9-a15f-ccb50b17a4c8"), // Radio
    new Guid("bb49a591-7705-408f-8177-45d633fdfad0"), // News
    new Guid("e34fdb62-1f3e-4aa9-8a61-d143e0af77b5"), // Weather
    new Guid("a24958e2-538a-455e-a1db-a7bb241aF7ec"), // (the reference left this one unnamed)
    new Guid("2ded75c0-5eae-4e69-9913-6b50a9ab2956"), // WebRadio
    new Guid("873EB147-C998-4632-8F86-D5E24062BE2E")  // App Launcher
  };
}
