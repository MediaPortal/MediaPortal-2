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
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace MediaPortal.Frontend.Views
{
  /// <summary>
  /// Moves the left edge of a margin the skin drew to wherever the screen says the content begins, and leaves
  /// the rest of that margin exactly as the skin wrote it.
  /// </summary>
  /// <remarks>
  /// <para>
  /// A skin writes its margins as numbers it drew - thirty-four under the line of text under the tiles, ten
  /// above the buttons at the top - and those belong to the face and stay in its file. What does not belong to
  /// the face is how wide the menu rail turned out to be on this particular panel, because that is what the
  /// screen measures when it lays the rail out. A margin whose left edge stands after the rail is therefore
  /// half the skin's and half the screen's, and this is the one place the two meet: the bound value is the
  /// measured inset, the parameter is the margin as the skin drew it, and only its left edge is replaced.
  /// </para>
  /// <para>
  /// The parameter is read here rather than by Avalonia's own parser so that a face's file never depends on
  /// what culture a test runner happens to use: the four numbers are separated by commas and read as written,
  /// the way every other number in a skin file is.
  /// </para>
  /// </remarks>
  public sealed class SkinInsetConverter : IValueConverter
  {
    /// <summary>The one instance templates bind with; see <see cref="SkinImageConverter.Instance"/>.</summary>
    public static readonly SkinInsetConverter Instance = new SkinInsetConverter();

    private SkinInsetConverter()
    {
    }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
      double left = value is double number && !double.IsNaN(number)
          ? Math.Max(0, number)
          : 0;

      if (parameter is string drawn)
      {
        string[] parts = drawn.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1 && Read(parts[0], out double all))
          return new Thickness(left, all, all, all);
        if (parts.Length == 2 && Read(parts[0], out double vertical) && Read(parts[1], out double horizontal))
          return new Thickness(left, vertical, horizontal, vertical);
        if (parts.Length == 4
            && Read(parts[1], out double top) && Read(parts[2], out double right) && Read(parts[3], out double bottom))
        {
          return new Thickness(left, top, right, bottom);
        }
      }

      return new Thickness(left, 0, 0, 0);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
      throw new NotSupportedException();
    }

    /// <summary>One number of a margin, read the way a skin file writes it and no other way.</summary>
    private static bool Read(string text, out double number)
    {
      return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out number);
    }
  }
}
