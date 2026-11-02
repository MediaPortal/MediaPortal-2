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
using System.Collections.Generic;
using System.IO;
using MediaPortal.Frontend.Api;

namespace MediaPortal.Frontend.ViewModels
{
  /// <summary>
  /// The figure a screen asks a picture for, spelled the way the skin's picture folders hold it.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The reference drew these facts as <b>pictures</b>, not words: its logo row picked a file per codec, per
  /// resolution and per proportion (<c>MediaInfoVideo.xaml:176-255</c>) and its track cards picked a flag per
  /// language, a loudspeaker picture per channel count and a logo per codec
  /// (<c>themes/default/styles/CurrentlyPlayingStyles.xaml:76-190</c>). Every one of those bindings handed a
  /// <i>value</i> to the skin and let the skin spell the file - <c>Medialogos\flags\{0}.png</c> is written in the
  /// style, not in the model - which is the same division <see cref="Views.SkinImageConverter"/> keeps here: this
  /// class says what a thing is worth, the skin file says which folder its picture stands in.
  /// </para>
  /// <para>
  /// <b>Only the name is answered, never the picture.</b> A picture belongs to the worn skin and a name belongs to
  /// the figure, so a name is all this returns; whether a picture of that name exists is the skin's answer and the
  /// view asks it with <see cref="Views.SkinImageMissingConverter"/>, which is how a codec the old skin never
  /// carried falls back to being written in words instead of drawing a hole.
  /// </para>
  /// <para>
  /// Naming is the reference's <c>SafeFilenameConverter</c> and nothing else: characters a file name cannot carry
  /// become <c>_</c> (<c>Source\UI\SkinEngine\MpfElements\Converters\SafeFilenameConverter.cs:39-52</c>). Case is
  /// <b>not</b> touched - the reference never touched it, and its picture catalog answers
  /// <c>flags\DE.png</c> and <c>flags\de.png</c> from one file - so a library that wrote "DE" and one that wrote
  /// "de" both stand at the same flag.
  /// </para>
  /// <para>
  /// Only two figures go one step further than the value, because the reference's straight binding misses the
  /// popular cases there and misses them silently: <see cref="Flag"/> and <see cref="AudioCodec"/> consult a
  /// short table first. Both tables are <b>ours</b>, are marked as such where they stand, and never rewrite a
  /// value the picture folder already answers.
  /// </para>
  /// </remarks>
  public static class MediaStreamLogos
  {
    /// <summary>
    /// A value written so that it can stand in a file name, which is the reference's one trick: every character
    /// <c>Path.GetInvalidFileNameChars()</c> dislikes becomes an underscore, everything else is left alone.
    /// </summary>
    /// <remarks>
    /// Left alone includes spaces and dots on purpose: the old skin's own catalog is full of them
    /// (<c>MediaLogos\audio\mpeg audio version 1 layer 3.png</c>), and a name this class tidied further would
    /// ask for a picture no skin has ever carried.
    /// </remarks>
    public static string SafeName(string? value)
    {
      string name = value?.Trim() ?? string.Empty;
      if (name.Length == 0)
        return string.Empty;

      char[] unsafe_ = Path.GetInvalidFileNameChars();
      foreach (char bad in unsafe_)
      {
        if (name.IndexOf(bad) >= 0)
          name = name.Replace(bad, '_');
      }

      return name;
    }

    /// <summary>
    /// The flag a language is drawn with, as the file name under the skin's flag folder. Empty when the file named
    /// no language, which is the one case the reference covered with its own picture: it drew
    /// <c>Medialogos\flags\Unknown.png</c> then (<c>CurrentlyPlayingStyles.xaml:83-84</c>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// The reference bound the stored language straight into the file name and left it at that
    /// (<c>CurrentlyPlayingStyles.xaml:76-82</c>), which works for the languages its catalog happened to file
    /// under a two-letter name and draws nothing for the rest: the library writes ISO 639-1
    /// (<c>da</c>, <c>el</c>, <c>zh</c>) while the catalog files its flags by <b>country</b>
    /// (<c>dk.png</c>, <c>gr.png</c>, <c>CN.png</c>). Measured against this library that left eleven languages
    /// without a flag.
    /// </para>
    /// <para>
    /// The table below is therefore <b>ours, not the reference's</b>, and deliberately short: it says nothing
    /// about a language whose own code the skin already answers - a name that works is never rewritten - and it
    /// maps a language only to the one flag that stands for it in that catalog, never to a nation it might also
    /// be spoken in. Where nothing is known the value stands as the reference left it, and the card draws
    /// without a flag rather than with a wrong one.
    /// </para>
    /// </remarks>
    public static string Flag(string? language)
    {
      string name = SafeName(language);
      return FLAGS_BY_LANGUAGE.TryGetValue(name, out string? alias) ? alias : name;
    }

    /// <summary>
    /// Languages whose flag the catalog files under a country's code, in the shape the folder holds it. Only
    /// languages the catalog has no own-code file for are in here.
    /// </summary>
    private static readonly Dictionary<string, string> FLAGS_BY_LANGUAGE =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["cs"] = "cz",   // Czech
            ["da"] = "dk",   // Danish
            ["el"] = "gr",   // Greek
            ["he"] = "il",   // Hebrew
            ["hi"] = "in",   // Hindi
            ["ja"] = "jp",   // Japanese
            ["ko"] = "kr",   // Korean
            ["nb"] = "no",   // Norwegian Bokmål
            ["nn"] = "no",   // Norwegian Nynorsk
            ["te"] = "in",   // Telugu
            ["uk"] = "ua",   // Ukrainian
            ["zh"] = "cn",   // Chinese
        };

    /// <summary>
    /// The picture for an audio codec: the name as the library wrote it, or the name the catalog files that
    /// codec's logo under when the two differ.
    /// </summary>
    /// <remarks>
    /// Again the reference bound the stored encoding straight into the name
    /// (<c>CurrentlyPlayingStyles.xaml:97-101</c>), and again the catalog disagrees at exactly the popular
    /// places: this library's most common audio codec is "AC-3", which the catalog only answers as
    /// <c>dolbydigital</c> (it holds no <c>AC-3.png</c>), and the same for Dolby's two younger codecs and for
    /// TrueHD, whose MediaInfo name is the MLP payload's. The table is ours, is keyed by the whole figure the
    /// importer wrote, and says nothing about a codec the folder answers by its own name.
    /// </remarks>
    public static string AudioCodec(string? encoding)
    {
      string name = SafeName(encoding);
      return PICTURES_BY_AUDIO_CODEC.TryGetValue(name, out string? alias) ? alias : name;
    }

    /// <summary>
    /// Codecs whose logo the catalog files under the brand's name rather than the importer's - the Dolby family
    /// (whose MediaInfo names are AC-3, E-AC-3 and MLP FBA) and the DTS extensions.
    /// </summary>
    private static readonly Dictionary<string, string> PICTURES_BY_AUDIO_CODEC =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["AC-3"] = "dolbydigital",
            ["E-AC-3"] = "dolbydigitalplus",
            ["MLP FBA"] = "dolbytruehd",
            ["MLP FBA 16-ch"] = "dolbytruehd",
            ["DTS-ES"] = "dtsex",
            ["DTS-HD"] = "dtshd",
            ["DTS-HD MA"] = "dtshd_ma",
            ["DTS-HD HRA"] = "dtshra",
            ["HE-AAC"] = "aac lc-sbr",
        };

    /// <summary>
    /// The loudspeaker picture for a number of channels, which the reference picked by the <b>plain count</b>
    /// (<c>CurrentlyPlayingStyles.xaml:90-95</c>, <c>Medialogos\audio\{0}.png</c> bound to the count, not to a
    /// trade spelling): six channels asked for the picture of a 5.1 arrangement, which is why the catalog holds
    /// "6" and not "5.1" beside it.
    /// </summary>
    public static string Channels(int count)
    {
      return count <= 0 ? string.Empty : count.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>The picture for a video codec, named the way the file wrote it (<c>MediaInfoVideo.xaml:195-199</c>).</summary>
    public static string VideoCodec(string? encoding)
    {
      return SafeName(encoding);
    }

    /// <summary>
    /// The picture for how big a picture is, at the reference's own bounds - and those bounds already name the
    /// file, because "1080p" is both the word and the file (<c>MediaInfoVideo.xaml:205-214</c>).
    /// </summary>
    public static string Resolution(int width, int height)
    {
      return MediaStreamWords.Resolution(width, height);
    }

    /// <summary>
    /// The picture for how a subtitle track is carried: the reference had one file for a track lying inside the
    /// file and one for a track lying beside it (<c>CurrentlyPlayingStyles.xaml:169-173</c>), so this says which
    /// of the two the skin is asked for.
    /// </summary>
    public static string Carriage(bool isInternal)
    {
      return isInternal ? "Internal" : "External";
    }

    /// <summary>
    /// Whether a file's kind of picture is one of the four stereoscopic ones, said as the reference said it - the
    /// type's own short word under its logo (<c>MediaInfoVideo.xaml:176-194</c>).
    /// </summary>
    public static string StereoscopicType(string? videoType)
    {
      string type = (videoType ?? string.Empty).Trim().ToUpperInvariant();
      switch (type)
      {
        case "HSBS":
        case "SBS":
        case "HTAB":
        case "TAB":
          return type;
        default:
          return string.Empty;
      }
    }

    /// <summary>
    /// The word a file's high dynamic range is called, or nothing when its picture is an ordinary one.
    /// </summary>
    /// <remarks>
    /// The importer writes the format MediaInfo read out of the file ("HDR10", "HDR10+"), and it writes it only
    /// when that format is a high dynamic range one at all - the same test the library's own rule for the
    /// "UHD HDR" video type uses (<c>VideoStreamAspect.cs</c>, <c>IsHighDynamicRange</c>). This front end repeats
    /// that test rather than trusting the field, because a word which does not say HDR is not an answer about HDR.
    /// </remarks>
    public static string Hdr(string? hdrFormat)
    {
      string value = (hdrFormat ?? string.Empty).Trim();
      return value.Contains("HDR", StringComparison.Ordinal) ? value : string.Empty;
    }

    /// <summary>
    /// The row of pictures a file's picture is made of, in the order the reference's logo row drew them: the
    /// stereoscopic logo, the codec, the size, the proportion, and - as the two figures it never had a logo for -
    /// its bitrate over its frame rate. The high dynamic range answer is this front end's own seventh cell; the
    /// reference never asked a file whether its picture was mastered in HDR.
    /// </summary>
    /// <param name="video">
    /// The picture to describe: the file's <b>first</b> one and only that, which is what the reference described
    /// too (<c>MediaInfoVideo.xaml:152</c>, <c>AspectIndex="0"</c>).
    /// </param>
    /// <remarks>
    /// A figure the importer never wrote leaves its cell out, cell by cell, exactly as each of the reference's
    /// logos and labels was visible on its own value (<c>MediaInfoVideo.xaml:196-255</c>). The row's first cell is
    /// not in here: whether the file carries text at all is a glyph this front end wears
    /// (<c>PlayerControl/subtitles</c>), drawn from whether the subtitle list has anything in it.
    /// </remarks>
    public static PictureLine Picture(VideoStreamInfo? video)
    {
      if (video == null)
        return PictureLine.Empty;

      string encoding = (video.VideoEncoding ?? string.Empty).Trim();

      return new PictureLine(
          StereoscopicType(video.VideoType),
          SafeName(encoding),
          encoding,
          Resolution(video.Width, video.Height),
          MediaStreamWords.RatioClass(video.AspectRatio),
          MediaStreamWords.RatioFigure(video.AspectRatio),
          Hdr(video.HdrFormat),
          MediaStreamWords.BitRate(video.BitRate),
          MediaStreamWords.FrameRate(video.Fps));
    }
  }

  /// <summary>
  /// One cell of the reference's logo row above the details: the name its picture is asked for, the word that
  /// stands when the worn skin carries no such picture, and whether the cell costs its room at all.
  /// </summary>
  /// <param name="StereoscopicType">The type's word ("SBS"), empty for an ordinary file.</param>
  /// <param name="CodecToken">The codec as a file name, empty when the importer wrote none.</param>
  /// <param name="CodecWord">The codec as the importer wrote it, for the case no picture answers.</param>
  /// <param name="Resolution">"1080p" and the like - one spelling that is both picture name and word.</param>
  /// <param name="RatioClass">"16:9", the class the proportion belongs to.</param>
  /// <param name="RatioFigure">"1.78:1", the measured proportion itself.</param>
  /// <param name="HdrFormat">"HDR10", empty for an ordinary picture.</param>
  /// <param name="BitrateText">"7080 kbps", empty when the importer never measured a bitrate.</param>
  /// <param name="FrameRateText">"23.976 fps", empty when the importer never measured a frame rate.</param>
  public sealed record PictureLine(
      string StereoscopicType,
      string CodecToken,
      string CodecWord,
      string Resolution,
      string RatioClass,
      string RatioFigure,
      string HdrFormat,
      string BitrateText,
      string FrameRateText)
  {
    /// <summary>The row of a file nobody looked inside: nothing to ask, nothing to say.</summary>
    public static readonly PictureLine Empty = new PictureLine("", "", "", "", "", "", "", "", "");

    /// <summary>Whether the file is one of the four stereoscopic kinds and may wear its logo.</summary>
    public bool ShowsStereoscopic => StereoscopicType.Length > 0;

    /// <summary>Whether the file's codec is known at all.</summary>
    public bool ShowsCodec => CodecWord.Length > 0;

    /// <summary>Whether the importer measured the picture.</summary>
    public bool ShowsResolution => Resolution.Length > 0;

    /// <summary>Whether anything is known about the proportion - either half of the cell is enough to draw it.</summary>
    public bool ShowsRatio => RatioClass.Length > 0 || RatioFigure.Length > 0;

    /// <summary>Whether the file's picture was mastered in a high dynamic range format at all.</summary>
    public bool ShowsHdr => HdrFormat.Length > 0;

    /// <summary>Whether the bitrate arrived.</summary>
    public bool ShowsBitrate => BitrateText.Length > 0;

    /// <summary>Whether the frame rate arrived.</summary>
    public bool ShowsFrames => FrameRateText.Length > 0;

    /// <summary>Whether the bitrate or the frame rate arrived - the cell stands as soon as one of them did.</summary>
    public bool ShowsRate => ShowsBitrate || ShowsFrames;

    /// <summary>
    /// Both sayings of the proportion in one line, the class first and the measured figure after it: "21:9 ·
    /// 2.38:1". The reference wrote the two on opposite edges of its proportion picture
    /// (<c>MediaInfoVideo.xaml:226-244</c>); said as one figure beside a glyph, they read as one answer, which is
    /// what they are.
    /// </summary>
    public string RatioText =>
        RatioClass.Length > 0 && RatioFigure.Length > 0
            ? RatioClass + " · " + RatioFigure
            : RatioClass.Length > 0 ? RatioClass : RatioFigure;

    /// <summary>
    /// The bitrate and the frame rate in the one line the last plaque writes: "3626 kbps · 24.000 fps". The
    /// reference stacked the two in its own cell (<c>MediaInfoVideo.xaml:246-255</c>); read beside a glyph they
    /// are one answer about the file's rate, said on the line the plaque has room for.
    /// </summary>
    public string RateText =>
        BitrateText.Length > 0 && FrameRateText.Length > 0
            ? BitrateText + " · " + FrameRateText
            : BitrateText.Length > 0 ? BitrateText : FrameRateText;

    /// <summary>Whether the row costs its height, which is true as soon as one of its cells has an answer.</summary>
    public bool ShowsAnything =>
        ShowsStereoscopic || ShowsCodec || ShowsResolution || ShowsRatio || ShowsHdr || ShowsRate;
  }
}
