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

using System.Collections.Generic;
using NUnit.Framework;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.ViewModels;

namespace Tests.Frontend
{
  /// <summary>
  /// The words a screen makes out of the figures a file's tracks carry.
  /// </summary>
  /// <remarks>
  /// These are the rules the reference drew as logos, so they are worth holding down by test: a picture that is
  /// called by the wrong size is a wrong statement about a film, and a file whose importer wrote nothing must stay
  /// silent rather than say "0 kbps".
  /// </remarks>
  [TestFixture]
  public class TestMediaStreamWords
  {
    [Test]
    public void APictureIsNamedByItsHeightAndItsWidthTogether()
    {
      // The reference's own bounds (MediaInfoVideo.xaml:210-229), which is why a DVD is "576" and not "720p":
      // both numbers decide, so a ladder of heights alone would call it wrong.
      Assert.That(MediaStreamWords.Resolution(640, 240), Is.EqualTo("240"));
      Assert.That(MediaStreamWords.Resolution(640, 360), Is.EqualTo("360"));
      Assert.That(MediaStreamWords.Resolution(720, 480), Is.EqualTo("480"));
      Assert.That(MediaStreamWords.Resolution(720, 576), Is.EqualTo("576"));
      Assert.That(MediaStreamWords.Resolution(1280, 720), Is.EqualTo("720p"));
      Assert.That(MediaStreamWords.Resolution(1920, 1080), Is.EqualTo("1080p"));
      Assert.That(MediaStreamWords.Resolution(3840, 2160), Is.EqualTo("2160p"));
      Assert.That(MediaStreamWords.Resolution(7680, 4320), Is.EqualTo("4320p"));
    }

    [Test]
    public void APictureNobodyMeasuredIsNotNamedAtAll()
    {
      Assert.That(MediaStreamWords.Resolution(0, 0), Is.Empty,
          "a file whose importer never got inside it has no size, and a nought on screen would be a size");
      Assert.That(MediaStreamWords.Resolution(1920, 0), Is.Empty);
      Assert.That(MediaStreamWords.Resolution(0, 1080), Is.Empty);
    }

    [Test]
    public void AProportionIsSaidAsItsClassAndAsItsNumber()
    {
      // The classes and their bounds are the reference's (MediaInfoVideo.xaml:229-238); the number is its
      // "{0:F2}:1" (MediaInfoVideo.xaml:224-226).
      Assert.That(MediaStreamWords.RatioClass(1.3333f), Is.EqualTo("4:3"));
      Assert.That(MediaStreamWords.RatioClass(1.7778f), Is.EqualTo("16:9"));
      Assert.That(MediaStreamWords.RatioClass(2.39f), Is.EqualTo("21:9"));
      Assert.That(MediaStreamWords.RatioFigure(1.7778f), Is.EqualTo("1.78:1"));
      Assert.That(MediaStreamWords.RatioClass(0f), Is.Empty);
      Assert.That(MediaStreamWords.RatioFigure(0f), Is.Empty);
    }

    [Test]
    public void ChannelsAreSaidInTheWordTheTradeUsesForThem()
    {
      Assert.That(MediaStreamWords.Channels(2), Is.EqualTo("2.0"));
      Assert.That(MediaStreamWords.Channels(6), Is.EqualTo("5.1"));
      Assert.That(MediaStreamWords.Channels(8), Is.EqualTo("7.1"));
      Assert.That(MediaStreamWords.Channels(0), Is.Empty,
          "a count the importer left out is not a track of no channels");
      Assert.That(MediaStreamWords.Channels(11), Is.EqualTo("11"),
          "an arrangement nobody has a word for is said as the count rather than as a guess");
    }

    [Test]
    public void AFigureTheImporterLeftOutIsWrittenNowhere()
    {
      // The reference hid the logo or the label rather than drawing a nought (MediaInfoVideo.xaml:239-255).
      Assert.That(MediaStreamWords.BitRate(640), Is.EqualTo("640 kbps"));
      Assert.That(MediaStreamWords.BitRate(0), Is.Empty);
      Assert.That(MediaStreamWords.SampleRate(48000), Is.EqualTo("48000 Hz"));
      Assert.That(MediaStreamWords.SampleRate(0), Is.Empty);
      Assert.That(MediaStreamWords.FrameRate(23.976f), Is.EqualTo("24 fps"));
      Assert.That(MediaStreamWords.FrameRate(0f), Is.Empty);
    }

    [Test]
    public void TheFrameRateIsToldAsAWholeNumber()
    {
      // The viewer asked for whole figures on 2026-11-02.  The worn skin wrote three decimals
      // (MediaInfoVideo.xaml:250-254) and so printed "23.976 fps" beside "24.000 fps" for two files that change
      // pictures at the same speed to any purpose; the two spellings now answer one and the same word, which is
      // the whole point of the rounding.
      Assert.That(MediaStreamWords.FrameRate(24f), Is.EqualTo("24 fps"));
      Assert.That(MediaStreamWords.FrameRate(23.976f), Is.EqualTo("24 fps"));
      Assert.That(MediaStreamWords.FrameRate(23.976f), Is.EqualTo(MediaStreamWords.FrameRate(24f)),
          "23.976 and 24 are the same answer now - that is what the rounding is for");
      Assert.That(MediaStreamWords.FrameRate(29.97f), Is.EqualTo("30 fps"));
      Assert.That(MediaStreamWords.FrameRate(25f), Is.EqualTo("25 fps"));
      Assert.That(MediaStreamWords.FrameRate(59.94f), Is.EqualTo("60 fps"));
    }

    [Test]
    public void OnlyAStereoscopicFileIsCalledThreeD()
    {
      // The four types the reference drew its 3D logo for (MediaInfoVideo.xaml:176-190); "HD" and "UHD" stand in
      // the same field and mean something else. The word under the logo is the type itself, not a sentence about it.
      Assert.That(MediaStreamLogos.StereoscopicType("SBS"), Is.EqualTo("SBS"));
      Assert.That(MediaStreamLogos.StereoscopicType("htab"), Is.EqualTo("HTAB"),
          "the reference compared its four types upper-cased, and the library writes them both ways");
      Assert.That(MediaStreamLogos.StereoscopicType("HD"), Is.Empty);
      Assert.That(MediaStreamLogos.StereoscopicType("UHD"), Is.Empty);
      Assert.That(MediaStreamLogos.StereoscopicType(null), Is.Empty);
    }

    [Test]
    public void APictureIsAskedForByTheNameTheOldSkinFiledItUnder()
    {
      // The value comes from the model and the folder from the skin, but the value has to be a file name: the
      // reference ran every one of them through its SafeFilenameConverter
      // (Source\UI\SkinEngine\MpfElements\Converters\SafeFilenameConverter.cs:44-52).
      Assert.That(MediaStreamLogos.Flag("de"), Is.EqualTo("de"),
          "the library writes audio languages in lower case and subtitle languages in capitals, and the old "
          + "skin's flag folder answers both from one file");
      Assert.That(MediaStreamLogos.Flag("  "), Is.Empty,
          "no language named is the one case the reference covered with a picture of its own, its unknown flag");
      Assert.That(MediaStreamLogos.Channels(6), Is.EqualTo("6"),
          "the loudspeaker pictures are filed by the plain count, not by the trade's word for the arrangement");
      Assert.That(MediaStreamLogos.Channels(0), Is.Empty);
      Assert.That(MediaStreamLogos.VideoCodec("AVC"), Is.EqualTo("AVC"),
          "case is the reference's to leave alone; its catalog answers 'avc.png' from the same file");
      Assert.That(MediaStreamLogos.SafeName("MPEG-1 Audio Layer 3"), Is.EqualTo("MPEG-1 Audio Layer 3"),
          "spaces stand in the old catalog's own names, so they are not tidied away");
      Assert.That(MediaStreamLogos.SafeName("DTS/HD: MA"), Is.EqualTo("DTS_HD_ MA"),
          "a character a file name cannot carry becomes an underscore, which is all the reference did to it");
      Assert.That(MediaStreamLogos.Carriage(true), Is.EqualTo("Internal"));
      Assert.That(MediaStreamLogos.Carriage(false), Is.EqualTo("External"));
    }

    [Test]
    public void ALanguageOrCodecThePictureFolderFilesUnderAnotherNameIsAskedForByThatName()
    {
      // The reference handed the stored value straight to the skin and left a hole where its folder held no file
      // of that name. Measured against this library that hole was eleven languages wide - it writes ISO 639-1
      // while the flag folder files by country - and it covered the library's most common audio codec, "AC-3",
      // which the logo folder only answers as the Dolby picture. The tables behind these two calls are therefore
      // ours (the user's word: "Tabelle für Sprachen und Codecs einbauen"), and they answer nothing that the
      // folder already answers on its own.
      Assert.That(MediaStreamLogos.Flag("ja"), Is.EqualTo("jp"),
          "the folder holds the Japanese flag under the country's code, not under the language's");
      Assert.That(MediaStreamLogos.Flag("ZH"), Is.EqualTo("cn"),
          "one table answers the library's capitals and its lower case from the same row");
      Assert.That(MediaStreamLogos.Flag("da"), Is.EqualTo("dk"));
      Assert.That(MediaStreamLogos.Flag("el"), Is.EqualTo("gr"));
      Assert.That(MediaStreamLogos.Flag("cs"), Is.EqualTo("cz"));
      Assert.That(MediaStreamLogos.Flag("de"), Is.EqualTo("de"),
          "a language the folder answers by its own code is never rewritten");
      Assert.That(MediaStreamLogos.Flag("sq"), Is.EqualTo("sq"),
          "a language no row speaks for stands as the reference left it, and the skin is asked for a picture it "
          + "may well not carry");
      Assert.That(MediaStreamLogos.AudioCodec("AC-3"), Is.EqualTo("dolbydigital"),
          "the logo folder carries Dolby's picture under Dolby's name and none under the importer's");
      Assert.That(MediaStreamLogos.AudioCodec("E-AC-3"), Is.EqualTo("dolbydigitalplus"));
      Assert.That(MediaStreamLogos.AudioCodec("MLP FBA"), Is.EqualTo("dolbytruehd"),
          "what the importer names as the MLP payload is the picture of the codec that carries it");
      Assert.That(MediaStreamLogos.AudioCodec("DTS"), Is.EqualTo("DTS"),
          "a codec whose own name the folder answers keeps it");
      Assert.That(MediaStreamLogos.AudioCodec(""), Is.Empty);
    }

    [Test]
    public void ThePictureRowFillsItsCellsTheWayTheReferenceReadThem()
    {
      VideoStreamInfo video = new VideoStreamInfo(0, "HD", "AVC", 1920, 1080, 1.7778f, 12500L, 23.976f, 7080L, "HDR10");
      PictureLine line = MediaStreamLogos.Picture(video);

      Assert.That(line.StereoscopicType, Is.Empty, "\"HD\" is not a stereoscopic type");
      Assert.That(line.HdrFormat, Is.EqualTo("HDR10"),
          "the format the file named for itself, not a bare \"HDR\" - a viewer deciding whether their set can show "
          + "the file wants to know which mastering it was");
      Assert.That(line.ShowsHdr, Is.True);
      Assert.That(line.CodecToken, Is.EqualTo("AVC"));
      Assert.That(line.CodecWord, Is.EqualTo("AVC"));
      Assert.That(line.Resolution, Is.EqualTo("1080p"));
      Assert.That(line.RatioClass, Is.EqualTo("16:9"), "the class the reference wrote above its ratio picture");
      Assert.That(line.RatioFigure, Is.EqualTo("1.78:1"), "the figure it wrote below");
      Assert.That(line.RatioText, Is.EqualTo("16:9 · 1.78:1"),
          "said beside the frame's glyph the two are one answer on one line, which is what they always were");
      Assert.That(line.BitrateText, Is.EqualTo("12500 kbps"));
      Assert.That(line.FrameRateText, Is.EqualTo("24 fps"),
          "the two figures it never had a logo for, the rate told as a whole number (see TheFrameRateIsToldAsAWholeNumber)");
      Assert.That(line.RateText, Is.EqualTo("12500 kbps · 24 fps"),
          "said beside the frame glyph the two are one line about the file's rate");
      Assert.That(line.ShowsAnything, Is.True);
    }

    [Test]
    public void AFileCarryingNoFiguresLeavesItsCellsAway()
    {
      VideoStreamInfo video = new VideoStreamInfo(0, null, null, 1280, 720, 0f, 0L, 0f, 0L, null);
      PictureLine line = MediaStreamLogos.Picture(video);

      Assert.That(line.CodecWord, Is.Empty);
      Assert.That(line.Resolution, Is.EqualTo("720p"));
      Assert.That(line.RatioClass, Is.Empty);
      Assert.That(line.BitrateText, Is.Empty);
      Assert.That(line.FrameRateText, Is.Empty,
          "a nought the importer never wrote is not a figure to draw a cell for (MediaInfoVideo.xaml:239-255)");
      Assert.That(line.ShowsResolution, Is.True);
      Assert.That(line.ShowsRatio, Is.False);
      Assert.That(line.ShowsHdr, Is.False, "an ordinary picture leaves its container away entirely");
      Assert.That(line.ShowsBitrate, Is.False);
      Assert.That(line.ShowsFrames, Is.False);
      Assert.That(line.ShowsRate, Is.False);
      Assert.That(line.RateText, Is.Empty);
      Assert.That(line.RatioText, Is.Empty);
    }

    [Test]
    public void ARateWithoutAFrameRateIsSaidWithoutADot()
    {
      // The dot between the two figures of the last plaque is only there when both arrived. A file whose
      // importer measured one of them says that one and does not stand there with a separator leading or
      // trailing into nothing - the same rule the proportion's line follows.
      PictureLine onlyRate = new PictureLine("", "", "", "", "", "", "", "4000 kbps", "");
      PictureLine onlyFrames = new PictureLine("", "", "", "", "", "", "", "", "24.000 fps");
      PictureLine neither = new PictureLine("", "", "", "", "", "", "", "", "");

      Assert.That(onlyRate.RateText, Is.EqualTo("4000 kbps"));
      Assert.That(onlyFrames.RateText, Is.EqualTo("24.000 fps"));
      Assert.That(neither.RateText, Is.Empty);
      Assert.That(onlyRate.ShowsRate, Is.True);
      Assert.That(neither.ShowsRate, Is.False);
    }

    [Test]
    public void AStereoscopicFileStandsInTheFirstCellOfItsRow()
    {
      VideoStreamInfo video = new VideoStreamInfo(0, "HSBS", "MVC", 1920, 1080, 2.39f, 30000L, 23.976f, 0L, "");
      PictureLine line = MediaStreamLogos.Picture(video);

      Assert.That(line.StereoscopicType, Is.EqualTo("HSBS"),
          "the kind of picture leads the row, which is where the reference hung its 3D logo");
      Assert.That(line.ShowsStereoscopic, Is.True);
      Assert.That(line.RatioClass, Is.EqualTo("21:9"));
      Assert.That(line.RatioFigure, Is.EqualTo("2.39:1"));
      Assert.That(line.RatioText, Is.EqualTo("21:9 · 2.39:1"));
      Assert.That(line.BitrateText, Is.EqualTo("30000 kbps"));
      Assert.That(line.FrameRateText, Is.EqualTo("24 fps"),
          "a frame rate is said over the bitrate even when the sample rate of a picture was never written - and it "
          + "is said as a whole number, which is what the rounding does to a file read as 23.976");
    }

    [Test]
    public void AHighDynamicRangeFileSaysWhichFormatAndAnOrdinaryFileSaysNothing()
    {
      // The word is the format the file named, and only a word that says HDR counts as one - the test the
      // library itself applies to the format MediaInfo read (VideoStreamAspect.IsHighDynamicRange), which is
      // also the rule behind its "UHD HDR" video type.  Said honestly: that rule misses a mastering which does
      // not spell itself out that way, "HLG" among them, and this front end misses it with it rather than
      // inventing a second, better-looking rule of its own.
      Assert.That(MediaStreamLogos.Hdr("HDR10"), Is.EqualTo("HDR10"));
      Assert.That(MediaStreamLogos.Hdr("  HDR10+  "), Is.EqualTo("HDR10+"), "the file's own spelling, trimmed");
      Assert.That(MediaStreamLogos.Hdr("HDR"), Is.EqualTo("HDR"), "a file that only says HDR says HDR");
      Assert.That(MediaStreamLogos.Hdr("HDR10 / HDR10+"), Is.EqualTo("HDR10 / HDR10+"),
          "a file mastered for both names both - four titles of the library in front of us answer that way");
      Assert.That(MediaStreamLogos.Hdr(null), Is.Empty);
      Assert.That(MediaStreamLogos.Hdr("   "), Is.Empty);
      Assert.That(MediaStreamLogos.Hdr("AVC"), Is.Empty, "a codec word is not an answer about HDR");
      Assert.That(MediaStreamLogos.Hdr("HLG"), Is.Empty,
          "the known hole of the library's own rule: Hybrid Log-Gamma is a high dynamic range format but never "
          + "says the letters HDR, so neither the video type nor this cell knows it");

      PictureLine hdrOnly = new PictureLine("", "", "", "", "", "", "HDR10", "", "");
      Assert.That(hdrOnly.ShowsHdr, Is.True);
      Assert.That(hdrOnly.ShowsAnything, Is.True,
          "an HDR answer of its own is worth the row's height, as every other answer is");
    }

    [Test]
    public void AFileNobodyLookedInsideSaysNothingAboutItsPicture()
    {
      PictureLine line = MediaStreamLogos.Picture(null);

      Assert.That(line, Is.SameAs(PictureLine.Empty),
          "no first picture means no row at all, and the band above the story then costs no room");
      Assert.That(line.ShowsAnything, Is.False);
    }

    [Test]
    public void AnAudioTrackIsSaidInItsFourCorners()
    {
      AudioTrackCard card = MediaStreamWords.AudioCard(new AudioStreamInfo(1, "de", "AC3", 6, 640L, 48000L));

      Assert.That(card.Language, Is.EqualTo("German"),
          "the language is read by its name, which the viewer asked for on 2026-11-03 in place of the two-letter "
          + "code the reference wrote (CurrentlyPlayingStyles.xaml:85-87)");
      Assert.That(card.FlagToken, Is.EqualTo("de"), "the flag it stood beside that word");
      Assert.That(card.ShowsFlag, Is.True);
      Assert.That(card.ShowsUnknownFlag, Is.False);
      Assert.That(card.Channels, Is.EqualTo("5.1"));
      Assert.That(card.ChannelsToken, Is.EqualTo("6"),
          "the loudspeaker picture of six channels, which is the file the old catalog filed it under");
      Assert.That(card.Encoding, Is.EqualTo("AC3"));
      Assert.That(card.CodecToken, Is.EqualTo("AC3"));
      Assert.That(card.Detail, Is.EqualTo("640 kbps · 48000 Hz"));
    }

    [Test]
    public void AnAudioTrackThatNamedNothingIsNotACardOfNoughts()
    {
      AudioTrackCard card = MediaStreamWords.AudioCard(new AudioStreamInfo(1, "  ", null, 0, 0L, 0L));

      Assert.That(card.Language, Is.Empty);
      Assert.That(card.Channels, Is.Empty);
      Assert.That(card.Encoding, Is.Empty);
      Assert.That(card.Detail, Is.Empty,
          "a track whose file named no rates is not described by two zeroes");
      Assert.That(card.ShowsUnknownFlag, Is.True,
          "a track that named no language is exactly the case the reference drew its unknown flag for "
          + "(CurrentlyPlayingStyles.xaml:83-84)");
      Assert.That(card.ShowsChannels, Is.False);
      Assert.That(card.ShowsCodec, Is.False);
    }

    [Test]
    public void ATrackWithOneRateSaysThatOneAlone()
    {
      AudioTrackCard card = MediaStreamWords.AudioCard(new AudioStreamInfo(0, "en", "EAC3", 8, 0L, 48000L));
      Assert.That(card.Detail, Is.EqualTo("48000 Hz"),
          "the two figures are joined by a point only when both are there");
      Assert.That(card.Channels, Is.EqualTo("7.1"));
    }

    [Test]
    public void ASubtitleTrackIsSaidInItsFourCorners()
    {
      SubtitleTrackCard card =
          MediaStreamWords.SubtitleCard(new SubtitleStreamInfo(3, "de", null, "SRT", true, false, true));

      Assert.That(card.Language, Is.EqualTo("German"),
          "both cards of a file read their language by the same name, whichever way its file wrote it");
      Assert.That(card.FlagToken, Is.EqualTo("de"), "the flag it stood beside that word");
      Assert.That(card.Carriage, Is.EqualTo("intern"));
      Assert.That(card.CarriageToken, Is.EqualTo("Internal"),
          "the picture of a track lying inside the file (CurrentlyPlayingStyles.xaml:169-171)");
      Assert.That(card.Format, Is.EqualTo("SRT"),
          "the word the reference stood inside its picture of a frame");
      Assert.That(card.ShowsFormat, Is.True);
      Assert.That(card.Forced, Is.EqualTo("Forced"),
          "the word the reference itself wrote in its skin (CurrentlyPlayingStyles.xaml:186-190)");
      Assert.That(card.ShowsForced, Is.True);
    }

    [Test]
    public void ATextTrackFromOutsideTheFileSaysSo()
    {
      SubtitleTrackCard card =
          MediaStreamWords.SubtitleCard(new SubtitleStreamInfo(4, "en", null, "ASS", false, false, false));

      Assert.That(card.Carriage, Is.EqualTo("extern"));
      Assert.That(card.CarriageToken, Is.EqualTo("External"),
          "the reference distinguished the tracks lying inside the file from the ones that came to it from "
          + "outside, and said them with two pictures (CurrentlyPlayingStyles.xaml:167-173)");
      Assert.That(card.Forced, Is.Empty);
      Assert.That(card.ShowsForced, Is.False);
      Assert.That(card.Format, Is.EqualTo("ASS"),
          "the format is on the card even when the codec of the text was never written");
    }

    /// <summary>
    /// The words a card reads its language by.  What a container writes in that place is not one alphabet but
    /// three - ISO 639-1 ("de"), ISO 639-2/B ("ger") and ISO 639-2/T ("deu") - and all three mean German, which is
    /// the word the viewer asked to read (2026-11-03) in place of the two letters the reference printed
    /// (<c>CurrentlyPlayingStyles.xaml:85-87</c>).  A code nobody can name stays the code it came as, in capitals:
    /// "QIC" is at least a thing to search for, and nothing at all is left for a file that named no language.
    /// </summary>
    [Test]
    public void ALanguageIsReadByItsNameAndAnUnknownOneByItsCode()
    {
      Assert.That(MediaStreamWords.LanguageWord("de"), Is.EqualTo("German"),
          "the two letters a matroska usually writes");
      Assert.That(MediaStreamWords.LanguageWord("deu"), Is.EqualTo("German"),
          "ISO 639-2/T, which is what an importer that reads the nfo writes");
      Assert.That(MediaStreamWords.LanguageWord("ger"), Is.EqualTo("German"),
          "ISO 639-2/B, which is what a dvd and a blu-ray carry");
      Assert.That(MediaStreamWords.LanguageWord(" EN "), Is.EqualTo("English"),
          "asked case-blind and with the room around the letters cut off, because that is how files write them");
      Assert.That(MediaStreamWords.LanguageWord("qic"), Is.EqualTo("QIC"),
          "a language neither the table nor the framework can name is said by the code its own file carries");
      Assert.That(MediaStreamWords.LanguageWord(string.Empty), Is.Empty, "no language, no word");
    }
  }
}
