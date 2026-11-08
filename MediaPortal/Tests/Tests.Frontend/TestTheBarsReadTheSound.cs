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

using System;
using NUnit.Framework;
using MediaPortal.Frontend.Player;

namespace Tests.Frontend
{
  /// <summary>
  /// Reading the loudness of each band out of the picture libVLC draws, which is the only account libVLC keeps of
  /// the sound it plays.
  /// </summary>
  /// <remarks>
  /// The reference's equalizer counted the sound itself: Controls/AudioSpectrumAnalyzer.cs ran an FFT over samples
  /// the BASS player handed it and spread the bins over its bars by an axis (line 411). libVLC hands no samples to
  /// anybody - the callbacks of MediaPlayer.SetAudioCallbacks stay silent through a whole song on the engine and
  /// library this frontend ships, measured with a probe - so the bars are measured off the spectrum module's own
  /// picture instead. What that picture means was measured and not guessed, as the remarks of SpectrumPicture
  /// tells: the width is the frequency axis, and a band's loudness is the <em>brightness</em> of its slice. The
  /// height of a bar's top edge was measured too and is not it: through three seconds of digital silence it kept
  /// standing at nine tenths of its loud height, because the filter paints a slowly falling peak marker above
  /// every bar and an edge reading answers for that marker rather than for the sound.
  /// </remarks>
  [TestFixture]
  public class TestTheBarsReadTheSound
  {
    /// <summary>The size the listener asks libVLC for, and the bar count the bars of the screen are built with.</summary>
    private const int Width = 640;
    private const int Height = 300;
    private const int Bands = 32;

    /// <summary>
    /// A picture whose every band's slice holds the wanted brightness: just so many of its pixels are lit wholly
    /// and the rest is black, since the filter's picture is made of lit and dark pixels too.
    /// </summary>
    private static byte[] PictureOfBrightness(float[] fractions)
    {
      byte[] pixels = new byte[Width * Height * 4];
      for (int band = 0; band < fractions.Length; band++)
      {
        int first = (int) (band * (long) Width / fractions.Length);
        int last = (int) ((band + 1) * (long) Width / fractions.Length);
        int toLight = (int) Math.Round(fractions[band] * (last - first) * Height);
        int lit = 0;
        for (int y = 0; y < Height && lit < toLight; y++)
        {
          for (int x = first; x < last && lit < toLight; x++, lit++)
          {
            int at = (y * Width + x) * 4;
            pixels[at] = 255;
            pixels[at + 1] = 255;
            pixels[at + 2] = 255;
            pixels[at + 3] = 255;
          }
        }
      }

      return pixels;
    }
    /// <summary>
    /// The bar a band's brightness has to be answered with: what is left of it above the floor silence leaves,
    /// times the gain that puts loud material at full height. The same two numbers the reading works with.
    /// </summary>
    private static float Wanted(int band, int slices, float brightness)
    {
      float floor = SpectrumPicture.SilenceBrightness
          + (slices > 1 ? SpectrumPicture.SilenceBrightnessRise * band / (slices - 1) : 0f);
      float gain = 1f / (SpectrumPicture.LoudBrightness - SpectrumPicture.SilenceBrightness);
      return Math.Clamp((brightness - floor) * gain, 0f, 1f);
    }

    /// <summary>What silence leaves standing in a band: measured 0.066 at the deep end, 0.082 at the high end.</summary>
    private static float SilenceOf(int band, int slices)
    {
      return SpectrumPicture.SilenceBrightness
          + (slices > 1 ? SpectrumPicture.SilenceBrightnessRise * band / (slices - 1) : 0f);
    }

    private static float[] Read(byte[] picture)
    {
      float[] levels = new float[Bands];
      SpectrumPicture.BandsFrom(picture, Width, Height, levels);
      return levels;
    }

    [Test]
    public void LoudMaterialStandsItsBarAtFullHeight()
    {
      float[] loud = new float[Bands];
      for (int band = 0; band < Bands; band++)
        loud[band] = SpectrumPicture.LoudBrightness;

      float[] read = Read(PictureOfBrightness(loud));
      for (int band = 0; band < Bands; band++)
        Assert.That(read[band], Is.GreaterThan(0.9f),
            "band " + band + " was given what loud material measures and read " + read[band]);
    }

    [Test]
    public void WhatSilenceLeavesStandingIsAnsweredAsNoSound()
    {
      // The picture of a stopped song is not black: peak marker, base line and grid glow a little, more at the
      // high end. A reading that called that brightness sound would have every bar stand up while nothing plays.
      float[] quiet = new float[Bands];
      for (int band = 0; band < Bands; band++)
        quiet[band] = SilenceOf(band, Bands);

      float[] read = Read(PictureOfBrightness(quiet));
      for (int band = 0; band < Bands; band++)
        Assert.That(read[band], Is.EqualTo(0f).Within(0.02f),
            "band " + band + " was made to stand by what silence leaves");
    }

    [Test]
    public void ABlackPictureIsAnsweredAsNoSoundAtAll()
    {
      float[] read = Read(new byte[Width * Height * 4]);
      for (int band = 0; band < Bands; band++)
        Assert.That(read[band], Is.EqualTo(0f), "band " + band + " stood up out of a black picture");
    }

    [Test]
    public void HalfwayBetweenSilenceAndLoudnessIsHalfABar()
    {
      float[] half = new float[Bands];
      for (int band = 0; band < Bands; band++)
        half[band] = SilenceOf(band, Bands) + (SpectrumPicture.LoudBrightness - SilenceOf(band, Bands)) / 2;

      float[] read = Read(PictureOfBrightness(half));

      // At the deep end the floor of silence is the plain number, so half the way is half a bar exactly.
      Assert.That(read[0], Is.EqualTo(0.5f).Within(0.02f), "the deep end was given half the way up and read " + read[0]);

      // Towards the high end silence stands higher, so half the way there is a little less than half a bar -
      // the tilt is measured, and what must not happen is a band falling out of the scale because of it.
      for (int band = 1; band < Bands; band++)
        Assert.That(read[band], Is.InRange(0.4f, 0.6f),
            "band " + band + " was given half the way up and read " + read[band]);
    }

    [Test]
    public void ABrightBandBesideDarkOnesIsToldApart()
    {
      // One tone of a known frequency fills one slice and leaves its neighbours at the floor, which is how the
      // reading has to keep the shape of the sound rather than its average.
      float[] one = new float[Bands];
      for (int band = 0; band < Bands; band++)
        one[band] = SilenceOf(band, Bands);
      one[7] = SpectrumPicture.LoudBrightness;

      float[] read = Read(PictureOfBrightness(one));
      Assert.That(read[7], Is.GreaterThan(0.9f), "the band the tone filled stayed down");
      Assert.That(read[6], Is.LessThan(0.05f), "the band left of it was made to stand by its neighbour");
      Assert.That(read[8], Is.LessThan(0.05f), "the band right of it was made to stand by its neighbour");
    }

    [Test]
    public void AFaintLineAcrossThePictureIsNotTakenForSound()
    {
      // The module does not only draw bars: it draws faint lines where its bands are separated, and a reading that
      // counted every bright pixel would stand those lines up as a wall of sound.
      byte[] pixels = new byte[Width * Height * 4];
      for (int y = 0; y < Height; y++)
      {
        int at = (y * Width + Width / 2) * 4;
        pixels[at] = 6;
        pixels[at + 1] = 6;
        pixels[at + 2] = 6;
        pixels[at + 3] = 255;
      }

      float[] read = Read(pixels);
      for (int band = 0; band < Bands; band++)
        Assert.That(read[band], Is.LessThan(0.1f), "band " + band + " was made to stand by a faint line");
    }

    [Test]
    public void APictureOfAnotherSizeIsReadOverAllOfItsWidth()
    {
      // The listener asks for one size, but nothing in libVLC promises it. The reading is told the size it is
      // looking at and has to use all of it rather than a size it remembered.
      const int smallWidth = 160;
      const int smallHeight = 90;
      byte[] pixels = new byte[smallWidth * smallHeight * 4];
      byte glow = (byte) Math.Round(0.2 * 255);
      for (int x = 0; x < smallWidth; x++)
      {
        for (int y = 0; y < smallHeight; y++)
        {
          int at = (y * smallWidth + x) * 4;
          pixels[at] = glow;
          pixels[at + 1] = glow;
          pixels[at + 2] = glow;
          pixels[at + 3] = 255;
        }
      }

      float[] levels = new float[Bands];
      SpectrumPicture.BandsFrom(pixels, smallWidth, smallHeight, levels);
      for (int band = 0; band < Bands; band++)
        Assert.That(levels[band], Is.EqualTo(Wanted(band, Bands, 0.2f)).Within(0.02f), "band " + band);
    }

    [Test]
    public void AnyNumberOfSlicesIsSpreadOverTheWholePicture()
    {
      // The control asks for its own thirty two slices and the listener hands them over, but the reading is not
      // tied to that number: fewer slices means wider ones over the same picture, not a picture read in part, and
      // the floor of silence is spread over the slices the answer is cut into.
      byte[] flat = PictureOfBrightness(new float[] { 0.2f });
      float[] coarse = new float[8];
      SpectrumPicture.BandsFrom(flat, Width, Height, coarse);
      for (int slice = 0; slice < coarse.Length; slice++)
        Assert.That(coarse[slice], Is.EqualTo(Wanted(slice, coarse.Length, 0.2f)).Within(0.02f),
            "slice " + slice);
    }
  }
}