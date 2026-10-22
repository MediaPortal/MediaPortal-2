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
*/

using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MediaPortal.Frontend.ViewModels
{
  /// <summary>
  /// One line of the home screen's main menu, together with what pressing it can honestly do.
  /// </summary>
  /// <remarks>
  /// <para>
  /// An entry exists whether or not anything can back it. The old client's home screen was built from the actions
  /// the installed plugins contributed, so an entry was simply absent when nothing contributed it; a front end that
  /// does the same hides the difference between "this media centre has no television" and "this media centre has
  /// television and this server holds none of it", and those two need different next moves from whoever is standing
  /// in front of the set. So an entry that cannot be opened says why, in the tile's own small print.
  /// </para>
  /// <para>
  /// <see cref="Section"/> is the whole of the difference between an openable entry and one that is not: it is the
  /// part of the library the application knows how to list, filled in by <see cref="HomeViewModel"/>, which is the
  /// only place that knows whether the server currently answers for that aspect.
  /// </para>
  /// </remarks>
  public sealed partial class MainMenuEntry : ObservableObject
  {
    /// <param name="mp2Action">
    /// The name MP2's own workflow files give the contribution this entry is - <c>"Home->Movies"</c> - or null for an
    /// entry the old client had no line for. The home-menu tests read the workflow XML of this tree and compare
    /// these names, so that the menu here cannot drift away from the menu the previous code assembled.
    /// </param>
    /// <param name="labelKey">
    /// The language resource MP2's label comes from - <c>[Media.MoviesMenuItem]</c> - or null when no plugin of this
    /// tree named this entry. The same tests resolve this key in the plugin's own <c>strings_en.xml</c> and require
    /// <see cref="Label"/> to be the string that is written there.
    /// </param>
    /// <param name="label">What the tile says.</param>
    /// <param name="aspects">
    /// The aspects whose titles pressing opens, in the order they are preferred, or empty for an entry with nothing
    /// behind it. More than one because one of MP2's pillars is filed two ways: the music library is a shelf of
    /// albums when the importer knew about albums and a flat shelf of tracks when it did not, and the tile should
    /// open whichever this server actually has rather than the one that happens to be named first here.
    /// </param>
    /// <param name="action">What pressing does, in the tile's own second line.</param>
    /// <param name="missing">
    /// Why this build cannot open the entry at all - a missing backend, not a missing title - or null when it can.
    /// </param>
    /// <param name="screenKey">
    /// The name of a screen this entry opens rather than a shelf, or null.
    /// </param>
    /// <param name="homeImageId">
    /// The workflow action's own Id, which is the name both rescued skins give this entry's home-screen picture -
    /// <c>images/HomeMenu/&lt;id&gt;.png</c> spells it bare in Nereus and <c>&lt;id&gt;Focus/NoFocus.png</c> in WMC.
    /// See <see cref="HomeImageId"/>.
    /// </param>
    internal MainMenuEntry(string? mp2Action, string? labelKey, string label, string[] aspects, string action,
        string? missing, string? screenKey = null, string? homeImageId = null)
    {
      Mp2Action = mp2Action;
      LabelKey = labelKey;
      Label = label;
      Aspects = aspects;
      Action = action;
      Missing = missing;
      ScreenKey = screenKey;
      HomeImageId = homeImageId;
    }

    /// <summary>
    /// The name of the workflow action MP2 contributes this entry with, or null when the old client had no line for
    /// it. See <see cref="MainMenu"/> for the files these names are taken from.
    /// </summary>
    public string? Mp2Action { get; }

    /// <summary>
    /// MP2's language resource for <see cref="Label"/>, in the brackets the old code writes it in, or null when the
    /// entry is this front end's own.
    /// </summary>
    public string? LabelKey { get; }

    /// <summary>What the tile says, in the same words the rest of the front end uses for that part of the library.</summary>
    public string Label { get; }

    /// <summary>The aspects pressing can list, best first.</summary>
    public string[] Aspects { get; }

    /// <summary>The aspect this entry is asking for, which is the first one it would accept.</summary>
    public string Aspect
    {
      get { return Aspects.Length > 0 ? Aspects[0] : ""; }
    }

    /// <summary>The tile's second line: what pressing it does, or what stops it.</summary>
    public string Action { get; }

    /// <summary>Why this build has nothing behind the entry, or null when it has the aspect.</summary>
    public string? Missing { get; }

    /// <summary>
    /// The name of the <i>screen</i> this entry opens rather than a shelf of titles - <c>"folders"</c> today - or
    /// null when pressing opens titles or nothing.
    /// </summary>
    /// <remarks>
    /// A screen, not a section, because what it shows is not a part of the library: the folders a library is built
    /// from exist before any title does, and there is no aspect to list. The application decides what to build for
    /// a key, which is why the key is a word rather than a factory in the entry itself.
    /// </remarks>
    public string? ScreenKey { get; }

    /// <summary>
    /// Why the server this front end is talking to cannot fill the entry, when the build could. Set by the home
    /// screen, which is the only thing that knows whether a server answered and what it answered with.
    /// </summary>
    public string? Unavailable { get; internal set; }

    /// <summary>
    /// The part of the library this entry opens, once the home screen has read that the server answers for it.
    /// Null means the tile cannot be opened right now.
    /// </summary>
    public HomeSection? Section { get; internal set; }

    /// <summary>What the small print says: the count when there is one, and the reason when there is not.</summary>
    public string Detail
    {
      get
      {
        if (Section != null)
          return Section.CountText;
        if (Missing == null && Unavailable == null && ScreenKey != null)
          // A screen is not a shelf, so it has no count to show; what it is for is the useful small print.
          return Action;
        return Missing ?? Unavailable ?? "not connected yet";
      }
    }

    /// <summary>Whether pressing this entry opens something - titles or a screen.</summary>
    public bool Openable
    {
      get { return Section != null || ScreenKey != null; }
    }

    /// <summary>The first letter, drawn large, for the same reason the tiles above it use it: no icon font.</summary>
    public string Badge
    {
      get { return string.IsNullOrEmpty(Label) ? "?" : Label.Substring(0, 1); }
    }

    /// <summary>
    /// The workflow action's own Id, lower-cased and without braces - which is exactly the name both rescued
    /// skins give this entry's home-screen picture: <c>images/HomeMenu/&lt;id&gt;.png</c> in Nereus, and
    /// <c>images/HomeMenu/&lt;id&gt;Focus.png</c> / <c>&lt;id&gt;NoFocus.png</c> in WMC. Null for an entry this
    /// front end made up, which no skin ever drew.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The Id is not a naming convention somebody could dream up; it is the identity MP2's own home tiles were
    /// painted under, so asking the worn skin for a picture by this name is how the reference's
    /// <c>HomeMenuModel</c> filled its tiles - its dictionary of home content is keyed by these very Ids. The
    /// <c>HomeMenuTileTemplate</c> tests re-read the plugins' workflow XML and demand that this is still the
    /// action's Id, and that the shipped skins still carry a picture of that name.
    /// </para>
    /// <para>
    /// What the entry cannot know - and therefore does not - is the shape each skin spells the name in. That is
    /// the skin's own sentence: a template hands this value to <c>SkinImageConverter</c> with its own format,
    /// Nereus <c>HomeMenu/{0}</c> and WMC <c>HomeMenu/{0}Focus</c>. A tile whose skin has no such picture says
    /// so by drawing no picture, which is why <see cref="Badge"/> stays on the entry: the letter is what a tile
    /// wears when the skin has nothing to give it.
    /// </para>
    /// </remarks>
    public string? HomeImageId { get; }

    /// <summary>
    /// The tile's picture asked for by its full name in the skin: <c>HomeMenuRound/&lt;id&gt;</c> while Nereus
    /// is switched to the round tiles (<c>EnableCircleMenuImages</c> - the reference drew its home tiles from
    /// that second folder through the same style), and <c>HomeMenu/&lt;id&gt;</c> for every other answer and
    /// every other face. WMC keeps asking with <see cref="HomeImageId"/> and its own format, since its focus
    /// and no-focus spellings belong to the template.
    /// </summary>
    public string? HomeImagePath
    {
      get
      {
        return HomeImageId == null ? null : HomeViewModel.HomeTileFolder + "/" + HomeImageId;
      }
    }

    public bool IsCircleHomeImage
    {
      get { return HomeImagePath?.StartsWith("HomeMenuRound/", StringComparison.Ordinal) == true; }
    }

    public bool IsSquareHomeImage
    {
      get { return !IsCircleHomeImage; }
    }

    private bool _isSelected;

    public bool IsSelected
    {
      get { return _isSelected; }
      internal set
      {
        if (_isSelected == value)
          return;
        _isSelected = value;
        OnPropertyChanged(nameof(IsSelected));
        OnPropertyChanged(nameof(IsSquareSelected));
        OnPropertyChanged(nameof(IsCircleSelected));
      }
    }

    public bool IsSquareSelected
    {
      get { return IsSelected && IsSquareHomeImage; }
    }

    public bool IsCircleSelected
    {
      get { return IsSelected && IsCircleHomeImage; }
    }

    /// <summary>Says the tile's picture may be asked for again - the skin's switches moved underneath it.</summary>
    public void RevealHomeTileFolder()
    {
      OnPropertyChanged(nameof(HomeImagePath));
      OnPropertyChanged(nameof(IsCircleHomeImage));
      OnPropertyChanged(nameof(IsSquareHomeImage));
      OnPropertyChanged(nameof(IsSquareSelected));
      OnPropertyChanged(nameof(IsCircleSelected));
    }

    /// <summary>
    /// A menu entry for one part of a library that MP2's own menu has no line for - a custom aspect somebody's
    /// server reports, or a catalogue entry this list has not caught up with.
    /// </summary>
    /// <remarks>
    /// Not every aspect is offered: only the ones <see cref="HomeSection.For"/> recognises as something to sit down
    /// and watch, so <c>PersonItem</c> and the rest of the schema stay out of the menu either way.
    /// </remarks>
    internal static MainMenuEntry FromSection(HomeSection section)
    {
      MainMenuEntry entry = new MainMenuEntry(null, null, section.Label, new[] { section.Aspect.Name },
          "the titles this server files here", null);
      entry.Section = section;
      return entry;
    }

    /// <summary>
    /// Whether this entry's shelf is what the machine is playing right now. Both rescued skins carried a picture
    /// named exactly <c>IsPlaying</c> for this sentence; the home tile wears it while the aspect behind the entry
    /// is the one a film, an album or a picture show is being read from.
    /// </summary>
    [ObservableProperty]
    private bool _isPlaying;
  }
}