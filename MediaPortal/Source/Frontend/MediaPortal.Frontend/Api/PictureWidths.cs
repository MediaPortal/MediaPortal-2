#region Copyright (C) 2007-2021 Team MediaPortal

/*
    Copyright (C) 2007-2021 Team MediaPortal
    http://www.team-mediaportal.com

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

#endregion

using System;
using System.Globalization;

namespace MediaPortal.Frontend.Api
{
  /// <summary>
  /// How wide this front end paints a picture of each sort, which is what it says to the server when asking for it.
  /// </summary>
  /// <remarks>
  /// <para>
  /// <b>Why the front end does the saying.</b> Only this program knows what it is about to paint: the server knows
  /// what a title's poster is, not whether a television six feet away is going to hang it in a wall frame five
  /// hundred and seventy three device pixels wide or file it in a grid tile two hundred and seventeen across. The
  /// reference drew the line in exactly this place - its client asked
  /// <c>/FanartService?…&amp;width=…&amp;height=…</c> out of a skin that knew the size of its own frames
  /// (FanArtImageSource.cs:236), and the server kept the resized copies.
  /// </para>
  /// <para>
  /// <b>Why it matters.</b> A library's poster is a thousand to two thousand pixels wide. Without the number the
  /// front end drags that file across the house, spends forty milliseconds of its own drawing thread decoding it
  /// down, and pays for the whole of it again on the next screen. Measured over one walk along a shelf of films:
  /// thirty-one posters, two and four tenths megapixels the piece, seventy five megapixels decoded to paint nine
  /// covers. With the number said, the server sends a fifth of the bytes once and everybody afterwards reads that.
  /// </para>
  /// <para>
  /// The three numbers are this front end's own decode ceilings (<c>ArtworkConverter.WALL_DECODE_WIDTH</c>,
  /// <c>TILE_DECODE_WIDTH</c> and <c>POSTER_DECODE_WIDTH</c>), so a picture arrives at the size it would have been
  /// decoded to anyway. They are not rounded up to the server's ladder: the server answers a number with the
  /// smallest step that covers it (<c>FanArtVariants</c> on the server), and asking for the width actually
  /// painted is what keeps that step from ever being stretched.
  /// </para>
  /// </remarks>
  public static class PictureWidths
  {
    /// <summary>
    /// A cover the size of the wall's frames: the widest thing a shelf paints INSIDE its frames.
    /// </summary>
    public const int COVER = 640;

    /// <summary>
    /// A picture that fills the whole screen behind everything - the backdrop a wall and a shelf stand on.
    /// </summary>
    /// <remarks>
    /// Measured, the hard way: the backdrop once asked at COVER and every viewer saw it blown up three to five
    /// times across the television - a fanart at six hundred and forty pixels stretched over a screen twice
    /// that wide is a blur by arithmetic.  The backdrop's frame is the screen, and the screen of a media centre
    /// is a full high definition one; asking nineteen hundred and twenty lets the server's ladder answer the
    /// two thousand and forty-eighth step for the big originals and the picture itself for everything smaller
    /// (a variant is never grown), which is the sharpest copy the ladder can hand and still a bounded one.
    /// </remarks>
    public const int BACKGROUND = 1920;

    /// <summary>
    /// A picture inside a grid tile, which is the same poster two thirds smaller.
    /// </summary>
    public const int TILE = 432;

    /// <summary>
    /// A picture that is only a hint of one: a person's portrait in a card, a film's poster beside a collection's
    /// name, a title's lettering or a disc - all of them drawn inside two hundred and forty pixels.
    /// </summary>
    public const int SMALL = 256;

    /// <summary>
    /// The width the backdrop is actually painted at right now: the window's own pixel width.
    /// </summary>
    /// <remarks>
    /// The backdrop's frame is the window, and the reference asked the size of the frame it painted
    /// (FanArtImageSource.cs:236) - so this says the same thing the reference said on every screen.  On a
    /// nineteen-hundred-and-twenty television that answers nineteen hundred and twenty; on a thirty-four hundred
    /// ultra-wide it answers thirty-four hundred, and the server's ladder then hands the original for everything
    /// narrower (a variant is never grown), so the picture is stretched by the window's arithmetic only, never by
    /// the asking.  Measured on this machine: asked at nineteen hundred and twenty, a full high definition
    /// backdrop was still blown up one and eight tenths times across the screen and the viewer called it blurry -
    /// which is why the asking reads the window rather than a constant.  While no window is there to read -
    /// during startup, or in a headless run - BACKGROUND is the answer.
    /// </remarks>
    public static int PaintedScreenWidth()
    {
      if (Avalonia.Application.Current?.ApplicationLifetime
              is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime
              { MainWindow: { } window }
          && window.ClientSize.Width > 0)
        return (int) Math.Round(window.ClientSize.Width * Math.Max(window.RenderScaling, 1.0));

      return BACKGROUND;
    }

    /// <summary>
    /// The name of the local shelf one kind's bytes lie in, said at the width they were asked at.
    /// </summary>
    /// <remarks>
    /// The picture shelf keys by kind alone, so a poster asked wide and a poster asked small would share one
    /// folder and answer for each other - which is also how a title's database thumbnail used to be able to answer
    /// a question about its fanart thumbnail, those two kinds having the same name. Naming the size makes the
    /// shelf tell the copies apart, and makes a viewer who walks from the wall into the grid read the copy that
    /// fits the frame instead of the copy that happens to be lying there.
    /// </remarks>
    public static string Shelf(string kind, int width)
    {
      return kind + "-" + width.ToString(CultureInfo.InvariantCulture);
    }
  }
}
