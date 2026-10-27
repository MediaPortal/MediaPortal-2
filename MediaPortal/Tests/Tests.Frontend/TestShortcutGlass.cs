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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using LiquidGlassAvaloniaUI;
using NUnit.Framework;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.Theming;
using MediaPortal.Frontend.ViewModels;
using MediaPortal.Frontend.Views;
using MediaPortal.Frontend.Workflow;
using IconPath = Avalonia.Controls.Shapes.Path;

namespace Tests.Frontend
{
  /// <summary>
  /// The home's shortcut tiles as glass: the icon a tile wears is the skin's own vector, the covers beside it are
  /// untouched, and standing on the glass morphs it - the veil drawing together, the edge brightening, the
  /// outline of the glyph coming round the icon - and only the glass. Nothing travels across the glass: the
  /// standing-on lifts its background lighter and brings up an edge that at rest is barely there. The lensing
  /// itself is not a state: it is on wherever the glass lies, and it bends the screen as it actually stands
  /// behind each individual tile, which is what a surface that samples this window's own visuals guarantees.
  /// </summary>
  /// <remarks>
  /// <para>
  /// These are the screens' own tests rather than the reader's, because the two claims are different. That a
  /// <c>.svg</c> file becomes a shape is <see cref="TestSvgGeometry"/>'s business; what this file holds is that a
  /// tile on the shown home screen is wearing a shape at all - the model's name, the converter, the worn skin's
  /// catalog, the template's binding and the style's fill, all in one assertion with no C# of the test in between.
  /// A template that quietly went back to a PNG, or a name that drifted away from the file on disk, fails here and
  /// says which tile it happened to.
  /// </para>
  /// <para>
  /// The morph is read as the properties the styles set, after the transitions have had their say: the veil's
  /// light and the lightness of the background under it, the brightness of the frame's edge, the rim of light the
  /// glyph's outline carries. Those are the numbers the skin promises, and they are what a viewer sees - a tile
  /// whose numbers never arrived is a tile whose stylesheet says something the screen does not do.
  /// </para>
  /// </remarks>
  [TestFixture]
  public class TestShortcutGlass
  {
    /// <summary>The videos entry, whose five shortcuts are the ones the skin has vectors for.</summary>
    private const string MOVIES_ACTION = "80d2e2cc-baaa-4750-807b-f37714153751";

    /// <summary>The music entry, whose shortcuts are only part written as vectors so far.</summary>
    private const string AUDIO_ACTION = "30715d73-4205-417f-80aa-e82f0834171f";

    /// <summary>
    /// A shortcut tile wears the skin's own glyph as a shape, and the covers beside it wear their plate: the two
    /// kinds of tile are apart, so dressing the glass in no way touches a cover.
    /// </summary>
    [AvaloniaTest]
    public void AShortcutTileWearsTheSkinsOwnGlyphAndACoverKeepsItsPlate()
    {
      RunOnHome(MOVIES_ACTION, null, delegate (Window window, List<Button> glass, List<Button> plates)
      {
        Assert.That(glass, Has.Count.EqualTo(5),
            "the videos rail is five shortcuts, and all five are glass now");
        Assert.That(glass.Select(tile => ((HomeContentTile) tile.Tag!).Label),
            Is.EquivalentTo(new[] { "Genre", "Year", "Age", "Actor", "Search" }),
            "the reference's shortcut rail, in the reference's order");

        foreach (Button tile in glass)
        {
          string label = ((HomeContentTile) tile.Tag!).Label;
          IconPath glyph = GlassChild(tile, "home-shortcut-glyph").OfType<IconPath>().FirstOrDefault()!;
          Assert.That(glyph, Is.Not.Null, label + "'s glass was written with a glyph, not a picture");
          Assert.That(glyph.Data, Is.Not.Null,
              label + " asked the worn skin for its icon and no shape came back - the name in the model and the "
              + "file in the skin have drifted apart");
          Assert.That(glyph.Fill, Is.Not.Null, label + "'s glyph is coloured by the tile, not by its file");

          HomeContentTile model = (HomeContentTile) tile.Tag!;
          Border face = (Border) GlassChild(tile, "home-tile-edge").Single();

          // The glyph stands centred on the glass with nothing around it - no plaque, no box, no view box. A tile
          // whose size the screen already decides needs no wrapper to keep its icon square.
          Assert.That(glyph.Width, Is.EqualTo(model.IconSize), label + "'s icon is sized off the tile itself");
          Assert.That(model.IconSize, Is.GreaterThanOrEqualTo(model.Width * 0.30),
              label + "'s icon is the tile's subject rather than a stamp on it");
          Assert.That(model.IconSize, Is.LessThanOrEqualTo(model.Width * 0.35),
              label + "'s icon stands back from its old size - the name under it shares the glass");
          Assert.That(glyph.Margin.Bottom, Is.GreaterThan(0.0),
              label + "'s icon is lifted off the dead centre so it stands above its name, not over it");
          Assert.That(tile.GetVisualDescendants().OfType<Viewbox>(), Is.Empty,
              label + "'s icon hangs on the glass, not in a box of its own");
          // The glyph stands beside the glass, not inside it: written into a surface it would have to be written
          // once per surface, and a glyph written twice is two glyphs that drift apart the next time one is
          // edited. The frame only lies over the glass's edge, so it is not the glass either and the glyph is no
          // more its child than the veil is.
          Assert.That(glyph.GetVisualParent(),
              Is.SameAs(GlassChild(tile, "home-shortcut-liquid").Single().GetVisualParent()),
              label + "'s glyph is a sibling of the glass in the tile's own Panel, painted after it");
          Assert.That(glyph.GetVisualAncestors().Contains(GlassChild(tile, "home-shortcut-liquid").Single()),
              Is.False,
              label + "'s glyph hangs inside the glass, which owns its colour and cuts it to its own path");
          Assert.That(GlassChild(tile, "home-shortcut-plaque"), Is.Empty,
              label + " wears its glyph on the glass; there is no plate under it");

          // The name stands on the glass too. A band is a rectangle, and a rectangle inside a rounded frame lies
          // across the corners of that frame, which is what a band on this tile used to do.
          Assert.That(GlassChild(tile, "home-tile-caption"), Is.Empty,
              label + " has no band lying across the bottom of its frame");
          TextBlock caption = GlassChild(tile, "home-shortcut-caption").OfType<TextBlock>().FirstOrDefault()!;
          Assert.That(caption, Is.Not.Null, label + "'s name still stands on the glass");
          Assert.That(caption.Text, Is.EqualTo(label));
          Assert.That(caption.GetVisualParent(), Is.SameAs(glyph.GetVisualParent()),
              "and it hangs on the glass directly, not on a plate of its own");

          // The surfaces the glass is made of, so that a template that dropped one of them says which.
          Assert.That(GlassChild(tile, "home-shortcut-ground"), Is.Empty,
              label + " wears no second ground of its own behind the glass");
          LiquidGlassSurface lens = (LiquidGlassSurface) GlassChild(tile, "home-shortcut-liquid").Single();
          // The shadow belongs to the tile and is the one the skin knows: the same Border.home-tile-shadow the
          // covers stand wrapped in, styled once in styles.xaml for the whole wall. The glass casting its own was
          // a second shadow under one tile - a darkness none of the shortcuts' neighbours had, and the reason the
          // rail read as five tiles sunk into holes.
          Border? standing = tile.GetVisualParent() as Border;
          Assert.That(standing, Is.Not.Null, label + " stands in a Border");
          Assert.That(standing!.Classes.Contains("home-tile-shadow"), Is.True,
              label + " is laid on the wall by the very shadow Border a cover is laid on, not by one of the "
              + "glass's own making");
          Assert.That(standing.BoxShadow, Is.Not.Null,
              "and the worn skin really has dressed that Border with its shadow");
          Assert.That(lens.ShadowEnabled, Is.False,
              "while the glass itself casts nothing, so exactly one shadow falls under a shortcut");

          // The one guard here that is about the shader rather than about the shape of the tile. A liquid glass
          // surface is drawn from a walk of this window's own visuals, and that walk leaves a surface's OWN
          // subtree out and no more than that, so a rail of these tiles samples each other's finished glass
          // unless every one of them is put out of the capture by hand. Lensing a neighbour's lens and being
          // lensed by it in turn is a loop that closes on white.
          Assert.That(LiquidGlassBackdrop.GetIsExcludedFromCapture(tile), Is.True,
              label + " stands out of the glass's own backdrop - its neighbours are glass too, and glass "
              + "sampling glass is a rumour each one repeats until it is a shout");

          // Measured rather than guessed: a photograph of the running screen showed the tile with no shadow at all
          // while the glass asked for one, because the button the glass stands in is exactly the tile's size and
          // its theme clips it to that - and a glass shadow lies outside its own surface by design, so the cut
          // threw every pixel of it away. Deepening the shadow cannot survive that; not clipping can.
          Assert.That(tile.ClipToBounds, Is.False,
              label + " gives its glass room to lean over its edge, because that is where its shadow falls");
          Assert.That(GlassChild(tile, "home-shortcut-liquid-deep"), Is.Empty,
              label + "'s lensing belongs to the glass and is not a second state of it");
          Assert.That(GlassChild(tile, "home-shortcut-tint"), Has.Length.EqualTo(1),
              label + " has the veil the glass itself throws back");
          Assert.That(GlassChild(tile, "home-shortcut-sheen"), Is.Empty,
              label + " has nothing travelling across its glass - a reflection swept over the tile is an "
              + "animation doing something to the picture rather than the tile answering the hand on it");

          // And the glass holds no bitmap at all: the icon is a shape, and the glass is a shader that bends the
          // screen itself, so there is no picture for the tile to carry any more.
          Image[] pictures = tile.GetVisualDescendants().OfType<Image>().ToArray();
          Assert.That(pictures, Is.Empty,
              label + "'s glass carries no picture of its own - the lens samples the wall where it stands");

          // And the plate the cover wears is there too, put away: the same template writes both, and the tile's
          // own kind is what decides which of the two a viewer sees.
          // Both kinds of tile are written into the template's own Panel, each wrapped in the shadow Border the
          // skin lays for it, so the cover a shortcut is written beside is one level further out than the button.
          Visual wall = tile.GetVisualAncestors()
              .First(ancestor => ancestor.GetVisualDescendants().OfType<Button>()
                  .Any(button => button.Classes.Contains("nereus-content-tile")));
          Button plate = wall.GetVisualDescendants().OfType<Button>()
              .First(button => button.Classes.Contains("nereus-content-tile"));
          Assert.That(plate.IsEffectivelyVisible, Is.False,
              label + " is a shortcut, so the plate beside it keeps itself out of the way");
        }

        Assert.That(plates, Is.Not.Empty, "the wall beside the shortcuts still has its covers");
        foreach (Button cover in plates)
        {
          Assert.That(cover.IsVisible, Is.True, ((HomeContentTile) cover.Tag!).Label + " is a shown cover");
          Assert.That(cover.CornerRadius.TopLeft, Is.EqualTo(10),
              "a cover keeps the ten of rounding it always had - the glass is the shortcuts' and nobody else's");
          Assert.That(GlassChild(cover, "home-shortcut-glyph"), Is.Empty,
              "and no glyph of the shortcuts' is anywhere inside a cover");
        }
      });
    }

    /// <summary>
    /// A shortcut is laid on the wall by the very shadow a cover is laid on: the same shared
    /// Border.home-tile-shadow, the one BoxShadow the skin writes for the whole wall, the same rounding, and
    /// nothing on the way out to the wall that cuts that shadow off at the tile's own edge - which is what a
    /// shortcut standing without a shadow always turned out to be.
    /// </summary>
    [AvaloniaTest]
    public void AShortcutIsLaidOnTheWallByTheCoversOwnShadow()
    {
      RunOnHome(MOVIES_ACTION, null, delegate (Window window, List<Button> glass, List<Button> plates)
      {
        Border shortcut = (Border) glass[0].GetVisualParent()!;
        Border cover = (Border) plates[0].GetVisualParent()!;

        Assert.That(shortcut.Classes.Contains("home-tile-shadow"), Is.True,
            "a shortcut stands in the shadow Border the wall shares, not in one of the glass's own making");
        Assert.That(cover.Classes.Contains("home-tile-shadow"), Is.True,
            "and that is the very Border a cover stands in");
        Assert.That(shortcut.BoxShadow, Is.EqualTo(cover.BoxShadow),
            "one shadow, written once in the skin for every tile on the wall - the shortcut's was a different "
            + "shadow altogether, and it sat the rail in a darkness none of the covers shared");
        Assert.That(shortcut.CornerRadius.TopLeft, Is.EqualTo(cover.CornerRadius.TopLeft),
            "and the shadow is rounded the same way, so it agrees with the glass's own corner");
        Assert.That(shortcut.Background, Is.Null,
            "and the ground paints that shadow and nothing else - the glass is what fills the tile");

        // The way out has to be free. A shadow lies outside the tile that casts it, and every ancestor that
        // clips to its own bounds throws the part beyond its edge away - a tile then measures as flat as the
        // wall it stands on, however deep the shadow is styled.
        Assert.That(glass[0].ClipToBounds, Is.False, "the button lets its shadow lean over its edge");
        ItemsControl? rail = glass[0].GetVisualAncestors().OfType<ItemsControl>().FirstOrDefault();
        Assert.That(rail, Is.Not.Null, "the shortcuts stand in a rail");
        Assert.That(rail!.ClipToBounds, Is.False, "and the rail lets them lean over ITS edge too");
      });
    }

    /// <summary>
    /// Standing on the glass morphs it: its background comes UP, the edge that at rest is barely there comes up to
    /// full light, and the outline of the glyph draws itself round the icon while the icon lifts off the glass.
    /// Nothing travels across the glass, the lensing stays as it was, the corner stays the corner and nothing
    /// grows - and nothing of the sort happens to a cover standing beside the glass.
    /// </summary>
    [AvaloniaTest]
    public void StandingOnTheGlassMorphsIt()
    {
      RunOnHome(MOVIES_ACTION, null, delegate (Window window, List<Button> glass, List<Button> plates)
      {
        Button tile = glass[0];
        Border face = (Border) GlassChild(tile, "home-tile-edge").Single();
        Border tint = (Border) GlassChild(tile, "home-shortcut-tint").Single();
        LiquidGlassSurface lens = (LiquidGlassSurface) GlassChild(tile, "home-shortcut-liquid").Single();
        IconPath glyph = (IconPath) GlassChild(tile, "home-shortcut-glyph").Single();

        Assert.That(tile.CornerRadius.TopLeft, Is.EqualTo(10), "the glass rests at ten of corner");
        Assert.That(face.CornerRadius.TopLeft, Is.EqualTo(10), "and so does the frame inside it");
        Assert.That(lens.BlurRadius, Is.GreaterThan(0),
            "the glass takes the screen behind it out of focus wherever it lies - not a tint, and not a state either");
        // One edge, and one circle. The frame is a ring laid OVER the glass at the glass's own corner rather than
        // a frame wrapped around it: two circles three of thickness apart are the step that used to sit in every
        // corner of these tiles, and no clip moved since ever removed it because the step was never a clip.
        // Two of thickness, measured: at three, a photograph of the running screen showed a white outline drawn
        // round each tile instead of an edge catching the light, which is the difference between glass and a card
        // with a border on it.
        Assert.That(face.BorderThickness.Left, Is.EqualTo(2), "the frame is a thin edge, not a border");
        Assert.That(lens.CornerRadius.TopLeft, Is.EqualTo(face.CornerRadius.TopLeft),
            "and the glass rounds itself to the frame's corner, so a corner is cut once and not twice");
        Assert.That(face.GetVisualParent(), Is.SameAs(lens.GetVisualParent()),
            "and the frame lies beside the glass, over its edge, rather than around it");
        Assert.That(lens.ClipToBounds, Is.True,
            "and the lens is cut to its own tile: a lens paints past its edge by design, and five of these "
            + "twelve pixels apart came out of a live run as one plate over the whole rail. The shadow the tile"
            + " stands on is not the lens's business - it falls from the skin's own shadow Border, outside.");
        Assert.That(lens.ShadowEnabled, Is.False,
            "and the glass casts no shadow of its own - a shortcut is laid on the wall by the skin's one shadow"
            + " Border, the very one every cover on the wall wears, not by a second one of the shader's making");
        Assert.That(lens.Opacity, Is.EqualTo(1),
            "and the glass covers the tile outright - a half transparent glass is a smudge over the sharp wall");
        Assert.That(tint.Opacity, Is.LessThanOrEqualTo(0.9),
            "and the glass lies there with its veil loose - it is the standing-on that pulls it down to full");
        Assert.That(glyph.Opacity, Is.LessThan(1), "and its glyph is lit a little under full");
        Assert.That(Lit(glyph.Stroke), Is.False,
            "and the rim of light the glyph wears is parked out of sight at rest");
        Assert.That(glyph.Stroke, Is.Not.Null, "and there is an outline to light at all");
        ITransform? restingLensTransform = lens.RenderTransform;
        ITransform? restingGlyphTransform = glyph.RenderTransform;
        ITransform? restingTransform = tile.RenderTransform;
        double restingVeil = VeilLightness(tint.Background);
        double restingEdge = BrightestEdge(face.BorderBrush);
        Assert.That(restingEdge, Is.LessThanOrEqualTo(0x30),
            "and the frame is barely there while nothing stands on it - the edge is there to say which tile a "
            + "viewer is standing on, not to draw a ring round all five");

        Assert.That(tile.Focus(), Is.True, "the test has to be able to stand on the tile");
        // The morph is waited for rather than timed: a headless screen advances its animations at a small
        // fraction of the wall clock, so the last frame of a six-tenths transition is not something a pumped
        // screen ever reaches. What this can prove is that the numbers the skin writes ARE written, that they
        // travel the way the styles say they travel, and that the skin registered an animation for them at all.
        PumpUntil(delegate
        {
          return Lit(glyph.Stroke) && tint.Opacity > 0.95 && VeilLightness(tint.Background) > restingVeil;
        }, 12000);
        Assert.That(window.GetVisualDescendants().Contains(tile), Is.True,
            "standing on the glass threw its own visual away; the morph cannot be seen on a tile that is gone");

        Assert.That(tint.Opacity, Is.GreaterThan(0.95), "standing on the glass draws its veil together");
        Assert.That(VeilLightness(tint.Background), Is.GreaterThan(restingVeil),
            "and the background of the glass comes UP - the tile a viewer stands on is the LIGHTER one, which is"
            + " what tells them where they are before they have read a single name");
        Assert.That(BrightestEdge(face.BorderBrush), Is.GreaterThanOrEqualTo(0xB0),
            "and the edge that was a ghost at rest is up to full light, so the lit tile is ringed in light");
        Assert.That(lens.BlurRadius, Is.GreaterThan(0),
            "and still out of focus behind it - standing on glass does not wipe its lensing away");
        Assert.That(lens.Opacity, Is.EqualTo(1), "and still covering the tile outright");
        Assert.That(lens.RenderTransform, Is.SameAs(restingLensTransform),
            "and standing on the glass does not move the lensing either");
        Assert.That(tile.CornerRadius.TopLeft, Is.EqualTo(10),
            "and the corner is the same corner it rested at - glass does not change its own shape");
        Assert.That(face.CornerRadius.TopLeft, Is.EqualTo(10), "and neither does the frame inside it");
        Assert.That(glyph.Opacity, Is.GreaterThan(0.95),
            "and the glyph comes up to the screen's full white");
        Assert.That(Lit(glyph.Stroke), Is.True,
            "and the rim of the glyph has lit up, which is what the icon does when something stands on it");
        Assert.That(glyph.RenderTransform, Is.Not.SameAs(restingGlyphTransform)
                .And.Not.EqualTo(restingGlyphTransform),
            "and the icon lifts off the glass - the glyph moves, and it is the glyph that moves");
        Assert.That(GlassChild(tile, "home-shortcut-sheen"), Is.Empty,
            "and nothing travels across the glass - the tile answers with its own edge, background and glyph, not"
            + " with a reflection swept over the picture standing behind it");
        Assert.That(tile.RenderTransform, Is.SameAs(restingTransform),
            "and standing on the glass does not zoom it at the viewer - the skin scales nothing");

        // And the morph is a morph and not a jump cut: the skin put an animation on each of the numbers that
        // change, which is the part a headless screen cannot show by waiting for it.
        Assert.That(IsAnimated(tint, "Opacity"), Is.True, "the veil draws together over time");
        Assert.That(IsAnimated(tint, "Background"), Is.True, "and its background lightens rather than cutting");
        Assert.That(IsAnimated(face, "BorderBrush"), Is.False,
            "and the edge is the one thing that does NOT come up over time: a ring that has to be faded up is a "
            + "ring that is missing while it fades, so a viewer walking two tiles in one breath - which is how "
            + "anybody moves along a rail or a wall - lands on a tile that still looks unfocused.  Nereus hands "
            + "the ring from tile to tile in one step; everything else above may keep its easing");
        Assert.That(IsAnimated(glyph, "Stroke"), Is.True, "the rim of the glyph is lit, not switched on");
        Assert.That(IsAnimated(glyph, "RenderTransform"), Is.True, "and the icon is eased off the glass");

        plates[0].Focus();
        PumpFor(900);
        Assert.That(plates[0].CornerRadius.TopLeft, Is.EqualTo(10),
            "a cover that something stands on keeps its corner, its plate and its picture to itself");
      });
    }

    /// <summary>
    /// Every shortcut on the music rail wears a drawn glyph now that the icon set is finished, and none of them
    /// goes borrowing the PNG that still lies beside its vector. The second half is the guard that keeps mattering:
    /// the icons are vectors so that they can take the colour and the state of the tile they sit on, and a rail
    /// that quietly fell back to pictures would look right at rest and wrong the moment something stood on it.
    /// </summary>
    [AvaloniaTest]
    public void TheShortcutsOnTheMusicRailWearTheirGlyphsAndBorrowNoPicture()
    {
      RunOnHome(AUDIO_ACTION, null, delegate (Window window, List<Button> glass, List<Button> plates)
      {
        Dictionary<string, bool> worn = new Dictionary<string, bool>();
        foreach (Button tile in glass)
        {
          string label = ((HomeContentTile) tile.Tag!).Label;
          IconPath? glyph = GlassChild(tile, "home-shortcut-glyph").OfType<IconPath>().FirstOrDefault();
          Assert.That(glyph, Is.Not.Null, label + " is glass, and glass is written with a glyph on it");
          worn[label] = glyph!.Data != null;
          Image[] pictures = tile.GetVisualDescendants().OfType<Image>().ToArray();
          Assert.That(pictures, Is.Empty, label + " did not go borrowing a picture for its icon");
        }

        Assert.That(worn["Year"], Is.True,
            "the year icon the music rail shares with the videos rail is the one the skin already carries");
        Assert.That(worn["Artist"], Is.True,
            "and so is the artist, which arrived as the thin-weight person (weight two hundred) so its line "
            + "can be weighed against the normal-weight one the Actor tile beside it wears");
        Assert.That(worn["Genre"], Is.True,
            "the music rail asks for its own genre picture (MediaMenu/audiogenre) rather than wearing the videos "
            + "rail's, and that drawing exists too - the batch that finished the set did not stop at the names "
            + "the videos rail uses");
        Assert.That(worn["Track"], Is.True, "and the track and album icons are worn as shapes rather than left "
            + "to the picture that lies beside them");
        Assert.That(worn["Album"], Is.True);
      });
    }

    /// <summary>
    /// The home screen with one entry's shortcuts on it, shown, and the two kinds of tile handed over: the glass
    /// the shortcuts wear and the plates the covers beside them wear. Both are taken out of a shown window rather
    /// than built by hand, because the bargain is that the <i>screen</i> wears the skin.
    /// </summary>
    private static void RunOnHome(string actionId, string? because,
        Action<Window, List<Button>, List<Button>> assert)
    {
      Application app = Application.Current!;
      string? previousFolder = SkinEngine.SkinsFolder;
      string previousTheme = FrontendTheme.CurrentName(app);
      SkinEngine.SkinsFolder = Path.Combine(MetadataPath("FrontendSource"), "Skins");
      ShellWindow? window = null;
      try
      {
        Assert.That(FrontendTheme.Apply(app, "nereus", out string? themeProblem), Is.EqualTo("nereus"),
            themeProblem ?? "");
        Assert.That(SkinEngine.Apply(app, "nereus", out string? skinProblem), Is.True, skinProblem ?? "");

        HomeViewModel home = new HomeViewModel();
        MainMenuEntry entry = home.DisplayMenu.Single(item => item.HomeImageId == actionId);
        home.SelectedEntry = entry;
        home.ShowContentAsync(entry).GetAwaiter().GetResult();

        // Without a server to ask, no list brought any titles, so the wall gets the covers the test compares the
        // glass against - the same tiles the real screen builds when a shelf answers.
        HomeContentGroup titles = new HomeContentGroup("Latest added", "poster1", 1500, 2);
        titles.AddTitle(new TitleViewModel(new MediaTitle(Guid.NewGuid(), "Alien", null, 1979, null, null, false)));
        titles.AddTitle(new TitleViewModel(
            new MediaTitle(Guid.NewGuid(), "Blade Runner", null, 1982, null, null, false)));
        home.ContentGroups.Add(titles);

        window = new ShellWindow(new HomeView(home, new WorkflowManager(),
            sectionScreen: delegate (HomeSection section) { return null!; }));
        window.Show();
        Pump();

        ScrollViewer content = window.GetVisualDescendants().OfType<ScrollViewer>()
            .Single(viewer => viewer.Name == "nereusContentPane");
        List<Button> glass = content.GetVisualDescendants().OfType<Button>()
            .Where(button => button.Classes.Contains("nereus-shortcut-tile") && button.IsVisible)
            .OrderBy(button => ((HomeContentTile) button.Tag!).X)
            .ToList();
        List<Button> plates = content.GetVisualDescendants().OfType<Button>()
            .Where(button => button.Classes.Contains("nereus-content-tile") && button.IsVisible
                && ((HomeContentTile) button.Tag!).IsTitle)
            .OrderBy(button => ((HomeContentTile) button.Tag!).X)
            .ToList();

        Assert.That(glass, Is.Not.Empty,
            "no shortcut tile was dressed as glass; because: " + (because ?? "the shown home screen"));
        assert(window, glass, plates);
      }
      finally
      {
        window?.Close();
        SkinEngine.Clear(app);
        SkinEngine.SkinsFolder = previousFolder;
        FrontendTheme.Apply(app, previousTheme, out _);
      }
    }

    /// <summary>The surfaces a glass tile is made of, by the class the skin dresses them with.</summary>
    private static Control[] GlassChild(Button tile, string className)
    {
      return tile.GetVisualDescendants().OfType<Control>()
          .Where(control => control.Classes.Contains(className))
          .ToArray();
    }

    /// <summary>
    /// Whether the worn styles put an animation on a property of a control. A morph is something a viewer sees
    /// happen, and on a headless screen the only honest witness of that is the transition the skin registered -
    /// the frames themselves belong to the television.
    /// </summary>
    private static bool IsAnimated(Visual control, string propertyName)
    {
      if (control.Transitions is not System.Collections.IEnumerable transitions)
        return false;

      foreach (object transition in transitions)
      {
        // The XAML writes the property as a string and the loader turns it into the property itself, so either
        // shape can come back depending on how the transition was built.
        object? declared = transition.GetType().GetProperty("Property")?.GetValue(transition);
        string? name = declared as string ?? (declared as AvaloniaProperty)?.Name;
        if (string.Equals(name, propertyName, StringComparison.Ordinal))
          return true;
      }

      return false;
    }

    /// <summary>
    /// Whether a brush is light enough to be seen - which for the glyph's rim means "lit", since the skin parks
    /// it transparent and transitions it up to the text colour when something stands on the tile.
    /// </summary>
    private static bool Lit(IBrush? brush)
    {
      return brush is SolidColorBrush paint && paint.Color.A > 40;
    }

    /// <summary>
    /// The colours a brush paints, in the order it lays them down. A veil is a gradient of four stops and an edge
    /// is a gradient too, so "lighter" and "barely there" have to be read off the stops rather than off the brush.
    /// </summary>
    private static List<Color> Stops(IBrush? brush)
    {
      if (brush is GradientBrush gradient)
        return gradient.GradientStops.Select(stop => stop.Color).ToList();
      if (brush is ISolidColorBrush solid)
        return new List<Color> { solid.Color };
      return new List<Color>();
    }

    /// <summary>
    /// How much light a veil carries: every stop's colour weighted by how opaque that stop is, averaged over the
    /// stops. A transparent stop throws no light wherever it sits, which is why a glass ring dressed in white
    /// measures as almost nothing at rest and as a lit edge when the same stops come up.
    /// </summary>
    private static double VeilLightness(IBrush? brush)
    {
      List<Color> stops = Stops(brush);
      if (stops.Count == 0)
        return -1;
      return stops.Average(color =>
          (double) color.A / 255.0 * (0.299 * color.R + 0.587 * color.G + 0.114 * color.B));
    }

    /// <summary>How bright the brightest point of an edge is, as the alpha the skin wrote for it.</summary>
    private static double BrightestEdge(IBrush? brush)
    {
      List<Color> stops = Stops(brush);
      return stops.Count == 0 ? 0 : stops.Max(color => (double) color.A);
    }

    private static void Pump()
    {
      for (int round = 0; round < 10; round++)
      {
        Dispatcher.UIThread.RunJobs();
        Thread.Sleep(10);
      }
      Dispatcher.UIThread.RunJobs();
    }

    /// <summary>
    /// Let the dispatcher run until a claim has come true, or until the screen has clearly had time to finish
    /// whatever it was doing. Reading a morph halfway through its own animation says nothing about the skin, and
    /// a headless screen does not keep wall-clock time with its animations.
    /// </summary>
    private static void PumpUntil(Func<bool> settled, int milliseconds)
    {
      DateTime until = DateTime.UtcNow.AddMilliseconds(milliseconds);
      while (DateTime.UtcNow < until && !settled())
      {
        Dispatcher.UIThread.RunJobs();
        Thread.Sleep(1);
      }
      Dispatcher.UIThread.RunJobs();
    }

    /// <summary>
    /// Let the dispatcher run for a while, long enough for a transition to have arrived where the styles say it
    /// ends. A morph read halfway through its own animation says nothing about the skin.
    /// </summary>
    private static void PumpFor(int milliseconds)
    {
      DateTime until = DateTime.UtcNow.AddMilliseconds(milliseconds);
      while (DateTime.UtcNow < until)
      {
        Dispatcher.UIThread.RunJobs();
        Thread.Sleep(15);
      }
      Dispatcher.UIThread.RunJobs();
    }

    /// <summary>
    /// A directory the test project was told about at build time - see <c>TestSkinFromDisk</c> for why a test
    /// cannot derive these from the working directory.
    /// </summary>
    private static string MetadataPath(string name)
    {
      AssemblyMetadataAttribute? value = typeof(TestShortcutGlass).Assembly
          .GetCustomAttributes<AssemblyMetadataAttribute>()
          .FirstOrDefault(a => string.Equals(a.Key, name, StringComparison.Ordinal));

      Assert.That(value?.Value, Is.Not.Null.And.Not.Empty,
          "the test project has to hand " + name + " over for this to be checkable");

      string path = Path.GetFullPath(value!.Value!);
      Assert.That(Directory.Exists(path), Is.True, name + " points at '" + path + "', which is not a directory");
      return path;
    }
  }
}
