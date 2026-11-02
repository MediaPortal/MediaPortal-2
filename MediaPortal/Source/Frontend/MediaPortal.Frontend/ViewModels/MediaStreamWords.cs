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
using System.Globalization;
using MediaPortal.Frontend.Api;

namespace MediaPortal.Frontend.ViewModels
{
  /// <summary>
  /// What the information screen says about the tracks a file carries, in the words a viewer reads.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The reference drew most of these facts as <b>pictures</b>: a row of six logos above the two track lists, and
  /// one card per audio track and per subtitle track below (<c>MediaInfoVideo.xaml:152-296</c> for the logos,
  /// <c>:309-380</c> for the lists, and the cards at
  /// <c>themes/default/styles/CurrentlyPlayingStyles.xaml:45-192</c>). Those pictures are this front end's now too
  /// (<c>Skins\Nereus\images\MediaLogos\</c>, asked for by <see cref="MediaStreamLogos"/>), so what this class
  /// holds is the other half: the figures the reference wrote as <b>words</b> - bitrate, sample rate, frame rate,
  /// "1.78:1", a language's name, the word "Forced" - and the wording a cell falls back to when the worn skin
  /// carries no picture of the name <see cref="MediaStreamLogos"/> asked for.
  /// </para>
  /// <para>
  /// Which thresholds group a picture into a class, which word stands for a number of channels, and what is left
  /// unsaid when the importer did not write a figure are all the reference's own.
  /// </para>
  /// <para>
  /// Everything on this class is arithmetic on figures the server already handed over, so it is testable without
  /// a server and without a screen.
  /// </para>
  /// </remarks>
  public static class MediaStreamWords
  {
    /// <summary>
    /// The word for how big a picture is, at the sizes the reference's own logo row sorted them into
    /// (<c>MediaInfoVideo.xaml:210-229</c>, which chose between the pictures named "240", "360", "480", "576",
    /// "720p", "1080p", "2160p" and "4320p" by exactly these bounds).
    /// </summary>
    /// <param name="width">
    /// The picture's width as the importer wrote it, or nought when it wrote none.
    /// </param>
    /// <param name="height">
    /// The picture's height, same condition.
    /// </param>
    /// <remarks>
    /// The sort is the reference's and both numbers matter, because a standard-definition picture is sorted by its
    /// width as much as by its height: a 720×576 DVD is "576" and not "720p", which is why the bounds are not a
    /// single ladder of heights. The player's tag line (<c>NowPlayingViewModel.ResolutionTag</c>) says the same
    /// fact coarser - it is built from a live frame rather than from the library's record, and it says "4K" where
    /// this says "2160p" - so the two are worded differently on purpose and are not two answers to one question.
    /// </remarks>
    public static string Resolution(int width, int height)
    {
      // The reference drew no logo at all when the width was missing (MediaInfoVideo.xaml:210), so neither does this.
      if (width <= 0 || height <= 0)
        return string.Empty;

      if (height < 320)
        return "240";
      if (height < 450)
        return "360";
      if (height < 485 && width < 725)
        return "480";
      if (width < 725 && height < 580)
        return "576";
      if (width < 1285)
        return "720p";
      if (width < 1925)
        return "1080p";
      if (width < 3845)
        return "2160p";
      return "4320p";
    }

    /// <summary>
    /// The proportion a picture has, in the class a viewer names it by: "4:3", "16:9" or "21:9".
    /// </summary>
    /// <param name="aspectRatio">
    /// The proportion as the importer measured it (width over height), or nought when it wrote none.
    /// </param>
    /// <remarks>
    /// The reference wrote these three classes and these two bounds (<c>MediaInfoVideo.xaml:229-238</c>: under
    /// 1.4 "4:3", under 1.8 "16:9", otherwise "21:9") but handed the comparison its <b>height</b> instead of its
    /// proportion, so its label stood at "21:9" for every picture in the house. The classes and the bounds are
    /// kept and the value they are compared against is the one the sentence was always about.
    /// </remarks>
    public static string RatioClass(float aspectRatio)
    {
      if (aspectRatio <= 0f)
        return string.Empty;
      if (aspectRatio < 1.4f)
        return "4:3";
      if (aspectRatio < 1.8f)
        return "16:9";
      return "21:9";
    }

    /// <summary>
    /// The proportion as a number, "1.78:1", which is what the reference printed over the class
    /// (<c>MediaInfoVideo.xaml:224-226</c>, "{0:F2}:1").
    /// </summary>
    public static string RatioFigure(float aspectRatio)
    {
      if (aspectRatio <= 0f)
        return string.Empty;
      return aspectRatio.ToString("F2", CultureInfo.InvariantCulture) + ":1";
    }

    /// <summary>
    /// The word for a number of audio channels, as the trade writes it: "2.0", "5.1", "7.1".
    /// </summary>
    /// <remarks>
    /// The reference picked a logo by the plain count (<c>CurrentlyPlayingStyles.xaml:90-95</c>,
    /// <c>Medialogos\audio\{0}.png</c>), so the file for six channels was the picture of a 5.1 arrangement. Spelled
    /// out, six channels are read "5.1" - five around and one for the low frequencies - and a count this class does
    /// not know is said as the count rather than as a guess at an arrangement.
    /// </remarks>
    public static string Channels(int count)
    {
      switch (count)
      {
        case 1: return "1.0";
        case 2: return "2.0";
        case 3: return "2.1";
        case 4: return "4.0";
        case 5: return "5.0";
        case 6: return "5.1";
        case 7: return "6.1";
        case 8: return "7.1";
        default: return count <= 0 ? string.Empty : count.ToString(CultureInfo.InvariantCulture);
      }
    }

    /// <summary>
    /// A bitrate in the unit the library keeps them in, which is a thousand bits a second
    /// (<c>VideoMetadataExtractor.cs:472</c>, "We store kbit/s"), as the reference wrote it: "{0} kbps".
    /// </summary>
    public static string BitRate(long kiloBitsPerSecond)
    {
      if (kiloBitsPerSecond <= 0)
        return string.Empty;
      return kiloBitsPerSecond.ToString(CultureInfo.InvariantCulture) + " kbps";
    }

    /// <summary>
    /// A sample rate as the reference wrote it, "{0:F0} Hz"
    /// (<c>CurrentlyPlayingStyles.xaml:113-118</c>) - the library keeps hertz and does not round them to kilohertz.
    /// </summary>
    public static string SampleRate(long hertz)
    {
      if (hertz <= 0)
        return string.Empty;
      return hertz.ToString("F0", CultureInfo.InvariantCulture) + " Hz";
    }

    /// <summary>
    /// How often a picture changes, whole: "24 fps".
    /// </summary>
    /// <remarks>
    /// Said away plainly, because this is one of the rare places where the front end does NOT copy the worn skin:
    /// the reference wrote three decimals ("<c>{0:F3} fps</c>", <c>MediaInfoVideo.xaml:250-254</c>), which read
    /// "23.976 fps" and "24.000 fps" side by side for two files that change pictures at the same speed for every
    /// practical purpose.  The viewer asked for the whole number instead (2026-11-02), so 23.976 and 24.000 both
    /// answer "24 fps" here.  What is given up is the distinction between 23.976 and 24 - a figure the library
    /// still keeps, so nothing is lost but the showing of it.
    /// </remarks>
    public static string FrameRate(float framesPerSecond)
    {
      if (framesPerSecond <= 0f)
        return string.Empty;
      return Math.Round(framesPerSecond).ToString("F0", CultureInfo.InvariantCulture) + " fps";
    }

    /// <summary>
    /// What one audio track is called on a card: the language it is in, how many channels it carries, what its
    /// codec is, and its two numbers - each of them twice, once as the word a viewer reads and once as the name of
    /// the picture the reference drew instead.
    /// </summary>
    /// <remarks>
    /// The four corners of the reference's audio card, in its order (<c>CurrentlyPlayingStyles.xaml:45-120</c>):
    /// flag and language at the top left - the flag picked by the language, the language itself written in capitals
    /// by the skin's own case converter - the channels at the top right as a loudspeaker picture of the plain
    /// count, the codec at the bottom left as a logo, and the bitrate over the sample rate at the bottom right as
    /// the two words they still are here.
    /// </remarks>
    public static AudioTrackCard AudioCard(AudioStreamInfo audio)
    {
      if (audio == null)
        return null;

      string language = audio.Language?.Trim() ?? string.Empty;
      string encoding = audio.Encoding?.Trim() ?? string.Empty;
      return new AudioTrackCard(
          language.Length > 0 ? language.ToUpperInvariant() : string.Empty,
          Channels(audio.Channels),
          encoding,
          Joined(BitRate(audio.BitRate), SampleRate(audio.SampleRate)),
          MediaStreamLogos.Flag(language),
          MediaStreamLogos.Channels(audio.Channels),
          MediaStreamLogos.AudioCodec(encoding));
    }

    /// <summary>
    /// What one subtitle track is called on a card: the language it is in, whether it lies inside the file, its
    /// format, and whether the file forces it.
    /// </summary>
    /// <remarks>
    /// Again the reference's four corners (<c>CurrentlyPlayingStyles.xaml:126-192</c>): flag and language, the
    /// internal-or-external <b>picture</b>, the format in its picture of a frame, and the word "Forced" for a
    /// track the file insists on - the one word of that card the reference did not have a picture for. The
    /// language is written here as the file wrote it - the reference upper-cased the audio language and left the
    /// subtitle language alone, and there is no reason to know better.
    /// </remarks>
    public static SubtitleTrackCard SubtitleCard(SubtitleStreamInfo subtitle)
    {
      if (subtitle == null)
        return null;

      string language = subtitle.Language?.Trim() ?? string.Empty;
      return new SubtitleTrackCard(
          language,
          subtitle.IsInternal ? "intern" : "extern",
          subtitle.Format?.Trim() ?? string.Empty,
          subtitle.IsForced ? "Forced" : string.Empty,
          MediaStreamLogos.Flag(language),
          MediaStreamLogos.Carriage(subtitle.IsInternal));
    }

    private static string Joined(string first, string second)
    {
      if (first.Length > 0 && second.Length > 0)
        return first + " · " + second;
      return first.Length > 0 ? first : second;
    }
  }

  /// <summary>
  /// One audio track, in the four facts a card of it is built from - each of them as a word and as the name of the
  /// picture the reference drew for it.
  /// </summary>
  /// <param name="Language">The language in capitals, empty when the file named none.</param>
  /// <param name="Channels">"5.1" and the like, empty when the file counted none.</param>
  /// <param name="Encoding">The codec as the file wrote it.</param>
  /// <param name="Detail">Bitrate and sample rate in one line.</param>
  /// <param name="FlagToken">The language as a flag's file name; empty when the file named no language, which the
  /// skin answers with its unknown flag.</param>
  /// <param name="ChannelsToken">The channel <b>count</b> as a loudspeaker picture's file name ("6" for a 5.1
  /// arrangement, which is how the reference's catalog is filed).</param>
  /// <param name="CodecToken">The codec as a logo's file name.</param>
  public sealed record AudioTrackCard(
      string Language,
      string Channels,
      string Encoding,
      string Detail,
      string FlagToken,
      string ChannelsToken,
      string CodecToken)
  {
    /// <summary>Whether the file named a language, which decides between its flag and the unknown one.</summary>
    public bool ShowsFlag => FlagToken.Length > 0;

    /// <summary>Whether the card asks the skin for its unknown-language flag instead (<c>CurrentlyPlayingStyles.xaml:83-84</c>).</summary>
    public bool ShowsUnknownFlag => FlagToken.Length == 0;

    /// <summary>Whether the importer counted channels.</summary>
    public bool ShowsChannels => ChannelsToken.Length > 0;

    /// <summary>Whether the importer named a codec.</summary>
    public bool ShowsCodec => CodecToken.Length > 0;
  }

  /// <summary>
  /// One subtitle track, in the four facts a card of it is built from, again as words and as picture names.
  /// </summary>
  /// <param name="Language">The language as the file wrote it.</param>
  /// <param name="Carriage">"intern" or "extern", for the case the skin carries no carriage picture.</param>
  /// <param name="Format">"SRT", "ASS" and the like, which the reference wrote inside a picture of a frame.</param>
  /// <param name="Forced">The word "Forced" for a track the file insists on, else empty.</param>
  /// <param name="FlagToken">The language as a flag's file name, empty when none was named.</param>
  /// <param name="CarriageToken">"Internal" or "External", the two pictures the reference had for this fact.</param>
  public sealed record SubtitleTrackCard(
      string Language,
      string Carriage,
      string Format,
      string Forced,
      string FlagToken,
      string CarriageToken)
  {
    /// <summary>Whether the file named a language, which decides between its flag and the unknown one.</summary>
    public bool ShowsFlag => FlagToken.Length > 0;

    /// <summary>Whether the card asks for the skin's unknown-language flag instead.</summary>
    public bool ShowsUnknownFlag => FlagToken.Length == 0;

    /// <summary>Whether the importer named a format, which is the case that wears the picture of a frame.</summary>
    public bool ShowsFormat => Format.Length > 0;

    /// <summary>Whether the file insists on this track.</summary>
    public bool ShowsForced => Forced.Length > 0;
  }
}
