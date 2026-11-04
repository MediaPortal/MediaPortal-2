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

  internal const int PAINTED_EACH_SIDE = 7;

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
  internal const double PITCH_RATIO = 160.0 / 344.0;

  /// <summary>
  /// How far in front of the row the lens stands, relative to a cover's height, as the reference camera does.
  /// This is what makes a cover turned forty degrees show its far edge noticeably smaller than its near one; a
  /// lens standing further off leaves the same angle in the numbers but reads as a shear rather than a turn.
  /// </summary>
  internal const double FOCAL_RATIO = 900.0 / 344.0;

  internal const double LEAN_PER_PLACE = 19.0;
  internal const double LEAN_CEILING = 68.0;
  internal const double SHRINK_PER_PLACE = 0.085;
  internal const double SHRINK_CEILING = 0.48;
  internal const double DARKEN_PER_PLACE = 0.12;

  internal const double LIGHT_FLOOR = 0.38;

  /// <summary>How round a cover's corners are, as a part of its own width; the turn takes the round with it.</summary>
  internal const double CORNER_RATIO = 0.045;

  static readonly SKColor FRAME_STOOD = new SKColor(245, 245, 255, 220);
  static readonly SKColor FRAME_OTHER = new SKColor(255, 255, 255, 55);
  static readonly SKColor COVER_GROUND = new SKColor(35, 35, 39);
  static readonly SKColor COVER_WAITING = new SKColor(65, 65, 72);

  private readonly List<object> _items = new List<object>();
  private readonly Dictionary<object, SKImage?> _pictures = new Dictionary<object, SKImage?>();

  private readonly LinkedList<object> _remembered = new LinkedList<object>();

  private readonly HashSet<object> _askedAbout = new HashSet<object>();

  /// <summary>The posters whose bytes are being read right now, so that one poster is never read twice.</summary>
  private readonly HashSet<object> _being_read = new HashSet<object>();
  private readonly object _picturesLock = new object();

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
        _position = clamped;
        _velocity = 0.0;
      }

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

    for (int place = middle - PAINTED_EACH_SIDE - 1; place <= middle + PAINTED_EACH_SIDE + 1; ++place)
    {
      if (place < 0 || place >= _items.Count)
        continue;

      object item = _items[place];

      if (_askedAbout.Add(item) && item is INotifyPropertyChanged row)
        row.PropertyChanged += OnCoverAnswered;

      PictureAsked?.Invoke(item);
      DecodeWhenBytesArrive(item);
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

    Task.Run(delegate
    {
      SKImage? picture;
      try
      {
        using SKBitmap? bitmap = SKBitmap.Decode(toRead);
        picture = bitmap == null ? null : SKImage.FromBitmap(bitmap);
      }
      catch (Exception)
      {
        picture = null;
      }

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
    double apart = to - _position;

    _velocity += apart * 0.16;
    _velocity *= 0.76;
    _position += _velocity;

    if (Math.Abs(apart) < 0.001 && Math.Abs(_velocity) < 0.001)
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

    SetCurrentValue(SelectedIndexProperty, clamped);
  }

  // ---- What the eye sees ---------------------------------------------------------------

  /// <summary>One cover, said as the numbers that draw it rather than as a control.</summary>
  internal readonly struct Frame
  {
    internal Frame(object item, SKPoint middle, double width, double height, double lean, double light,
        bool stood)
    {
      Item = item;
      Middle = middle;
      Width = width;
      Height = height;
      Lean = lean;
      Light = light;
      Stood = stood;
    }

    internal object Item { get; }

    internal SKPoint Middle { get; }

    internal double Width { get; }

    internal double Height { get; }

    /// <summary>The turn about the cover own upright line, in degrees; the sign says which shoulder goes back.</summary>
    internal double Lean { get; }

    internal double Light { get; }

    internal bool Stood { get; }
  }

  public override void Render(DrawingContext context)
  {
    base.Render(context);

    double width = Bounds.Width;
    double height = Bounds.Height;
    if (_items.Count == 0 || width <= 0.0 || height <= 0.0)
      return;

    int first = Math.Max(0, (int) Math.Floor(_position) - PAINTED_EACH_SIDE);
    int last = Math.Min(_items.Count - 1, (int) Math.Ceiling(_position) + PAINTED_EACH_SIDE);

    List<Frame> frames = new List<Frame>(last - first + 1);
    for (int place = first; place <= last; ++place)
      frames.Add(FrameAt(place - _position, width, height, _items[place]));

    // The covers furthest from the viewer go down first, so that a cover always lies over the one it stands in
    // front of and the stood cover, being last, covers the edges of its neighbours.  This is the one place where
    // the two hands of the row are looked at together, and it is ordered by distance alone.
    frames.Sort((left, right) =>
        Math.Abs(right.Lean).CompareTo(Math.Abs(left.Lean)));

    // The painting is HANDED to the compositor here and happens later, on the thread that draws; nothing asked of
    // the posters may be guarded by a lock taken now.  Hence the gate travelling with the painting, which is what
    // keeps a poster from being dropped out from under a cover being drawn - seen as a hard crash of the front
    // end while a wall was walked, because a thousand-cover row drops posters as fast as it picks them up.
    context.Custom(new CoverPainting(new Rect(0, 0, width, height), frames.ToArray(), _pictures, _picturesLock));
  }

  /// <summary>
  /// Where a cover stands, how big it is, how far it has turned and how much light it kept - all of it out of the
  /// signed distance from the cover being stood on, and out of nothing else.  That is the whole of the symmetry:
  /// two covers equally far from the middle are handed the same numbers with the sign of one reversed.
  /// </summary>
  internal static Frame FrameAt(double apart, double width, double height, object item)
  {
    double distance = Math.Abs(apart);

    // The sign here is the whole of the direction the row turns: with it, a cover standing right of the one being
    // stood on swings its OUTER edge towards the eye, so that the row opens away from the middle like a fan.  The
    // reversed sign leans every cover into the middle, which is the one look the viewer rejected outright.
    double lean = distance < 0.001
        ? 0.0
        : Math.Sign(apart) * Math.Min(LEAN_CEILING, distance * LEAN_PER_PLACE);

    double over = 1.0 - Math.Min(distance * SHRINK_PER_PLACE, SHRINK_CEILING);
    double light = Math.Clamp(1.0 - distance * DARKEN_PER_PLACE, LIGHT_FLOOR, 1.0);

    double row_height = height * ROW_SHARE;
    double cover_width = row_height * COVER_RATIO;
    double pitch = row_height * PITCH_RATIO;

    return new Frame(item,
        new SKPoint((float) (width / 2.0 + apart * pitch), (float) (height / 2.0)),
        cover_width * over,
        row_height * over,
        lean,
        light,
        distance < 0.01);
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

    internal CoverPainting(Rect bounds, Frame[] frames, Dictionary<object, SKImage?> pictures, object gate)
    {
      Bounds = bounds;
      _frames = frames;
      _pictures = pictures;
      _gate = gate;
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

      using (SKPaint shadow = new SKPaint
      {
        IsAntialias = true,
        Color = SKColors.Black.WithAlpha((byte) (frame.Light * 130.0)),
        Style = SKPaintStyle.Fill
      })
      {
        canvas.Save();
        canvas.Translate(0.0f, 9.0f);
        canvas.DrawPath(shape, shadow);
        canvas.Restore();
      }

      using (SKPaint ground = new SKPaint
      {
        IsAntialias = true,
        Color = COVER_GROUND.WithAlpha(light),
        Style = SKPaintStyle.Fill
      })
      {
        canvas.DrawPath(shape, ground);
      }

      _pictures.TryGetValue(frame.Item, out SKImage? picture);

      using (SKPaint edge = new SKPaint
      {
        IsAntialias = true,
        Color = frame.Stood ? FRAME_STOOD : FRAME_OTHER,
        Style = SKPaintStyle.Stroke,
        StrokeWidth = frame.Stood ? 2.2f : 1.0f
      })
      {
        if (picture != null)
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
        else
        {
          using SKPaint waiting = new SKPaint
          {
            IsAntialias = true,
            Color = COVER_WAITING.WithAlpha(light),
            Style = SKPaintStyle.Fill
          };

          canvas.DrawPath(shape, waiting);
        }

        canvas.DrawPath(shape, edge);
      }
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
