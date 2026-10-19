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
using MediaPortal.Frontend.Theming;

namespace MediaPortal.Frontend.Views
{
  /// <summary>
  /// Turns a skin picture's name into the picture itself, read from the skin the application is wearing.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The reference's screens never named a picture's path; they named the picture - <c>Square_small.png</c> -
  /// and the skin engine answered from whichever skin was on. That is the arrangement <see cref="SkinImages"/>
  /// keeps, and this converter is the end a template reaches: the view model hands over a name and the worn
  /// skin hands over a picture, so no view model ever holds a bitmap and no view ever goes looking in a folder.
  /// </para>
  /// <para>
  /// <b>The name is spelled per skin, not per entry.</b> The binding's parameter is the format the skin uses -
  /// the home tiles are one file's bare Id in Nereus and its <c>Focus</c> variant in WMC - which is exactly the
  /// reference's division: what a picture <i>is</i> belongs to the model, how its name is <i>spelled</i>
  /// belongs to the skin. A format of <c>"{0}"</c> takes the name as it is; anything in braces in the parameter
  /// is filled with the bound value.
  /// </para>
  /// <para>
  /// Null out is the honest answer as often as a picture is: no skin worn, no name bound, no picture of that
  /// name in the skin, or a file that will not read. A tile then draws without its picture - the templates keep
  /// the letter badge for exactly that case - rather than showing a broken frame, because a media centre is
  /// judged by whether it ever shows a hole.
  /// </para>
  /// </remarks>
  public sealed class SkinImageConverter : IValueConverter
  {
    /// <summary>
    /// The one instance templates bind with. A converter holds no state, and an instance per tile would be a
    /// hundred allocations to say the same sentence.
    /// </summary>
    public static readonly SkinImageConverter Instance = new SkinImageConverter();

    private SkinImageConverter()
    {
    }

    /// <summary>
    /// The name a binding asks the worn skin for: the bound value written into the parameter's format. Null
    /// when the binding gave no name, or the parameter is not a format <c>string.Format</c> can survive.
    /// </summary>
    internal static string? ImageName(object? value, object? parameter)
    {
      if (value is not string name || name.Length == 0)
        return null;

      string format = parameter as string ?? "{0}";
      try
      {
        return string.Format(CultureInfo.InvariantCulture, format, name);
      }
      catch (FormatException)
      {
        // A template whose parameter does not format is a typo in a skin file, not a reason to stop the
        // television: the tile answers without a picture, like one whose name the skin has never heard.
        return null;
      }
      catch (IndexOutOfRangeException)
      {
        return null; // the format asks for an argument this binding does not have
      }
    }

    /// <summary>
    /// The pictures the application currently wears, or null while it wears only a palette. The one place both
    /// converters ask the same question, so that "no skin on" means the same thing to both.
    /// </summary>
    internal static WornSkin? WornPictures()
    {
      Application? app = Application.Current;
      return app == null ? null : SkinEngine.Worn(app);
    }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
      string? name = ImageName(value, parameter);
      if (name == null)
        return null;

      WornSkin? worn = WornPictures();
      return worn?.Images.Load(name);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
      throw new NotSupportedException("a skin picture is asked for, never written back");
    }
  }

  /// <summary>
  /// Whether the worn skin has no picture of the bound name - the answer a tile's letter badge binds its
  /// visibility to, so the badge shows exactly when there is no picture to show instead of it.
  /// </summary>
  /// <remarks>
  /// This asks only whether the skin carries the picture, not whether the file reads; a picture that is
  /// registered but unreadable leaves its tile picture-less and badge-less, which is the same hole
  /// <see cref="SkinImageConverter"/> answers without, and rarer by one disk failure on top of one typo.
  /// </remarks>
  public sealed class SkinImageMissingConverter : IValueConverter
  {
    /// <summary>The one instance templates bind with; see <see cref="SkinImageConverter.Instance"/>.</summary>
    public static readonly SkinImageMissingConverter Instance = new SkinImageMissingConverter();

    private SkinImageMissingConverter()
    {
    }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
      string? name = SkinImageConverter.ImageName(value, parameter);
      if (name == null)
        return true;

      WornSkin? worn = SkinImageConverter.WornPictures();
      return worn == null || !worn.Images.Has(name);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
      throw new NotSupportedException("a skin picture is asked for, never written back");
    }
  }
}
