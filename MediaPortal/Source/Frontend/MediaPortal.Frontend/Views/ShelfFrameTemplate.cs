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
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using MediaPortal.Frontend.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaPortal.Frontend.Views
{
  /// <summary>
  /// The frame this file draws for a shape the worn skin does not answer: the skin's tile frame for the titles,
  /// and a divider for the plates a grouping
  /// stands in the row.  A dispatcher rather than a third template, because the wall owns exactly one
  /// <c>ItemTemplate</c> and the grouping's plates share that wall with the titles - and because the titles'
  /// answer must stay the worn skin's business, untouched, whether or not anything is grouped.
  /// </summary>
  /// <remarks>
  /// The plate says its word in white and opacity, like everything else the shelf lights: it is not a title,
  /// takes no caret and wears no selection, so it has no second state and nothing to say in colour. Each shape
  /// of the wall gets the divider that shape can carry: a headline over a strip of rows, a standing card in a
  /// wall of tiles, and a book's spine among covers - three readings of the same one sentence, "here a group
  /// of the shelf begins".
  /// </remarks>
  internal sealed class ShelfFrameTemplate : IRecyclingDataTemplate
  {
    private readonly IDataTemplate _titles;
    private readonly BrowserLayout _layout;

    // The gap a tile leaves around its face, read from the FIRST tile the worn frame ever builds. A plate may
    // not change the spacing of a wall it is dropped into, and every wall in this front end lays its tiles
    // uniformly - so the tiles themselves say what their gap is, and the plate wears exactly that. It is read
    // once, the first time the frame answers a title, and it then belongs to this frame: the dispatcher is
    // rebuilt whenever the frame or the picture changes, and a fresh dispatcher asks the fresh frame afresh.
    private Thickness? _tile_margin;
    private bool _margin_learned;

    public ShelfFrameTemplate(IDataTemplate titles, BrowserLayout layout)
    {
      _titles = titles;
      _layout = layout;
    }

    public bool Match(object? data)
    {
      // The wall lays both kinds of item through this one door; Build answers the difference.
      return data is GroupHeaderRow || _titles.Match(data);
    }

    public Control Build(object? data)
    {
      if (data is GroupHeaderRow plate)
        return BuildPlate(plate);
      Control tile = _titles.Build(data)!;
      LearnMargin(tile);
      return tile;
    }

    // What the worn frame could do with a recycled shell it can still do: the titles pass straight through to
    // it, unchanged. A plate answers for itself only - the card that says "Action" cannot be told to say
    // "Drama" in its sleep - so a plate always arrives as its own fresh card. Saying nothing of recycling at
    // all would have taken the frame's own recycling away from every tile on the wall.
    public Control? Build(object? data, Control? existing)
    {
      if (data is GroupHeaderRow plate)
        return BuildPlate(plate);

      Control? tile = _titles is IRecyclingDataTemplate recycling
          ? recycling.Build(data, existing)
          : _titles.Build(data);
      if (tile != null)
        LearnMargin(tile);
      return tile;
    }

    private Control BuildPlate(GroupHeaderRow plate)
    {
      if (_layout == BrowserLayout.List)
        return RowPlate(plate);
      if (_layout is BrowserLayout.Cover or BrowserLayout.Stacked or BrowserLayout.Carousel)
        return SpinePlate(plate);
      return CardPlate(plate);
    }

    /// <summary>
    /// The first thing the worn frame builds for a title carries the gap every tile of this wall leaves around
    /// its face - the templates hang that gap on their root (a poster breathes 8,8,8,14, a banner 8,6,8,10, the
    /// built-in frame 8 on all sides), and every wall lays its tiles uniformly. Once learned, the gap belongs to
    /// the wall, and every plate wears it, so the spacing between media cannot change when a grouping stands in
    /// it. Plates built before the first tile (a group's headline always leads its tiles) are remembered and
    /// dressed as soon as the answer arrives.
    /// </summary>
    private readonly List<WeakReference<Border>> _plates_awaiting_margin = new List<WeakReference<Border>>();

    private void LearnMargin(Control tile)
    {
      if (_margin_learned)
        return;

      _margin_learned = true;
      _tile_margin = tile.Margin;

      foreach (WeakReference<Border> held in _plates_awaiting_margin)
      {
        if (held.TryGetTarget(out Border? awaiting))
          awaiting.Margin = _tile_margin.Value;
      }
      _plates_awaiting_margin.Clear();
    }

    /// <summary>A headline: the word, its count beside it, and a hairline under the line.</summary>
    private static Control RowPlate(GroupHeaderRow plate)
    {
      StackPanel line = new StackPanel
      {
        Orientation = Orientation.Horizontal,
        Margin = new Thickness(2, 16, 2, 4)
      };

      line.Children.Add(new TextBlock
      {
        Text = plate.Label,
        FontSize = 24,
        FontWeight = FontWeight.SemiBold,
        Foreground = new SolidColorBrush(Color.Parse("#F0FFFFFF")),
        VerticalAlignment = VerticalAlignment.Center
      });

      if (plate.CountWord.Length > 0)
      {
        line.Children.Add(new TextBlock
        {
          Text = plate.CountWord,
          FontSize = 18,
          Opacity = 0.55,
          Foreground = new SolidColorBrush(Color.Parse("#F0FFFFFF")),
          Margin = new Thickness(10, 3, 0, 0),
          VerticalAlignment = VerticalAlignment.Center
        });
      }

      return new StackPanel
      {
        Children =
        {
          line,
          new Border { Height = 1, Background = new SolidColorBrush(Color.Parse("#26FFFFFF")) }
        }
      };
    }

    /// <summary>
    /// A card the measure of the tile beside it - whatever that is this time, poster, thumbnail or banner - with
    /// its word standing in the middle. The card names no size of its own: the wall measures it with the cell its
    /// tiles just made and arranges it into that cell, so a headline is as big as a tile and no bigger. What it
    /// does copy from the tiles is their margin, so the gaps of the wall stay exactly what they were, and their
    /// face - the same shadow and plate the media tiles wear.
    /// </summary>
    private Control CardPlate(GroupHeaderRow plate)
    {
      StackPanel inside = new StackPanel
      {
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center,
        Margin = new Thickness(12)
      };

      inside.Children.Add(new TextBlock
      {
        Text = plate.Label,
        FontSize = 22,
        FontWeight = FontWeight.SemiBold,
        TextAlignment = TextAlignment.Center,
        TextWrapping = TextWrapping.Wrap,
        Foreground = new SolidColorBrush(Color.Parse("#F0FFFFFF"))
      });

      if (plate.CountWord.Length > 0)
      {
        inside.Children.Add(new TextBlock
        {
          Text = plate.CountWord,
          FontSize = 16,
          Opacity = 0.55,
          Foreground = new SolidColorBrush(Color.Parse("#F0FFFFFF")),
          HorizontalAlignment = HorizontalAlignment.Center,
          Margin = new Thickness(0, 6, 0, 0)
        });
      }

      Border face = new Border
      {
        Classes = { "home-tile-face" },
        Background = new SolidColorBrush(Color.Parse("#222222")),
        ClipToBounds = true,
        Child = inside
      };

      Border outer = new Border
      {
        Classes = { "home-tile-shadow" },
        // Until the wall's first tile has answered its gap, breathe like the wall most walls are.
        Margin = _tile_margin ?? new Thickness(8),
        Child = face
      };

      if (!_margin_learned)
        _plates_awaiting_margin.Add(new WeakReference<Border>(outer));

      return outer;
    }

    /// <summary>
    /// A book's spine among covers: the word read from top to bottom, one letter standing on the next, in a
    /// card the width and height of a cover - which is exactly how a divider between albums says which shelf
    /// a run of books belongs to. The letters are stacked by hand rather than wrapped, because a long value
    /// must not climb out of the cover's height - the word's size gives way instead, down to what a viewer
    /// can still read from the sofa.
    /// </summary>
    private static Control SpinePlate(GroupHeaderRow plate)
    {
      string stacked = string.Join("\n", plate.Label.Select(letter => letter.ToString()));
      int lines = plate.Label.Length;
      double size = lines <= 1 ? 30.0 : Math.Clamp(320.0 / lines, 15.0, 28.0);

      TextBlock word = new TextBlock
      {
        Text = stacked,
        FontSize = size,
        FontWeight = FontWeight.SemiBold,
        TextAlignment = TextAlignment.Center,
        Foreground = new SolidColorBrush(Color.Parse("#F0FFFFFF")),
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center
      };

      return new Border
      {
        Width = 260,
        Height = 410,
        Background = new SolidColorBrush(Color.Parse("#0DFFFFFF")),
        BorderBrush = new SolidColorBrush(Color.Parse("#26FFFFFF")),
        BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(10),
        Child = word
      };
    }
  }
}