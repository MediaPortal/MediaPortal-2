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

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Generators;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.VisualTree;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace MediaPortal.Frontend.Views
{
  /// <summary>
  /// The tiles' panel: laid left to right and wrapping whole the way a <see cref="WrapPanel"/> lays them,
  /// but with only the rows standing on the tree that the screen could be drawing.
  /// </summary>
  /// <remarks>
  /// A wrap panel hands a container to every title in the shelf, and a shelf is a film library: eleven
  /// hundred containers wearing the skin's tile frame were measured at 1.7 seconds before the first picture
  /// showed. The reference never pays that, because its GUI builds a container as a tile comes into sight and
  /// takes it away when it leaves - which is what this panel does with Avalonia's own container generator.
  /// The cell the tiles make is asked of one container rather than of all of them, the columns follow from the
  /// width the shelf was given, and the rows built are the ones crossing the drawn band plus a band's worth
  /// above and below. What leaves the band goes back into a pool and comes out of it again under another
  /// title, so scrolling costs a handful of containers instead of the shelf.
  ///
  /// The geometry is the wrap panel's own to the millimetre: containers are measured with the constraint the
  /// panel was given, one row is as high as its tallest tile, and the margins the tile frames carry are
  /// themselves the spacing - which is why nothing here has to know what a poster or a banner measures.
  ///
  /// The panel is deliberately ignorant of what a tile looks like. Selection, focus and the glow the skin
  /// hangs on a selected container stay the list's own work, because the containers here are the same
  /// <c>ListBoxItem</c>s a wrap panel would have made; only how many of them exist has changed.
  /// </remarks>
  public class VirtualizingTilePanel : VirtualizingPanel
  {
    /// <summary>
    /// How much shelf keeps its containers beyond what the screen draws, counted in screens.
    /// </summary>
    /// <remarks>
    /// One screen in each direction, which is what the row painting works to as well: arriving at a tile that
    /// is already dressed, rather than at grey.
    /// </remarks>
    private const double BANDS_ABOVE_AND_BELOW = 1.0;

    /// <summary>The pool a container came back from, remembered on the container itself.</summary>
    private static readonly AttachedProperty<object?> RecycleKeyProperty =
        AvaloniaProperty.RegisterAttached<VirtualizingTilePanel, Control, object?>("RecycleKey");

    /// <summary>Marked on a title that is its own container, which no pool may take back.</summary>
    private static readonly object ITS_OWN_CONTAINER = new object();

    /// <summary>Containers that stand, in the order of their titles, starting at <see cref="_standingAt"/>.</summary>
    private readonly List<Control> _standing = new List<Control>();

    /// <summary>Containers that stood and are kept for the next title that needs one.</summary>
    private readonly Dictionary<object, Stack<Control>> _pooled = new Dictionary<object, Stack<Control>>();

    private int _standingAt = -1;
    private Size _cell;
    private int _columns = 1;

    /// <summary>
    /// How far the whole wall stands from the scroller's left edge: half of what the columns leave over.
    /// </summary>
    private double _xOffset;

    /// <summary>What the screen draws, in the shelf's own coordinates.</summary>
    private Rect _view;

    /// <summary>What the containers standing after the last measure cover, top to bottom.</summary>
    private Rect _covered = new Rect(0, 0, 0, -1);

    private bool _layingOut;
    private Control? _focusHeld;
    private int _focusHeldAt = -1;
    private Control? _targetHeld;
    private int _targetHeldAt = -1;
    private int _toBringBackInSight = -1;
    private IScrollAnchorProvider? _anchors;

    public VirtualizingTilePanel()
    {
      // The band the screen draws is the only thing that says which rows have to exist, and the panel learns
      // of it through this event rather than by being asked: a shelf inside a scroller is measured with as
      // much room as it likes, which says nothing at all about what is on the television.
      EffectiveViewportChanged += OnEffectiveViewportChanged;
    }

    /// <summary>The first title that has a container, for the tests that count what stands.</summary>
    public int FirstStandingIndex => _standingAt;

    /// <summary>How many containers stand, which is what the whole panel exists to keep small.</summary>
    public int StandingCount => _standing.Count;

    /// <summary>How many tiles one row holds at the width the shelf was measured with.</summary>
    public int Columns => _columns;

    /// <summary>
    /// How many containers have been built in the panel's whole life. On a shelf that is read right through
    /// this stays near the number of tiles the screen can show; if it climbs with the number of titles, the
    /// pool is not catching anything.
    /// </summary>
    public int ContainersBuilt { get; private set; }

    /// <summary>How many times a container that had left the band was taken out of the pool again.</summary>
    public int ContainersReused { get; private set; }

    protected override Size MeasureOverride(Size availableSize)
    {
      if (ItemContainerGenerator == null || Items.Count == 0)
      {
        TakeAllDown();
        return default;
      }

      _layingOut = true;
      try
      {
        ForgetHeldFocus();

        double track = double.IsPositiveInfinity(availableSize.Width)
            ? Math.Max(_view.Width, _cell.Width)
            : availableSize.Width;

        Size cell = TheCell(availableSize);
        if (cell.Width <= 0 || cell.Height <= 0)
          return default;

        int columns = Math.Max(1, (int)Math.Floor(track / cell.Width + 0.000001));
        int rows = (int)Math.Ceiling(Items.Count / (double)columns);

        if (cell != _cell || columns != _columns)
        {
          // The frame the tiles wear changed under the shelf - a skin put on, a poster turned into a banner -
          // and nothing that stands knows its row any more.
          _cell = cell;
          _columns = columns;
          TakeAllDown();
        }

        // A whole number of columns seldom fills the scroller, and what it leaves over used to lie whole at the
        // wall's right hand end, so the wall leaned against the room's left edge while a band of empty fanart
        // waited before the details column.  The rest is shared between both hands now.  The scroller keeps its
        // own width and therefore its bar at the edge of the room; only the tiles move.
        _xOffset = Math.Max(0, (track - _columns * _cell.Width) / 2);

        Rect wanted = TheBandToStand(rows);
        int first = RowOf(wanted.Top) * _columns;
        int last = Math.Min(Items.Count - 1, (RowOf(wanted.Bottom) + 1) * _columns - 1);

        StandUp(first, last, availableSize);

        // A container that was borrowed for a scroll is only borrowed: the row it belongs to has either taken
        // it by now, or the shelf is not looking at that row at all.
        if (_targetHeld != null)
          TakeDown(_targetHeld, _targetHeldAt);

        return new Size(Math.Min(track, _columns * _cell.Width), rows * _cell.Height);
      }
      finally
      {
        _layingOut = false;
      }
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
      _layingOut = true;
      try
      {
        for (int i = 0; i < _standing.Count; ++i)
          PutInItsPlace(_standing[i], _standingAt + i);

        // A container held outside the standing rows still has to be somewhere: it is either wearing the
        // focus or on its way into sight, and in both cases it sits where its title belongs.
        if (_focusHeld != null)
          PutInItsPlace(_focusHeld, _focusHeldAt);
        if (_targetHeld != null)
          PutInItsPlace(_targetHeld, _targetHeldAt);

        return finalSize;
      }
      finally
      {
        _layingOut = false;
      }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
      base.OnAttachedToVisualTree(e);
      _anchors = this.FindAncestorOfType<IScrollAnchorProvider>();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
      base.OnDetachedFromVisualTree(e);
      _anchors = null;
    }

    protected override void OnItemsControlChanged(ItemsControl? oldValue)
    {
      // The panel is being taken off this list, and the containers it was handed went with it.
      if (ItemsControl == null)
      {
        _standing.Clear();
        _standingAt = -1;
        _focusHeld = null;
        _focusHeldAt = -1;
        _targetHeld = null;
        _targetHeldAt = -1;
        _toBringBackInSight = -1;
        _pooled.Clear();
        _covered = new Rect(0, 0, 0, -1);
      }
    }

    protected override void OnItemsChanged(IReadOnlyList<object?> items, NotifyCollectionChangedEventArgs e)
    {
      // A shelf that grows at the back - which is how it reads itself - leaves everything standing that
      // stands already; only the height it stretches to has grown.
      if (e.Action == NotifyCollectionChangedAction.Add && e.NewStartingIndex > _standingAt + _standing.Count)
      {
        InvalidateMeasure();
        return;
      }

      // Anything else moves titles between rows out from under the containers showing them, so the rows are
      // built again. A cleared shelf is announced as a reset, which is also how a new frame for the tiles is
      // announced, and both mean the pooled containers were dressed for the old one.
      TakeAllDown();
      _pooled.Clear();
      InvalidateMeasure();
    }

    protected override IInputElement? GetControl(NavigationDirection direction, IInputElement? from,
        bool wrap)
    {
      int count = Items.Count;
      if (count == 0)
        return null;

      int here = from is Control container ? IndexFromContainer(container) : -1;
      int rowsInSight = Math.Max(1, (int)Math.Round(_view.Height / Math.Max(_cell.Height, 1)));

      int there = direction switch
      {
        NavigationDirection.First => 0,
        NavigationDirection.Last => count - 1,
        // Sideways is one tile, and up or down is exactly one row - which is what a row of tiles means: the
        // tile under this one is the same place in the row below.
        NavigationDirection.Left or NavigationDirection.Previous => here - 1,
        NavigationDirection.Right or NavigationDirection.Next => here + 1,
        NavigationDirection.Up => here - _columns,
        NavigationDirection.Down => here + _columns,
        NavigationDirection.PageUp => here - _columns * rowsInSight,
        NavigationDirection.PageDown => here + _columns * rowsInSight,
        _ => here
      };

      if (there == here)
        return from;

      if (there < 0 || there >= count)
      {
        // At the end of a row of tiles the arrow has nowhere to go. Staying put is the answer here, and the
        // bounce the shelf gives is what tells the viewer that the edge is the edge.
        if (!wrap)
          return from;

        there = ((there % count) + count) % count;
      }

      return ScrollIntoView(there) ?? from;
    }

    protected override IEnumerable<Control>? GetRealizedContainers()
    {
      return _standing;
    }

    protected override Control? ContainerFromIndex(int index)
    {
      if (index < 0 || index >= Items.Count)
        return null;
      if (index == _targetHeldAt)
        return _targetHeld;
      if (index == _focusHeldAt)
        return _focusHeld;

      int offset = index - _standingAt;
      return offset >= 0 && offset < _standing.Count ? _standing[offset] : null;
    }

    protected override int IndexFromContainer(Control container)
    {
      if (ReferenceEquals(container, _targetHeld))
        return _targetHeldAt;
      if (ReferenceEquals(container, _focusHeld))
        return _focusHeldAt;

      int offset = _standing.IndexOf(container);
      return offset < 0 ? -1 : _standingAt + offset;
    }

    protected override Control? ScrollIntoView(int index)
    {
      if (_layingOut || ItemContainerGenerator == null || index < 0 || index >= Items.Count
          || !IsEffectivelyVisible)
        return null;

      if (ContainerFromIndex(index) is Control alreadyInSight)
      {
        alreadyInSight.BringIntoView();
        return alreadyInSight;
      }

      // The container is built now and put where its title belongs, so that the scroller has something to look
      // at; the layout pass that follows hands it to the row it belongs to.
      Control target = TakeOrMake(index);
      target.Measure(Size.Infinity);
      target.Arrange(ThePlace(index, target.DesiredSize));

      _targetHeld = target;
      _targetHeldAt = index;

      // The rows around it have to be laid out for the scroll to find the shelf at its full height, so the
      // measure is asked for before the scroll rather than after it.
      InvalidateMeasure();
      target.BringIntoView();

      if (!_view.Contains(ThePlace(index, target.DesiredSize)))
      {
        // Either the band on the screen does not reach this title yet - which the measure asked for above is
        // about to change - or the shelf has grown since it was measured and the scroller does not yet know
        // how far down the title is. One more ask once the screen has reported its band, and only then, so
        // that nobody fights with the viewer's own scrolling.
        _toBringBackInSight = index;
      }

      return target;
    }

    /// <summary>
    /// The size one tile takes, margins and all, from the container of the title the screen is looking at.
    /// </summary>
    private Size TheCell(Size constraint)
    {
      int probe = _view.Height > 0 ? RowOf(_view.Top) * _columns : 0;
      probe = Math.Min(Math.Max(probe, 0), Items.Count - 1);

      Control container = TakeOrMake(probe);
      container.Measure(constraint);
      return container.DesiredSize;
    }

    /// <summary>
    /// The stretch of shelf whose titles get a container: what the screen draws, with a band above and below.
    /// </summary>
    private Rect TheBandToStand(int rows)
    {
      double full = rows * _cell.Height;

      if (_view.Height <= 0)
        return new Rect(0, 0, _view.Width, _cell.Height);

      double buffer = _view.Height * BANDS_ABOVE_AND_BELOW;
      double top = Math.Max(0, _view.Top - buffer);
      double bottom = Math.Min(full, _view.Bottom + buffer);

      // A title the shelf is on its way to, or one that wears the focus outside the band, keeps the container
      // it was borrowed for and stands where it belongs; the band itself never reaches out for it. Were the
      // band stretched to a far-away target, a shelf being scrolled to its end would stand every row on the
      // way there - the whole thing - which is the one thing this panel was built to avoid.
      return new Rect(_view.X, top, _view.Width, Math.Max(0, bottom - top));
    }

    /// <summary>
    /// Gives every title between the two a container, takes the containers away from those whose titles have
    /// left the band, and measures what stands.
    /// </summary>
    private void StandUp(int first, int last, Size constraint)
    {
      Control?[] staying = new Control?[Math.Max(0, last - first + 1)];

      for (int i = 0; i < _standing.Count; ++i)
      {
        int index = _standingAt + i;
        if (index >= first && index <= last)
          staying[index - first] = _standing[i];
        else
          TakeDown(_standing[i], index);
      }

      _standing.Clear();
      _standingAt = first;

      for (int index = first; index <= last; ++index)
      {
        Control container = staying[index - first] ?? TakeHeld(index) ?? TakeOrMake(index);
        container.Measure(constraint);
        _standing.Add(container);
      }

      int topRow = _standing.Count > 0 ? RowOf(_standingAt) : 0;
      int bottomRow = _standing.Count > 0 ? RowOf(_standingAt + _standing.Count - 1) : -1;
      _covered = new Rect(_view.X, topRow * _cell.Height, _view.Width,
          Math.Max(0, (bottomRow + 1 - topRow) * _cell.Height));
    }

    /// <summary>
    /// The container a borrowed title was held for, should the band have come round to that title.
    /// </summary>
    private Control? TakeHeld(int index)
    {
      if (index == _targetHeldAt && _targetHeld != null)
      {
        Control container = _targetHeld;
        _targetHeld = null;
        _targetHeldAt = -1;
        return container;
      }

      if (index == _focusHeldAt && _focusHeld != null)
      {
        Control container = _focusHeld;
        _focusHeld = null;
        _focusHeldAt = -1;
        return container;
      }

      return null;
    }

    /// <summary>
    /// A container whose title left the band: back into the pool, unless the focus stands in it, which is the
    /// one thing a pool cannot hold on to.
    /// </summary>
    private void TakeDown(Control container, int index)
    {
      if (ReferenceEquals(container, _targetHeld))
      {
        _targetHeld = null;
        _targetHeldAt = -1;
      }

      if (container.IsFocused)
      {
        // The focus is the one thing a pool cannot hold on to. The container is held as the focused one, and
        // as nothing else: held twice over, one of the two holds would outlive the other and hand back a
        // container that the pool had already given to another title.
        _focusHeld = container;
        _focusHeldAt = index;
        return;
      }

      if (ReferenceEquals(container, _focusHeld))
      {
        _focusHeld = null;
        _focusHeldAt = -1;
      }

      PutBack(container);
    }

    /// <summary>Forgets every container, standing or borrowed, and hands the pooled ones back.</summary>
    private void TakeAllDown()
    {
      foreach (Control container in _standing)
        PutBack(container);

      _standing.Clear();
      _standingAt = -1;

      if (_focusHeld != null)
        PutBack(_focusHeld);
      if (_targetHeld != null)
        PutBack(_targetHeld);

      _focusHeld = null;
      _focusHeldAt = -1;
      _targetHeld = null;
      _targetHeldAt = -1;
      _covered = new Rect(0, 0, 0, -1);
    }

    /// <summary>
    /// Lets go of a container the focus has moved out of, so that it can be pooled like any other.
    /// </summary>
    private void ForgetHeldFocus()
    {
      if (_focusHeld == null || _focusHeld.IsFocused)
        return;

      Control container = _focusHeld;
      _focusHeld = null;
      _focusHeldAt = -1;
      PutBack(container);
    }

    /// <summary>
    /// A container for one title: the one that stands, one out of the pool, or a new one from the generator.
    /// </summary>
    private Control TakeOrMake(int index)
    {
      ItemContainerGenerator generator = ItemContainerGenerator!;
      object? item = Items[index];

      if (!generator.NeedsContainer(item, index, out object? key))
      {
        // A title that is its own container is put on the shelf as it is, once, and is never pooled.
        Control itsOwn = (Control)item!;
        if (!itsOwn.IsSet(RecycleKeyProperty))
        {
          generator.PrepareItemContainer(itsOwn, itsOwn, index);
          AddInternalChild(itsOwn);
          itsOwn.SetValue(RecycleKeyProperty, ITS_OWN_CONTAINER);
          generator.ItemContainerPrepared(itsOwn, item, index);
        }

        itsOwn.SetCurrentValue(IsVisibleProperty, true);
        return itsOwn;
      }

      if (key != null && _pooled.TryGetValue(key, out Stack<Control>? pool) && pool.Count > 0)
      {
        Control recycled = pool.Pop();
        ContainersReused++;
        recycled.SetCurrentValue(IsVisibleProperty, true);
        generator.PrepareItemContainer(recycled, item, index);
        AddInternalChild(recycled);
        generator.ItemContainerPrepared(recycled, item, index);
        return recycled;
      }

      Control fresh = generator.CreateContainer(item, index, key);
      ContainersBuilt++;
      fresh.SetValue(RecycleKeyProperty, key);
      generator.PrepareItemContainer(fresh, item, index);
      AddInternalChild(fresh);
      generator.ItemContainerPrepared(fresh, item, index);
      return fresh;
    }

    /// <summary>Takes a container off the shelf and keeps it for the next title that needs one.</summary>
    private void PutBack(Control container)
    {
      _anchors?.UnregisterAnchorCandidate(container);

      // A container that goes back to the pool is nobody's target and nobody's focus any more. Were a hold to
      // keep pointing at it, the next row to empty the pool would be handed the very container a scroll is on
      // its way to, and the shelf would show one title where another was asked for.
      if (ReferenceEquals(container, _targetHeld))
      {
        _targetHeld = null;
        _targetHeldAt = -1;
      }

      if (ReferenceEquals(container, _focusHeld))
      {
        _focusHeld = null;
        _focusHeldAt = -1;
      }

      object? key = container.GetValue(RecycleKeyProperty);
      ItemContainerGenerator? generator = ItemContainerGenerator;

      if (key == null)
      {
        // Nothing to go back to: the container was made for this one title and is thrown away with it.
        generator?.ClearItemContainer(container);
        RemoveInternalChild(container);
        return;
      }

      if (key == ITS_OWN_CONTAINER)
      {
        container.SetCurrentValue(IsVisibleProperty, false);
        RemoveInternalChild(container);
        return;
      }

      if (generator == null)
        return;

      generator.ClearItemContainer(container);

      if (!_pooled.TryGetValue(key, out Stack<Control>? pool))
      {
        pool = new Stack<Control>();
        _pooled.Add(key, pool);
      }

      pool.Push(container);
      container.SetCurrentValue(IsVisibleProperty, false);
      RemoveInternalChild(container);
    }

    /// <summary>
    /// Puts a container where its title sits in the grid and offers it to the scroller as what the scroll
    /// should hold on to while the shelf moves.
    /// </summary>
    private void PutInItsPlace(Control container, int index)
    {
      if (index < 0 || index >= Items.Count)
        return;

      Rect place = ThePlace(index, container.DesiredSize);
      container.Arrange(place);

      if (container.IsVisible && _view.Intersects(place))
      {
        try
        {
          _anchors?.RegisterAnchorCandidate(container);
        }
        catch (InvalidOperationException)
        {
          // The container went somewhere else while the shelf was being laid out. Nothing to anchor.
        }
      }
    }

    /// <summary>Where a title sits: its column across, its row down, the whole wall in the middle of the track.</summary>
    private Rect ThePlace(int index, Size size)
    {
      int row = RowOf(index);
      return new Rect(_xOffset + (index - row * _columns) * _cell.Width, row * _cell.Height, size.Width,
          size.Height);
    }

    /// <summary>The row a title belongs to.</summary>
    private int RowOf(int index)
    {
      return index / _columns;
    }

    /// <summary>The row a height inside the shelf belongs to.</summary>
    private int RowOf(double height)
    {
      return _cell.Height <= 0 ? 0 : Math.Max(0, (int)Math.Floor(height / _cell.Height));
    }

    /// <summary>
    /// Remembers what the screen draws, asks for the rows again should it slide out of what stands, and
    /// finishes one scroll that had to wait for the shelf's height.
    /// </summary>
    private void OnEffectiveViewportChanged(object? sender, EffectiveViewportChangedEventArgs e)
    {
      _view = e.EffectiveViewport.Intersect(new Rect(Bounds.Size));

      if (_toBringBackInSight >= 0)
      {
        int ask = _toBringBackInSight;
        _toBringBackInSight = -1;

        if (ContainerFromIndex(ask) is Control stillThere
            && !_view.Contains(ThePlace(ask, stillThere.DesiredSize)))
          stillThere.BringIntoView();
      }

      double buffer = _view.Height * BANDS_ABOVE_AND_BELOW;
      if (_standing.Count > 0
          && _view.Top - buffer >= _covered.Top
          && _view.Bottom + buffer <= _covered.Bottom)
        return;

      InvalidateMeasure();
    }
  }
}
