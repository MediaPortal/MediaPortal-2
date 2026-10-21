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
using Avalonia.Data.Converters;
using Avalonia.Media;
using MediaPortal.Frontend.Theming;

namespace MediaPortal.Frontend.Views
{
  /// <summary>
  /// Turns a skin icon's name into the shape the worn skin carries for it, read from the skin the application
  /// is wearing.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The twin of <see cref="SkinImageConverter"/>, for the icons a skin ships as vectors rather than as
  /// pictures: the view model names the icon, the worn skin answers with a geometry, and no template goes
  /// looking in a folder. The name is spelled the same way - the binding's parameter is the format, so a skin
  /// that wants its icons under a prefix says so in the template - and it is answered out of the skin's
  /// vector catalog, never out of its picture catalog.
  /// </para>
  /// <para>
  /// Null when the front end wears no skin, nothing is bound, the skin carries no icon of that name, or the
  /// file holds nothing paintable. A plaque without its glyph is the honest result; a glyph borrowed from the
  /// picture catalog is not, because a bitmap cannot be tinted, thickened or lit the way a shape can.
  /// </para>
  /// </remarks>
  public sealed class SkinIconGeometryConverter : IValueConverter
  {
    /// <summary>The one instance templates bind with; see <see cref="SkinImageConverter.Instance"/>.</summary>
    public static readonly SkinIconGeometryConverter Instance = new SkinIconGeometryConverter();

    private SkinIconGeometryConverter()
    {
    }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
      string? name = SkinImageConverter.ImageName(value, parameter);
      if (name == null)
        return null;

      WornSkin? worn = SkinImageConverter.WornPictures();
      return worn?.Vectors.Load(name);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
      throw new NotSupportedException("an icon is asked for, never written back");
    }
  }

  /// <summary>
  /// Whether the worn skin carries no vector for the bound icon name - which is how a tile decides to say so
  /// rather than standing there with an empty plaque.
  /// </summary>
  public sealed class SkinIconMissingConverter : IValueConverter
  {
    /// <summary>The one instance templates bind with.</summary>
    public static readonly SkinIconMissingConverter Instance = new SkinIconMissingConverter();

    private SkinIconMissingConverter()
    {
    }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
      string? name = SkinImageConverter.ImageName(value, parameter);
      if (name == null)
        return true;

      WornSkin? worn = SkinImageConverter.WornPictures();
      return worn == null || !worn.Vectors.Has(name);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
      throw new NotSupportedException("an icon is asked for, never written back");
    }
  }
}
