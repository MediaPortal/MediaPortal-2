// SPDX-License-Identifier: GPL-2.0-or-later
// Copyright (C) 2025 the MediaPortal2 contributors

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Avalonia.Threading;
using MediaPortal.Frontend.Theming;
using MediaPortal.Frontend.ViewModels;
using SkiaSharp;

namespace MediaPortal.Frontend.Views;

/// <summary>
/// The row of covers, drawn.  One control owns the row and paints the covers that answer to it, which is the
/// shape the reference cover flow has and the shape this screen wall never had: until here the covers were
/// containers of a virtualizing list, dressed one by one by a shape that had to guess where each stood.
/// </summary>
/// <remarks>
/// <para>
/// What this takes away, and why that matters more than it looks.  No containers, therefore no indices to lose:
/// a recycled container answers with the measurements and the item of the place it came from - measured here, one
/// standing at 118534 in its own Bounds while it was drawn at 1662 - and a cover dressed from such an answer is a
/// cover that comes out bigger on one side of the stood one than on the other.  No scroller, therefore nothing to
/// fight: the row used to be carried by a ScrollViewer whose own answer to a selection change landed after the
/// travel had been asked for, which is the re-centring jerk a viewer felt at every key.  This control owns one
/// number and nothing else writes it.  And no layout: a step of the caret used to cost a pass over every realized
/// cover with two measures apiece, where a step here costs one walk over the covers a viewer can see.
/// </para>
/// <para>
/// The turn is a camera and not a toolkit transform: the cover is turned about its own upright line and its four
/// corners are projected through a lens standing Focal points in front of the row, onto which the picture is then
/// mapped.  That is the whole of the symmetry - FrameAt is a function of the signed distance alone, so two covers
/// equally far from the stood one are handed equal corners in mirrored places.  Avalonia own Rotate3DTransform
/// was measured on this screen and is NOT mirrored: a two hundred and thirty point cover through ninety points of
/// depth came out a hundred and forty-three points wide at plus forty-five degrees and a hundred and eighty-five
/// at minus forty-five, with an identical M11 and no identical term anywhere the perspective lives.
/// </para>
/// </remarks>
public sealed class CoverCarousel : Control
{
  public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
      AvaloniaProperty.Register<CoverCarousel, IEnumerable?>(nameof(ItemsSource));

  /// <summary>
  /// The cover the row stands on.  Bound two ways to the shelf list, which is what keeps the caret, the details
  /// and the foot line answering to this row without this control having to know they exist.
  /// </summary>
  public static readonly StyledProperty<int> SelectedIndexProperty =
      AvaloniaProperty.Register<CoverCarousel, int>(nameof(SelectedIndex), 0);

  /// <summary>
  /// How many covers are asked for their picture on each hand of the stood one.  What is PAINTED is counted off
  /// the room's width and the row's own step (see <see cref="Render"/>), because a row that paints a fixed count
  /// leaves the last cover of a wide screen unpainted; what is ASKED FOR stays a fixed count, since a poster that
  /// is on its way from the server costs bandwidth whether the television is wide or narrow.
  /// </summary>
  internal const int PAINTED_EACH_SIDE = 9;

  /// <summary>
  /// The band the row stands in.  The covers themselves are smaller than the band, as they are in the picture the
  /// viewer pointed at: a cover that fills its band edge to edge has no room left for the shadow it stands on.
  /// The band is what a cover is measured against, so growing it is the one honest way of asking for bigger
  /// covers: step and lens hang off the cover's own height and grow with it, which leaves the row's proportions
  /// alone and only moves it further into the viewer's face - a quarter nearer, as the viewer counted.
  /// </summary>
  internal const double ROW_HEIGHT = 495.0;

  /// <summary>
  /// How tall a cover stands, as a share of the band the row is measured into, and the proportions belonging to it
  /// - the cover 230:344 and the neighbours 160 apart.  These are the proportions of the picture the viewer
  /// pointed at, and they are the proportions a poster is drawn into: a narrower cover stretches every picture
  /// horizontally, which is what the viewer called a deformation rather than a shape.
  /// </summary>
  internal const double ROW_SHARE = 0.85;
  internal const double COVER_RATIO = 230.0 / 344.0;

  /// <summary>
  /// How far the neighbours stand apart, as a share of a cover's own height - a little under a third, which is
  /// half the width of a cover: the covers of the row lie one behind the other rather than standing side by side,
  /// and the row of the picture the viewer pointed at runs to about fourteen of them across the screen.  Step and
  /// height were tied together for a while, which made the step grow with every request for a bigger cover and
  /// left eight covers in a room built for fourteen.
  /// </summary>
  internal const double PITCH_RATIO = 0.30;

  /// <summary>
  /// How far in front of the row the lens stands, relative to a cover's height, as the reference camera does.
  /// This is what makes a cover turned forty degrees show its far edge noticeably smaller than its near one; a
  /// lens standing further off leaves the same angle in the numbers but reads as a shear rather than a turn.
  /// </summary>
  internal const double FOCAL_RATIO = 900.0 / 344.0;

  internal const double LEAN_PER_PLACE = 14.0;
  internal const double LEAN_CEILING = 55.0;
  internal const double SHRINK_PER_PLACE = 0.06;
  internal const double SHRINK_CEILING = 0.40;
  internal const double DARKEN_PER_PLACE = 0.08;

  /// <summary>
  /// How many places away from the cover being stood on the row begins to lose light.  Inside that many places a
  /// cover stands at full light, which is what the middle of a cover flow is for: the viewer came for the picture
  /// he is standing on and for its immediate neighbours, and a row that greys out from the first neighbour reads as
  /// a row going out of focus rather than as a row going out of the room.  Beyond this many places the light falls
  /// by <see cref="DARKEN_PER_PLACE"/> for every further place, so the number is where the fade BEGINS and the
  /// other number is how steep it is once begun.
  /// </summary>
  internal const double FADE_FROM = 3.0;

  internal const double LIGHT_FLOOR = 0.3;

  /// <summary>How round a cover's corners are, as a part of its own width; the turn takes the round with it.</summary>
  internal const double CORNER_RATIO = 0.045;

  /// <summary>
  /// How much wider a poster is read than the cover it will be painted into.  The slide carries a cover through
  /// sizes as it comes round, and a poster read exactly at the standing width would already be too small on the
  /// way in and on a screen whose scaling is not one; a quarter more than the widest the row paints is what the
  /// slide needs and still a twentieth of the pixels a library's scan holds.
  /// </summary>
  internal const double PAINTED_ROOM_FOR_SLIDE = 1.25;

  /// <summary>
  /// The narrowest and widest poster the row will read.  The narrowest is the width a small tile is drawn from
  /// elsewhere in this front end, so a poster that has to be read again for the row costs nothing new; the widest
  /// is the width a wall tile is read at, which is the largest picture this front end remembers anywhere - a
  /// cover painted wider than that would be a cover the row was never measured for.
  /// </summary>
  internal const int POSTER_NARROWEST = 256;

  internal const int POSTER_WIDEST = 640;

  static readonly SKColor FRAME_STOOD = new SKColor(245, 245, 255, 220);
  static readonly SKColor COVER_WAITING = new SKColor(65, 65, 72);

  /// <summary>How many readings of the light the resting ring is worked out for.</summary>
  const int RING_LIGHT_STEPS = 9;

  /// <summary>Where the four stops of the resting ring lie down the cover's own upright line.</summary>
  static readonly float[] FRAME_RESTING_STOPS = { 0.0f, 0.35f, 0.72f, 1.0f };

  /// <summary>
  /// The ring a cover wears while the viewer does not stand on it: the resting edge of the wall's covers
  /// (styles.xaml, <c>Border.home-tile-edge</c>), the light above the row catching the top of the plate and dying
  /// away down its sides, so that a row of turned covers reads as plates standing in the room rather than as
  /// pictures floating over the television.  It is worked out once for nine readings of the light a cover's place
  /// leaves it, so that a travelling row asks for no colour while it moves.
  /// </summary>
  static readonly SKColor[][] FRAME_RESTING = RestingRingAtEveryLight();

  static SKColor[][] RestingRingAtEveryLight()
  {
    SKColor[] stops =
    {
      new SKColor(255, 255, 255, 0x21),
      new SKColor(255, 255, 255, 0x0D),
      new SKColor(255, 255, 255, 0x08),
      new SKColor(255, 255, 255, 0x16)
    };

    SKColor[][] steps = new SKColor[RING_LIGHT_STEPS + 1][];
    for (int step = 0; step <= RING_LIGHT_STEPS; ++step)
    {
      double dim = step / (double) RING_LIGHT_STEPS;
      SKColor[] at_light = new SKColor[stops.Length];
      for (int stop = 0; stop < stops.Length; ++stop)
        at_light[stop] = stops[stop].WithAlpha((byte) Math.Round(stops[stop].Alpha * dim));
      steps[step] = at_light;
    }

    return steps;
  }

  /// <summary>
  /// The watched corner's numbers, which are the ones the tiles' templates give it: a disc of thirty-eight with
  /// the eye of twenty-eight centred in it, standing six from the cover's top and right edge.  The drawn row
  /// wears no template, so it says the same numbers here.
  /// </summary>
  internal const double MARK_DISC = 38.0;
  internal const double MARK_EYE = 28.0;
  internal const double MARK_MARGIN = 6.0;

  /// <summary>The corner's ground and light: the skin's plate darkening, and its text colour at the nine tenths
  /// the templates give the eye.</summary>
  static readonly SKColor MARK_DISC_GROUND = new SKColor(0, 0, 0, 0x99);
  static readonly SKColor MARK_EYE_LIGHT = new SKColor(255, 255, 255, 230);

  /// <summary>The partly-watched eye, which the skin paints as a light running across it: nothing at a quarter
  /// of its way, whole white at six sevenths.</summary>
  static readonly float[] MARK_PARTLY_STOPS = { 0.25f, 0.85f };

  /// <summary>The icon the corner wears, asked of the worn skin the way a template asks for it.</summary>
  internal const string MARK_ICON = "watched";

  private readonly List<object> _items = new List<object>();
  private readonly Dictionary<object, SKImage?> _pictures = new Dictionary<object, SKImage?>();

  private readonly LinkedList<object> _remembered = new LinkedList<object>();

  private readonly HashSet<object> _askedAbout = new HashSet<object>();

  /// <summary>The posters whose bytes are being read right now, so that one poster is never read twice.</summary>
  private readonly HashSet<object> _being_read = new HashSet<object>();
  private readonly object _picturesLock = new object();

  /// <summary>
  /// The watched corner's eye, read off the worn skin the first time the row is painted (see
  /// <see cref="EyeGlyph"/>).  Once per row, because a skin comes onto the front end together with the screens
  /// that read it; a skin taken off again takes this control with it.
  /// </summary>
  private SKPath? _glyph;

  private bool _glyph_read;

  private DispatcherTimer? _spring;
  private INotifyCollectionChanged? _watched;
  private double _position;
  private double _velocity;
  private bool _dragging;
  private Point _drag_from;
  private int _drag_index;
  private int _detach_pass;

  static CoverCarousel()
  {
    AffectsRender<CoverCarousel>(ItemsSourceProperty, SelectedIndexProperty);
  }

  public CoverCarousel()
  {
    ClipToBounds = false;
    Focusable = true;
  }

  public IEnumerable? ItemsSource
  {
    get { return GetValue(ItemsSourceProperty); }
    set { SetValue(ItemsSourceProperty, value); }
  }

  public int SelectedIndex
  {
    get { return GetValue(SelectedIndexProperty); }
    set { SetValue(SelectedIndexProperty, value); }
  }

  /// <summary>
  /// Who gets told that a cover standing within the viewer reach wants its picture.  Asking the server is the
  /// view model business; this control business ends at painting what the row answers.
  /// </summary>
  public Action<object>? PictureAsked { get; set; }

  /// <summary>
  /// The covers this row has asked a picture for so far - the covers whose frames this row itself laid. The
  /// wall's law ("ask a picture of no title you have not laid a frame for") is measured against the titles, and
  /// a drawn cover is a frame the screen laid just as much as a container of the list is.
  /// </summary>
  internal IReadOnlyCollection<object> CoversAsked
  {
    get { lock (_askedAbout) return new List<object>(_askedAbout); }
  }

  /// <summary>
  /// Where the row stands, counted in covers.  Read by the tests, which want to ask whether the row came to rest
  /// without pressing keys and waiting for a screen a headless test has never drawn.
  /// </summary>
  internal double Position => _position;

  /// <summary>The row's own height, and whatever width it is offered.</summary>
  protected override Size MeasureOverride(Size availableSize)
  {
    return new Size(availableSize.Width, ROW_HEIGHT);
  }

  protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
  {
    base.OnPropertyChanged(change);

    if (change.Property == ItemsSourceProperty)
    {
      Rebuild();
    }
    else if (change.Property == SelectedIndexProperty)
    {
      int clamped = Math.Clamp(SelectedIndex, 0, Math.Max(0, _items.Count - 1));
      if (clamped != SelectedIndex)
      {
        SetCurrentValue(SelectedIndexProperty, clamped);
        return;
      }

      // The row is SPRUNG to the cover rather than shoved there, which is what makes a walk held down against the
      // right key read as a row travelling rather than as a picture being replaced.  A place further away than a
      // viewer could have been looking at is taken without a travel.
      if (Math.Abs(_position - clamped) > PAINTED_EACH_SIDE)
      {
        Services.HomeTrace.Log("row.snap", "from=" + (int) Math.Round(_position) + " to=" + clamped);
        _position = clamped;
        _velocity = 0.0;
      }

      Services.HomeTrace.Log("row.caret", "to=" + clamped + " items=" + _items.Count);
      WatchCoversThatMayAnswer();
      StartSpring();
      InvalidateVisual();
    }
  }

  private void Rebuild()
  {
    SyncItems();
    _position = Math.Clamp(SelectedIndex, 0, Math.Max(0, _items.Count - 1));
    _velocity = 0.0;

    WatchCoversThatMayAnswer();
    InvalidateVisual();
  }

  /// <summary>
  /// Lays the row's own copy of the items out and hangs the watches off it, WITHOUT touching where the row
  /// stands.  Split from <see cref="Rebuild"/> because a wall fills itself in batches while a viewer is already
  /// standing in it, and a batch arriving may not throw the row back onto the cover being stood on - seen as a
  /// re-centring jerk on every step of a walk, because the titles keep arriving during one.
  /// </summary>
  private void SyncItems()
  {
    _items.Clear();
    if (ItemsSource is IEnumerable items)
    {
      foreach (object? item in items)
      {
        if (item != null)
          _items.Add(item);
      }
    }

    if (_watched is INotifyCollectionChanged changed)
      changed.CollectionChanged -= OnItemsChanged;

    _watched = ItemsSource as INotifyCollectionChanged;
    if (_watched != null)
      _watched.CollectionChanged += OnItemsChanged;
  }

  private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
  {
    if (e.Action == NotifyCollectionChangedAction.Reset)
    {
      Rebuild();
      return;
    }

    int stood_on = (int) Math.Round(_position);
    int shift = 0;

    if (e.NewItems != null && e.NewStartingIndex >= 0 && e.NewStartingIndex < stood_on)
      shift += e.NewItems.Count;
    if (e.OldItems != null && e.OldStartingIndex >= 0 && e.OldStartingIndex < stood_on)
      shift -= e.OldItems.Count;

    SyncItems();

    // Covers handed in BEHIND the viewer move the cover being stood on along by their number; covers handed in
    // ahead of them leave the row exactly where it is.  Either way the row keeps its own travel and its own speed,
    // which is what a viewer sees as the row travelling rather than as the picture jumping back to the middle.
    _position += shift;

    WatchCoversThatMayAnswer();
    InvalidateVisual();
  }

  // ---- The pictures --------------------------------------------------------------------

  /// <summary>
  /// Asks for the posters of the covers a viewer could be looking at and watches them for the answer.  The bytes
  /// come from the shelf own artwork source, are decoded off the thread that draws, and the row is woken when one
  /// arrives - which is the same lesson every other wall of this front end learned: a poster is a JPEG, and a row
  /// that decodes its own posters between two frames is a row that catches on every step.
  /// </summary>
  private void WatchCoversThatMayAnswer()
  {
    int middle = (int) Math.Round(_position);

    // Asked from where the eye stands and outwards, never from the left edge of the row inwards.  Only four
    // posters are on the wire at a time and a question already on its way is not overtaken, so the order this
    // loop walks IS the order in which the row is answered.  Walking the places from the left was measured at
    // fourteen posters asked per step with the cover stood on asked eleventh of them - which is what a viewer
    // sees as the middle of the row lagging behind the walk while the covers beside it fill in.
    for (int stride = 0; stride <= PAINTED_EACH_SIDE + 1; ++stride)
    {
      for (int side = 0; side <= 1; ++side)
      {
        // The cover stood on is asked alone; every stride further is asked behind and before it, in that order.
        if (stride == 0 && side == 1)
          continue;

        int place = side == 0 ? middle - stride : middle + stride;
        if (place < 0 || place >= _items.Count)
          continue;

        object item = _items[place];

        if (_askedAbout.Add(item) && item is INotifyPropertyChanged row)
          row.PropertyChanged += OnCoverAnswered;

        PictureAsked?.Invoke(item);
        DecodeWhenBytesArrive(item);
      }
    }
  }

  private void OnCoverAnswered(object? sender, PropertyChangedEventArgs e)
  {
    if (sender == null || !string.Equals(e.PropertyName, nameof(TitleViewModel.Artwork), StringComparison.Ordinal))
      return;

    DecodeWhenBytesArrive(sender);
  }

  private void DecodeWhenBytesArrive(object item)
  {
    if (item is not TitleViewModel title)
      return;

    byte[]? bytes = title.Artwork;
    if (bytes == null || bytes.Length == 0)
      return;

    int asked;

    lock (_picturesLock)
    {
      if (_pictures.ContainsKey(title))
        return;

      // ONE read per poster.  The row asks again every frame it is on its way, and two reads of the same poster
      // ending one after the other means the second one throws the first away - while the thread that draws may
      // be standing inside it.  Measured as this front end dying with an access violation inside
      // sk_image_new_from_bitmap, which is what reading a poster that was just freed is called in Skia.
      if (!_being_read.Add(title))
        return;

      asked = Volatile.Read(ref _detach_pass);
    }

    byte[] toRead = bytes;

    //  Asked HERE, on the thread that owns the bounds: the width a cover is painted at is a question about the
    //  screen, and a thread that fetches has no business reading the layout.
    int across = PaintedCoverWidth();

    Task.Run(delegate
    {
      SKImage? picture = ReadPosterForPainting(toRead, across);

      Dispatcher.UIThread.Post(delegate
      {
        lock (_picturesLock)
          _being_read.Remove(title);

        if (asked != Volatile.Read(ref _detach_pass))
        {
          picture?.Dispose();
          return;
        }

        Remember(title, picture);
        InvalidateVisual();
      });
    });
  }

  /// <summary>
  /// How wide a cover stands on the screen this row is painted in, with room left for a scale.
  /// </summary>
  /// <remarks>
  /// The standing cover is the widest thing the row draws, and it stands at the band's height times the cover's
  /// own ratio; nothing further along the row is wider, since the lens only ever throws a cover smaller.  Asked
  /// of the bounds rather than of a constant because the row is measured in the window's own units and a screen
  /// with a scaling paints those units with more pixels.
  /// </remarks>
  private int PaintedCoverWidth()
  {
    double across = Bounds.Height * ROW_SHARE * COVER_RATIO * (TopLevel.GetTopLevel(this)?.RenderScaling ?? 1.0);
    return Math.Clamp((int) Math.Ceiling(across * PAINTED_ROOM_FOR_SLIDE), POSTER_NARROWEST, POSTER_WIDEST);
  }

  /// <summary>
  /// The poster these bytes hold, read no wider than the row can paint it.
  /// </summary>
  /// <remarks>
  /// <para>
  /// A library hands out scans of a thousand and of two thousand pixels while the widest cover the row paints
  /// stands at about three hundred.  Measured over one walk of a shelf: the sixty-two posters the row remembered
  /// averaged two point EIGHT megapixels each and amounted to six hundred and ninety megabytes of pixels, and
  /// every frame of the slide sampled that lot down to the covers the viewer actually sees.  Reading the whole
  /// scan therefore paid for the read three times over - once in the decoder, once in the memory it stands in,
  /// and once on every frame it is drawn from.
  /// </para>
  /// <para>
  /// So the small picture is asked of the decoder DIRECTLY rather than read large and shrunk afterwards: for a
  /// JPEG that is the decoder's own trick of reading every other block of the image, which costs a fraction of
  /// the reading instead of all of it.  A poster already narrower than the painted width is taken as it came,
  /// since scaling upwards was never what a question about width meant, and bytes no codec will open still go
  /// through the plain door, which knows more formats than this one.
  /// </para>
  /// </remarks>
  private static SKImage? ReadPosterForPainting(byte[] bytes, int across)
  {
    try
    {
      //  The same ladder the tiles and backdrops are read at (ArtworkConverter.ReadScanAtLadder): a two-thousand
      //  wide scan costs eleven milliseconds at a quarter and forty-two whole, and the row reads dozens of them a
      //  second while a viewer walks its shelf.
      using SKBitmap? rung = ArtworkConverter.ReadScanAtLadder(bytes, across);

      if (rung == null)
        return ReadWholePoster(bytes);

      return SKImage.FromBitmap(rung);

      //  A poster the small door would not open still goes through the big one: a cover read late and large is
      //  worth more to the screen than a cover that stays a grey placeholder.
    }
    catch (Exception)
    {
      return null;
    }
  }

  /// <summary>
  /// The poster at the size the file itself holds, for the pictures no small reading applies to - a poster
  /// already narrower than the cover, a format the scaled door does not know, a decoder that refused the sheet.
  /// </summary>
  private static SKImage? ReadWholePoster(byte[] bytes)
  {
    SKBitmap? whole = SKBitmap.Decode(bytes);
    if (whole == null)
      return null;

    using (whole)
      return SKImage.FromBitmap(whole);
  }

  /// <summary>Holds one poster and drops the oldest of the ninety-six the row remembers.</summary>
  private void Remember(TitleViewModel title, SKImage? picture)
  {
    lock (_picturesLock)
    {
      if (_pictures.TryGetValue(title, out SKImage? old))
      {
        old?.Dispose();
        _pictures.Remove(title);
        _remembered.Remove(title);
      }

      _pictures.Add(title, picture);
      _remembered.AddFirst(title);

      Services.HomeTrace.Log("row.pic",
          "w=" + (picture?.Width ?? 0) + " h=" + (picture?.Height ?? 0) + " held=" + _remembered.Count);

      while (_remembered.Count > 96)
      {
        LinkedListNode<object>? oldest = _remembered.Last;
        if (oldest == null)
          break;

        _remembered.RemoveLast();

        if (_pictures.TryGetValue(oldest.Value, out SKImage? dropped))
        {
          dropped?.Dispose();
          _pictures.Remove(oldest.Value);
        }

        if (oldest.Value is INotifyPropertyChanged row)
          row.PropertyChanged -= OnCoverAnswered;

        _askedAbout.Remove(oldest.Value);
      }
    }
  }

  // ---- The row own motion --------------------------------------------------------------

  /// <summary>
  /// How hard a cover that stands a place away is pulled towards its place, every tick.
  /// </summary>
  internal const double GLIDE_PULL = 0.4444;

  /// <summary>
  /// How much of the row's travel one tick hands to the next.  The two numbers are not free: a spring carried
  /// along like this rocks past its target whenever the pull is stronger than this hold allows, and the pair
  /// that was here (a pull of .16 on a hold of .76) was such a spring - its roots were complex, so a step of one
  /// cover arrived by sailing past the cover, back over it and past it again, which a viewer standing in front of
  /// the television reads as a row that will not keep still.  A hold of .36 and a pull of .4444 stand in the one
  /// ratio that makes the roots equal - (1 - √hold)² / hold - which is the damping that arrives in a single
  /// unbroken glide: same first stride as the rocking pair (one tick moves a sixth of a cover), no second stride
  /// past the place, and the whole of a step walked in about three hundred milliseconds.
  /// </summary>
  internal const double GLIDE_HELD = 0.36;

  /// <summary>
  /// One tick of the row's glide: where the row stands next and what it carries into the tick after that.
  /// Kept apart from the timer so that the numbers can be asked without a screen, a timer or a cover.
  /// </summary>
  internal static double NextGlideStep(double at, double carrying, double to, out double carries_next)
  {
    double next = (carrying + (to - at) * GLIDE_PULL) * GLIDE_HELD;
    carries_next = next;
    return at + next;
  }

  private void StartSpring()
  {
    _spring ??= new DispatcherTimer(DispatcherPriority.Render)
    {
      Interval = TimeSpan.FromMilliseconds(16)
    };

    _spring.Tick -= OnSpringTick;
    _spring.Tick += OnSpringTick;

    if (!_spring.IsEnabled)
      _spring.Start();
  }

  private void StopSpring()
  {
    _spring?.Stop();
  }

  private void OnSpringTick(object? sender, EventArgs e)
  {
    double to = Math.Clamp(SelectedIndex, 0, Math.Max(0, _items.Count - 1));

    _position = NextGlideStep(_position, _velocity, to, out _velocity);

    if (Math.Abs(to - _position) < 0.002 && Math.Abs(_velocity) < 0.002)
    {
      _position = to;
      _velocity = 0.0;
      StopSpring();
    }

    WatchCoversThatMayAnswer();
    InvalidateVisual();
  }

  // ---- What the hand does --------------------------------------------------------------

  protected override void OnKeyDown(KeyEventArgs e)
  {
    switch (e.Key)
    {
      case Key.Left:
      case Key.Up:
        AskFor(SelectedIndex - 1);
        e.Handled = true;
        break;

      case Key.Right:
      case Key.Down:
        AskFor(SelectedIndex + 1);
        e.Handled = true;
        break;

      case Key.Home:
        AskFor(0);
        e.Handled = true;
        break;

      case Key.End:
        AskFor(Math.Max(0, _items.Count - 1));
        e.Handled = true;
        break;

      default:
        base.OnKeyDown(e);
        break;
    }
  }

  protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
  {
    if (e.Delta.Y == 0.0)
      return;

    AskFor(SelectedIndex + (e.Delta.Y < 0.0 ? 1 : -1));
    e.Handled = true;
  }

  protected override void OnPointerPressed(PointerPressedEventArgs e)
  {
    Focus();

    _dragging = true;
    _drag_from = e.GetPosition(this);
    _drag_index = SelectedIndex;

    e.Pointer.Capture(this);
  }

  protected override void OnPointerMoved(PointerEventArgs e)
  {
    if (!_dragging)
      return;

    Point now = e.GetPosition(this);

    double step = Bounds.Height * ROW_SHARE * PITCH_RATIO;
    _position = Math.Clamp(
        _drag_index - (now.X - _drag_from.X) / Math.Max(1.0, step), 0, Math.Max(0, _items.Count - 1));

    int nearest = (int) Math.Round(_position);
    if (nearest != SelectedIndex)
      SetCurrentValue(SelectedIndexProperty, nearest);

    WatchCoversThatMayAnswer();
    InvalidateVisual();
  }

  protected override void OnPointerReleased(PointerReleasedEventArgs e)
  {
    if (!_dragging)
      return;

    _dragging = false;
    e.Pointer.Capture(null);

    AskFor((int) Math.Round(_position));
  }

  private void AskFor(int place)
  {
    int clamped = Math.Clamp(place, 0, Math.Max(0, _items.Count - 1));
    if (clamped == SelectedIndex)
      return;

    Services.HomeTrace.Log("row.key", "from=" + SelectedIndex + " to=" + clamped);
    Services.HomeTrace.NextFrames("rowjump", 4);

    SetCurrentValue(SelectedIndexProperty, clamped);
  }

  /// <summary>
  /// Stands the caret on the first or the last cover of the row, from wherever it stood.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The view hands this out for the far keys of the keyboard.  A list answers those keys by carrying the far
  /// container into view, which is a walk through every container between - and the row is worn over a list of
  /// a thousand titles, where that walk was measured at eleven hundred realised containers and two seconds of
  /// one frame.  Asked of the row, the same key is a number: the place the eye is carried to is painted from
  /// that number, and nothing between the two places is built at all.
  /// </para>
  /// </remarks>
  internal void StandAtEitherEnd(bool at_the_end)
  {
    AskFor(at_the_end ? Math.Max(0, _items.Count - 1) : 0);
  }

  // ---- What the eye sees ---------------------------------------------------------------

  /// <summary>One cover, said as the numbers that draw it rather than as a control.</summary>
  internal readonly struct Frame
  {
    internal Frame(object item, SKPoint middle, double width, double height, double lean, double light,
        bool stood, bool watched = false, bool partly_watched = false)
    {
      Item = item;
      Middle = middle;
      Width = width;
      Height = height;
      Lean = lean;
      Light = light;
      Stood = stood;
      Watched = watched;
      PartlyWatched = partly_watched;
    }

    internal object Item { get; }

    internal SKPoint Middle { get; }

    internal double Width { get; }

    internal double Height { get; }

    /// <summary>The turn about the cover own upright line, in degrees; the sign says which shoulder goes back.</summary>
    internal double Lean { get; }

    internal double Light { get; }

    internal bool Stood { get; }

    /// <summary>Whether this cover wears the watched corner, and which of the two answers the corner gives.</summary>
    internal bool Watched { get; }

    internal bool PartlyWatched { get; }
  }

  public override void Render(DrawingContext context)
  {
    base.Render(context);

    double width = Bounds.Width;
    double height = Bounds.Height;
    if (_items.Count == 0 || width <= 0.0 || height <= 0.0)
      return;

    double band = height * ROW_SHARE;
    double cover = band * COVER_RATIO;
    double pitch = band * PITCH_RATIO;

    // THE REACH OF THE PAINTING.  A cover flow is ONE picture of a row that goes on past both edges of the
    // television, so the row must not end where its own box ends - the box stands a breath inside the window
    // (the content panel keeps ten points at each hand), and a cover travelling sideways was cut at those two
    // lines: it came into being or left it in full sight of the viewer, which is the popping he counted.  The
    // painting is therefore handed a ground that reaches a cover's width past the box on either hand, and the
    // window's own edge becomes the only edge the row meets.  The box stays where the markup put it - nothing
    // about the room's layout is moved for this - only what is painted on it grows.
    double reach = cover + pitch;

    // How many places lie within that ground, counted from where the row stands rather than from a number kept
    // beside the other numbers: on a wider television the row has to paint further out, and a fixed count left
    // the last cover of a wide screen unpainted - which is the same popping, arriving from the other side.
    int places = (int) Math.Ceiling((width / 2.0 + reach) / Math.Max(1.0, pitch)) + 1;

    int first = Math.Max(0, (int) Math.Floor(_position) - places);
    int last = Math.Min(_items.Count - 1, (int) Math.Ceiling(_position) + places);

    // The bright frame follows the CARET and not the place the row happens to have reached.  It used to be given
    // to whichever cover the row was standing on to within a hundredth of a place, which no cover is while the
    // row still swings: the frame went away for most of the glide, came back as the row arrived, and jumped
    // between two covers when the spring overshot - what the viewer counted as a flickering frame.
    int stood_on = Math.Clamp(SelectedIndex, 0, Math.Max(0, _items.Count - 1));

    List<Frame> frames = new List<Frame>(last - first + 1);
    for (int place = first; place <= last; ++place)
      frames.Add(FrameAt(place - _position, width, height, _items[place], place == stood_on));

    // The covers furthest from the viewer go down first, so that a cover always lies over the one it stands in
    // front of and the stood cover, being last, covers the edges of its neighbours.  This is the one place where
    // the two hands of the row are looked at together, and it is ordered by distance alone.
    frames.Sort((left, right) =>
        Math.Abs(right.Lean).CompareTo(Math.Abs(left.Lean)));

    // The painting is HANDED to the compositor here and happens later, on the thread that draws; nothing asked of
    // the posters may be guarded by a lock taken now.  Hence the gate travelling with the painting, which is what
    // keeps a poster from being dropped out from under a cover being drawn - seen as a hard crash of the front
    // end while a wall was walked, because a thousand-cover row drops posters as fast as it picks them up.
    context.Custom(new CoverPainting(
        new Rect(-reach, 0.0, width + 2.0 * reach, height), frames.ToArray(), _pictures, _picturesLock,
        EyeGlyph()));
  }

  /// <summary>
  /// Where a cover stands, how big it is, how far it has turned and how much light it kept - all of it out of the
  /// signed distance from the cover being stood on, and out of nothing else.  That is the whole of the symmetry:
  /// two covers equally far from the middle are handed the same numbers with the sign of one reversed.
  /// </summary>
  internal static Frame FrameAt(double apart, double width, double height, object item, bool stood = false)
  {
    double distance = Math.Abs(apart);

    // The sign here is the whole of the direction the row turns: with it, a cover standing right of the one being
    // stood on swings its OUTER edge towards the eye, so that the row opens away from the middle like a fan.  The
    // reversed sign leans every cover into the middle, which is the one look the viewer rejected outright.
    double lean = distance < 0.001
        ? 0.0
        : Math.Sign(apart) * Math.Min(LEAN_CEILING, distance * LEAN_PER_PLACE);

    double over = 1.0 - Math.Min(distance * SHRINK_PER_PLACE, SHRINK_CEILING);

    // The light counts the places BEYOND FADE_FROM, not the places as such: inside that distance a cover keeps
    // everything it has, and only the row's outer reaches fade away.
    double light = Math.Clamp(
        1.0 - Math.Max(0.0, distance - FADE_FROM) * DARKEN_PER_PLACE, LIGHT_FLOOR, 1.0);

    double row_height = height * ROW_SHARE;
    double cover_width = row_height * COVER_RATIO;
    double pitch = row_height * PITCH_RATIO;

    // The corner a title wears travels with its cover, so it is read here, on the thread that lays the row out;
    // the painting that happens later on the drawing thread only carries the two answers it found.
    TitleViewModel? title = item as TitleViewModel;
    bool wears_corner = title != null && title.WatchFlagsShown;

    return new Frame(item,
        new SKPoint((float) (width / 2.0 + apart * pitch), (float) (height / 2.0)),
        cover_width * over,
        row_height * over,
        lean,
        light,
        stood,
        wears_corner && title!.IsWatched,
        wears_corner && title!.IsPartlyWatched);
  }

  /// <summary>
  /// The four corners of a turned cover as the lens sees them.  The cover is turned about its own upright line
  /// through its middle; a corner that swings away from the eye is drawn smaller and nearer the middle, which is
  /// all perspective is.  Both hands of the row get their mirror from here because the swing is a function of the
  /// distance alone.
  /// </summary>
  internal static SKPoint[] CornersOf(Frame frame)
  {
    double radians = frame.Lean * Math.PI / 180.0;
    double cos = Math.Cos(radians);
    double sin = Math.Sin(radians);

    double half_width = frame.Width / 2.0;
    double half_height = frame.Height / 2.0;

    double focal = frame.Height * FOCAL_RATIO;

    double[] across = { -half_width, half_width, half_width, -half_width };
    double[] down = { -half_height, -half_height, half_height, half_height };

    SKPoint[] corners = new SKPoint[4];
    for (int corner = 0; corner < 4; ++corner)
    {
      double turned = across[corner] * cos;
      double depth = -across[corner] * sin;
      double through = focal / (focal + depth);

      corners[corner] = new SKPoint(
          (float) (frame.Middle.X + turned * through),
          (float) (frame.Middle.Y + down[corner] * through));
    }

    return corners;
  }

  /// <summary>
  /// The eye the watched corner wears, read from the worn skin once and kept.  A tile is handed the same icon as
  /// a geometry by the template's converter; this row paints with the other toolkit and is therefore handed the
  /// file's own paths, fitted into the same square the skin's reader fits every glyph into
  /// (<see cref="SvgGeometry.GLYPH_BOX"/>), so that the eye on a painted cover and the eye on a tile are one and
  /// the same drawing.  Null when the skin carries no eye, which leaves the corner its disc alone.
  /// </summary>
  private SKPath? EyeGlyph()
  {
    if (!_glyph_read)
    {
      _glyph_read = true;
      _glyph = ReadWatchedGlyph();
    }

    return _glyph;
  }

  private static SKPath? ReadWatchedGlyph()
  {
    WornSkin? worn = SkinImageConverter.WornPictures();
    if (worn == null || !worn.Vectors.TryGetLocation(MARK_ICON, out string file))
      return null;

    string? text = SvgGeometry.ReadText(file);
    if (text == null)
      return null;

    List<SvgGeometry.SvgPath> paths = SvgGeometry.PathsIn(text);
    if (paths.Count == 0)
      return null;

    Rect box = SvgGeometry.BoxOf(text, paths);
    SvgGeometry.Fitting(box, out double fit, out double offset_x, out double offset_y);
    if (fit <= 0.0 || double.IsInfinity(fit) || double.IsNaN(fit))
      return null;

    SKPath glyph = new SKPath();
    bool even_odd = false;

    foreach (SvgGeometry.SvgPath path in paths)
    {
      SKPath? piece = SKPath.ParseSvgPathData(path.Data);
      if (piece == null)
        continue;

      even_odd |= path.EvenOdd;
      glyph.AddPath(piece);
      piece.Dispose();
    }

    // Asked once for the whole drawing rather than per figure, because the hole in an eye is a matter of how its
    // figures wind together.
    glyph.FillType = even_odd ? SKPathFillType.EvenOdd : SKPathFillType.Winding;
    glyph.Transform(SKMatrix.CreateScaleTranslation((float) fit, (float) fit, (float) offset_x, (float) offset_y));
    return glyph;
  }

  /// <summary>
  /// The mapping that lays a flat poster of w x h points over the four corners a lens showed.  Four corner pairs
  /// are eight equations for eight unknowns, so the answer is one matrix and there is nothing left to fit: the
  /// poster's corners land on the projected corners, which is perspective, and a poster mapped through mirrored
  /// corners comes out mirrored.  Written out here rather than asked of the drawing toolkit, because the toolkit's
  /// own helper for this goes by three different names across the versions in play.
  /// </summary>
  /// <param name="corner">The four corners, from the top left, clockwise.</param>
  internal static SKMatrix PerspectiveToQuad(SKPoint[] corner, float width, float height)
  {
    float x0 = corner[0].X, y0 = corner[0].Y;
    float x1 = corner[1].X, y1 = corner[1].Y;
    float x2 = corner[2].X, y2 = corner[2].Y;
    float x3 = corner[3].X, y3 = corner[3].Y;

    float dx1 = x1 - x2, dx2 = x3 - x2, dx3 = x0 - x1 + x2 - x3;
    float dy1 = y1 - y2, dy2 = y3 - y2, dy3 = y0 - y1 + y2 - y3;

    float across = dx1 * dy2 - dx2 * dy1;
    if (Math.Abs(across) < 1e-9f)
    {
      // A cover turned flat enough that its four corners fall onto a line has no perspective to be had; the
      // affine half of the mapping still stands the poster up, which is all a viewer has left to look at.
      return new SKMatrix(
          (x1 - x0) / width, (x3 - x0) / height, x0,
          (y1 - y0) / width, (y3 - y0) / height, y0,
          0.0f, 0.0f, 1.0f);
    }

    float lean_x = (dx3 * dy2 - dx2 * dy3) / across;
    float lean_y = (dx1 * dy3 - dx3 * dy1) / across;

    // The mapping is worked out for a poster spread over 0..1 in both directions; the drawing is handed a poster
    // of its own pixels, so BOTH columns of the mapping are taken BACK by the poster's size rather than multiplied
    // by it.  Multiplied, the far edge of a cover landed a hundred poster-widths off the screen and every cover
    // fell together into a line - what the viewer saw as rods standing in the row.
    return new SKMatrix(
        (x1 - x0 + lean_x * x1) / width, (x3 - x0 + lean_y * x3) / height, x0,
        (y1 - y0 + lean_x * y1) / width, (y3 - y0 + lean_y * y3) / height, y0,
        lean_x / width, lean_y / height, 1.0f);
  }

  /// <summary>Paints the row through Skia, the only toolkit here that maps a picture onto four corners.</summary>
  private sealed class CoverPainting : ICustomDrawOperation
  {
    private readonly Frame[] _frames;

    private readonly Dictionary<object, SKImage?> _pictures;

    private readonly object _gate;

    /// <summary>The watched corner's eye, already fitted into the skin's glyph square; see
    /// <see cref="CoverCarousel.EyeGlyph"/>.</summary>
    private readonly SKPath? _glyph;

    internal CoverPainting(
        Rect bounds, Frame[] frames, Dictionary<object, SKImage?> pictures, object gate, SKPath? glyph)
    {
      Bounds = bounds;
      _frames = frames;
      _pictures = pictures;
      _gate = gate;
      _glyph = glyph;
    }

    public Rect Bounds { get; }

    public bool HitTest(Point point)
    {
      return Bounds.Contains(point);
    }

    public bool Equals(ICustomDrawOperation? other)
    {
      return false;
    }

    public void Dispose()
    {
    }

    public void Render(ImmediateDrawingContext context)
    {
      ISkiaSharpApiLeaseFeature? feature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
      if (feature == null)
        return;

      using ISkiaSharpApiLease lease = feature.Lease();
      SKCanvas canvas = lease.SkCanvas;

      // Held for the whole walk over the covers, because dropping a poster out from under a cover that is being
      // drawn is not a picture missing for a frame - it is the drawing toolkit reading memory it no longer owns.
      canvas.Save();
      lock (_gate)
      {
        foreach (Frame frame in _frames)
          Paint(canvas, frame);
      }
      canvas.Restore();
    }

    private void Paint(SKCanvas canvas, Frame frame)
    {
      SKPoint[] target = CornersOf(frame);
      SKMatrix onto = PerspectiveToQuad(target, (float) frame.Width, (float) frame.Height);

      // The cover's ground, its picture and its edge are ONE shape: the poster's own rounded rectangle, laid onto
      // the four corners the lens showed.  Laid onto them rather than drawn as a rectangle between them, so that
      // the corners keep their round as the cover turns - and a row of covers cut square reads as a wall of
      // planks, which is what the viewer reported the first time this row was drawn.
      using SKPath shape = new SKPath();
      float round = (float) (frame.Width * CORNER_RATIO);
      shape.AddRoundRect(
          new SKRoundRect(new SKRect(0.0f, 0.0f, (float) frame.Width, (float) frame.Height), round, round));
      shape.Transform(onto);

      byte light = (byte) Math.Clamp(frame.Light * 255.0, 0.0, 255.0);

      _pictures.TryGetValue(frame.Item, out SKImage? picture);

      // THE GROUND, and only where a cover has no picture to wear.  This used to be laid under every cover - and
      // offset downwards by nine lines as a shadow - which showed as a dark plate standing out below the poster it
      // was meant to sit behind, and as an outline of its own around covers nobody had stood on.  Both were the
      // row's own doing and nothing of the picture: what a cover shows is its poster, and what it has no poster to
      // show is said by the waiting grey alone.  (The edge a cover does wear is the skin's resting ring, said
      // under THE EDGE below; it is the tile's own, not a second plate behind it.)
      if (picture == null)
      {
        using SKPaint waiting = new SKPaint
        {
          IsAntialias = true,
          Color = COVER_WAITING.WithAlpha(light),
          Style = SKPaintStyle.Fill
        };

        canvas.DrawPath(shape, waiting);
      }
      else
      {
        canvas.Save();
        canvas.ClipPath(shape);

        canvas.Concat(ref onto);

        using SKPaint poster = new SKPaint
        {
          IsAntialias = true,
          Color = new SKColor(255, 255, 255, light)
        };

        canvas.DrawImage(picture, new SKRect(0, 0, (float) frame.Width, (float) frame.Height), poster);
        canvas.Restore();
      }

      // THE CORNER, worn by a title the library knows was watched.  It goes on before the edge, because the edge
      // is the plate's own rim and the corner stands inside it.
      if (frame.Watched || frame.PartlyWatched)
        PaintWatchedCorner(canvas, onto, frame);

      // THE EDGE.  The cover a viewer stands on wears the bright answer that says where they are; every other
      // cover wears the resting ring the wall's covers wear, so that a row of turned covers reads as plates
      // standing in the room rather than as pictures floating over the television.  Earlier the row painted no
      // edge at all here, because what it painted instead was its own dark ground pushed nine lines down, which
      // came out below the poster as a plate and around the un-stood covers as an outline - that ground is gone,
      // and what remains is the skin's own resting edge, faint at its top and fainter down its sides.
      if (frame.Stood)
      {
        using SKPaint edge = new SKPaint
        {
          IsAntialias = true,
          Color = FRAME_STOOD,
          Style = SKPaintStyle.Stroke,
          StrokeWidth = 2.2f
        };

        canvas.DrawPath(shape, edge);
      }
      else
      {
        // The ring's light runs down the cover's own upright line, from the middle of its top edge to the middle
        // of its bottom one, so that a turned cover is lit along its own face and not along the screen.
        int step = (int) Math.Round(Math.Clamp(frame.Light, 0.0, 1.0) * RING_LIGHT_STEPS);
        SKPoint above = new SKPoint(
            (target[0].X + target[1].X) / 2.0f, (target[0].Y + target[1].Y) / 2.0f);
        SKPoint below = new SKPoint(
            (target[2].X + target[3].X) / 2.0f, (target[2].Y + target[3].Y) / 2.0f);

        using SKShader ring_light = SKShader.CreateLinearGradient(
            above, below, FRAME_RESTING[step], FRAME_RESTING_STOPS, SKShaderTileMode.Clamp);
        using SKPaint edge = new SKPaint
        {
          IsAntialias = true,
          Shader = ring_light,
          Style = SKPaintStyle.Stroke,
          StrokeWidth = 2.0f
        };

        canvas.DrawPath(shape, edge);
      }

      // THE PLATE SPEAKS.  A group's divider has no poster to wear, and a grey cover with no word reads as a
      // hole in the row rather than as the boundary of a group - so the value's word stands on it, letter under
      // letter the way a book's spine is read, dimmed by the same light the cover beside it was dimmed by.  The
      // caret never stands here, so there is no stood answer; the resting ring above is all the edge gets.
      if (frame.Item is GroupHeaderRow divider)
        PaintDividerWord(canvas, onto, frame, divider);
    }

    /// <summary>
    /// Paints one group plate's word in the plate's OWN space, so the letters lie on the plate and turn with it
    /// rather than floating in front of the row - the same law the watched corner obeys.
    /// </summary>
    private void PaintDividerWord(SKCanvas canvas, SKMatrix onto, Frame frame, GroupHeaderRow divider)
    {
      canvas.Save();
      canvas.Concat(ref onto);

      double kept = Math.Clamp(frame.Light, 0.0, 1.0);
      using SKPaint word = new SKPaint
      {
        IsAntialias = true,
        Color = new SKColor(255, 255, 255, (byte) Math.Round(235.0 * kept)),
        TextAlign = SKTextAlign.Center,
        TextSize = (float) Math.Clamp(frame.Width * 0.14, 18.0, 34.0),
        FakeBoldText = true
      };

      float across = (float) frame.Width;
      float down = (float) frame.Height;
      string text = divider.Label ?? string.Empty;

      // Letter under letter, centred on the plate: a long value stays inside the cover's height without the
      // word shrinking below what a viewer can read from the sofa.
      float line = word.TextSize * 1.12f;
      float middle = down / 2.0f - (text.Length - 1) * line / 2.0f;
      for (int i = 0; i < text.Length; i++)
      {
        char letter = text[i];
        if (letter == ' ')
          continue;
        canvas.DrawText(letter.ToString(), across / 2.0f,
            middle + i * line + word.TextSize * 0.36f, word);
      }

      // The count, small and dim, sits at the plate's foot whenever the shelf may say it at all.
      if (divider.CountWord.Length > 0)
      {
        using SKPaint count = new SKPaint
        {
          IsAntialias = true,
          Color = new SKColor(255, 255, 255, (byte) Math.Round(140.0 * kept)),
          TextAlign = SKTextAlign.Center,
          TextSize = word.TextSize * 0.62f
        };
        canvas.DrawText(divider.CountWord, across / 2.0f, down - 18.0f, count);
      }

      canvas.Restore();
    }

    /// <summary>
    /// The watched corner: its disc and the eye standing on it, painted in the cover's OWN space so that the
    /// corner lies on the plate and turns with it rather than floating over the screen.  The numbers are the
    /// templates' numbers (thirty-eight of disc, twenty-eight of eye, six from the cover's top and right edge),
    /// and the whole corner is dimmed to the light the cover's place in the row left it, which is what a stacked
    /// tile does to its corner through the tile's opacity.
    /// </summary>
    private void PaintWatchedCorner(SKCanvas canvas, SKMatrix onto, Frame frame)
    {
      double kept = Math.Clamp(frame.Light, 0.0, 1.0);
      float middle_x = (float) (frame.Width - MARK_MARGIN - MARK_DISC / 2.0);
      float middle_y = (float) (MARK_MARGIN + MARK_DISC / 2.0);

      canvas.Save();
      canvas.Concat(ref onto);

      using (SKPaint ground = new SKPaint
      {
        IsAntialias = true,
        Color = MARK_DISC_GROUND.WithAlpha((byte) Math.Round(MARK_DISC_GROUND.Alpha * kept)),
        Style = SKPaintStyle.Fill
      })
      {
        canvas.DrawCircle(middle_x, middle_y, (float) (MARK_DISC / 2.0), ground);
      }

      if (_glyph == null)
      {
        // A skin that carries no eye leaves the corner its disc alone, exactly as a tile does.
        canvas.Restore();
        return;
      }

      double measure = MARK_EYE / SvgGeometry.GLYPH_BOX;
      SKMatrix onto_eye = SKMatrix.CreateScaleTranslation(
          (float) measure,
          (float) measure,
          middle_x - (float) (SvgGeometry.GLYPH_BOX / 2.0 * measure),
          middle_y - (float) (SvgGeometry.GLYPH_BOX / 2.0 * measure));
      canvas.Concat(ref onto_eye);

      if (frame.PartlyWatched)
      {
        // The part answer is a light running ACROSS the eye -- nothing at its left hand, whole white at its right
        // -- which is the skin's gradient read across the eye's own square.
        byte whole = (byte) Math.Round(0xFF * kept);
        using SKShader across_the_eye = SKShader.CreateLinearGradient(
            new SKPoint(0.0f, 0.0f),
            new SKPoint((float) SvgGeometry.GLYPH_BOX, 0.0f),
            new SKColor[] { new SKColor(255, 255, 255, 0), new SKColor(255, 255, 255, whole) },
            MARK_PARTLY_STOPS,
            SKShaderTileMode.Clamp);
        using SKPaint eye = new SKPaint { IsAntialias = true, Shader = across_the_eye };

        canvas.DrawPath(_glyph, eye);
      }
      else
      {
        using SKPaint eye = new SKPaint
        {
          IsAntialias = true,
          Color = MARK_EYE_LIGHT.WithAlpha((byte) Math.Round(MARK_EYE_LIGHT.Alpha * kept))
        };

        canvas.DrawPath(_glyph, eye);
      }

      canvas.Restore();
    }
  }

  protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
  {
    base.OnAttachedToVisualTree(e);
    StartSpring();
  }

  protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
  {
    base.OnDetachedFromVisualTree(e);

    StopSpring();
    Interlocked.Increment(ref _detach_pass);

    lock (_picturesLock)
    {
      foreach (SKImage? picture in _pictures.Values)
        picture?.Dispose();

      _pictures.Clear();
      _remembered.Clear();
    }

    foreach (object item in _askedAbout)
    {
      if (item is INotifyPropertyChanged row)
        row.PropertyChanged -= OnCoverAnswered;
    }

    _askedAbout.Clear();

    if (_watched is INotifyCollectionChanged changed)
      changed.CollectionChanged -= OnItemsChanged;
  }
}
