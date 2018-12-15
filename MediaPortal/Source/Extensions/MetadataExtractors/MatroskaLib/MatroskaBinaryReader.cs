#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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

using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.ResourceAccess;
using NEbml.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MediaPortal.Extensions.MetadataExtractors.MatroskaLib
{
  public class MatroskaBinaryReader
  {
    #region Private Fields

    private static readonly ElementDescriptor SegmentElement = new ElementDescriptor(0x18538067, "Segment", ElementType.MasterElement);
    private static readonly ElementDescriptor SegmentInfoElement = new ElementDescriptor(0x1549a966, "Info", ElementType.MasterElement);
    private static readonly ElementDescriptor SeekHeadElement = new ElementDescriptor(0x114d9b74, "SeekHead", ElementType.MasterElement);
    private static readonly ElementDescriptor ClusterElement = new ElementDescriptor(0x1f43b675, "Cluster", ElementType.MasterElement);
    private static readonly ElementDescriptor TracksElement = new ElementDescriptor(0x1654ae6b, "Tracks", ElementType.MasterElement);
    private static readonly ElementDescriptor VideoElement = new ElementDescriptor(0xe0, "Video", ElementType.MasterElement);
    private static readonly ElementDescriptor AudioElement = new ElementDescriptor(0xe1, "Audio", ElementType.MasterElement);
    private static readonly ElementDescriptor CuesElement = new ElementDescriptor(0x1c53bb6b, "Cues", ElementType.MasterElement);
    private static readonly ElementDescriptor ContentEncodingsElement = new ElementDescriptor(0x6d80, "ContentEncodings", ElementType.MasterElement);
    private static readonly ElementDescriptor AttachmentsElement = new ElementDescriptor(0x1941a469, "Attachments", ElementType.MasterElement);
    private static readonly ElementDescriptor ChaptersElement = new ElementDescriptor(0x1043a770, "Chapters", ElementType.MasterElement);
    private static readonly ElementDescriptor TagsElement = new ElementDescriptor(0x1254c367, "Tags", ElementType.MasterElement);

    private static readonly ElementDescriptor TitleElement = new ElementDescriptor(0x7ba9, "Title", ElementType.Utf8String);
    private static readonly ElementDescriptor MuxingAppElement = new ElementDescriptor(0x4d80, "MuxingApp", ElementType.Utf8String);
    private static readonly ElementDescriptor WritingAppElement = new ElementDescriptor(0x5741, "WritingApp", ElementType.Utf8String);
    private static readonly ElementDescriptor DurationElement = new ElementDescriptor(0x4489, "Duration", ElementType.Float);
    private static readonly ElementDescriptor DateUTCElement = new ElementDescriptor(0x4461, "DateUTC", ElementType.Date);

    private ILocalFsResourceAccessor _lfsra = null;
    private IDictionary<string, IList<string>> _tags = null;
    private IDictionary<string, IList<string>> _infoProps = null;
    private IDictionary<string, IList<string>> _audioProps = null;
    private IDictionary<string, IList<string>> _videoProps = null;
    private IDictionary<string, byte[]> _attachments = null;
    private List<MatroskaConsts.MatroskaAttachment> _attachmentList;
    private List<ulong> _elementsRead = new List<ulong>();
    private readonly IDictionary<ulong, ElementDescriptor> _descriptorsMap;
    private readonly ElementDescriptor[] _elementDescriptors =
    {
            SegmentElement,

            SeekHeadElement,
            new ElementDescriptor(0x4dbb, "Seek", ElementType.MasterElement),
            new ElementDescriptor(0x53ab, "SeekID", ElementType.Binary),
            new ElementDescriptor(0x53ac, "SeekPosition", ElementType.UnsignedInteger),

            SegmentInfoElement,
            new ElementDescriptor(0x73a4, "SegmentUID", ElementType.Binary),
            new ElementDescriptor(0x7384, "SegmentFilename", ElementType.Utf8String),
            new ElementDescriptor(0x3cb923, "PrevUID", ElementType.Binary),
            new ElementDescriptor(0x3c83ab, "PrevFilename", ElementType.Utf8String),
            new ElementDescriptor(0x3eb923, "NextUID", ElementType.Binary),
            new ElementDescriptor(0x3e83bb, "NextFilename", ElementType.Utf8String),
            new ElementDescriptor(0x4444, "SegmentFamily", ElementType.Binary),
            new ElementDescriptor(0x6924, "ChapterTranslate", ElementType.MasterElement),
            new ElementDescriptor(0x69fc, "ChapterTranslateEditionUID", ElementType.UnsignedInteger),
            new ElementDescriptor(0x69bf, "ChapterTranslateCodec", ElementType.UnsignedInteger),
            new ElementDescriptor(0x69a5, "ChapterTranslateID", ElementType.Binary),
            new ElementDescriptor(0x2ad7b1, "TimecodeScale", ElementType.UnsignedInteger),
            DurationElement,
            DateUTCElement,
            TitleElement,
            MuxingAppElement,
            WritingAppElement,

            ClusterElement,
            new ElementDescriptor(0xe7, "Timecode", ElementType.UnsignedInteger),
            new ElementDescriptor(0x5854, "SilentTracks", ElementType.MasterElement),
            new ElementDescriptor(0x58d7, "SilentTrackNumber", ElementType.UnsignedInteger),
            new ElementDescriptor(0xa7, "Position", ElementType.UnsignedInteger),
            new ElementDescriptor(0xab, "PrevSize", ElementType.UnsignedInteger),
            new ElementDescriptor(0xa0, "BlockGroup", ElementType.MasterElement),
            new ElementDescriptor(0xa1, "Block", ElementType.Binary),
            new ElementDescriptor(0xa2, "BlockVirtual", ElementType.Binary),
            new ElementDescriptor(0x75a1, "BlockAdditions", ElementType.MasterElement),
            new ElementDescriptor(0xa6, "BlockMore", ElementType.MasterElement),
            new ElementDescriptor(0xee, "BlockAddID", ElementType.UnsignedInteger),
            new ElementDescriptor(0xa5, "BlockAdditional", ElementType.Binary),
            new ElementDescriptor(0x9b, "BlockDuration", ElementType.UnsignedInteger),
            new ElementDescriptor(0xfa, "ReferencePriority", ElementType.UnsignedInteger),
            new ElementDescriptor(0xfb, "ReferenceBlock", ElementType.SignedInteger),
            new ElementDescriptor(0xfd, "ReferenceVirtual", ElementType.SignedInteger),
            new ElementDescriptor(0xa4, "CodecState", ElementType.Binary),
            new ElementDescriptor(0x8e, "Slices", ElementType.MasterElement),
            new ElementDescriptor(0xe8, "TimeSlice", ElementType.MasterElement),
            new ElementDescriptor(0xcc, "LaceNumber", ElementType.UnsignedInteger),
            new ElementDescriptor(0xcd, "FrameNumber", ElementType.UnsignedInteger),
            new ElementDescriptor(0xcb, "BlockAdditionID", ElementType.UnsignedInteger),
            new ElementDescriptor(0xce, "Delay", ElementType.UnsignedInteger),
            new ElementDescriptor(0xcf, "Duration", ElementType.UnsignedInteger),
            new ElementDescriptor(0xa3, "SimpleBlock", ElementType.Binary),
            new ElementDescriptor(0xaf, "EncryptedBlock", ElementType.Binary),

            TracksElement,
            new ElementDescriptor(0xae, "TrackEntry", ElementType.MasterElement),
            new ElementDescriptor(0xd7, "TrackNumber", ElementType.UnsignedInteger),
            new ElementDescriptor(0x73c5, "TrackUID", ElementType.UnsignedInteger),
            new ElementDescriptor(0x83, "TrackType", ElementType.UnsignedInteger),
            new ElementDescriptor(0xb9, "FlagEnabled", ElementType.UnsignedInteger),
            new ElementDescriptor(0x88, "FlagDefault", ElementType.UnsignedInteger),
            new ElementDescriptor(0x55aa, "FlagForced", ElementType.UnsignedInteger),
            new ElementDescriptor(0x9c, "FlagLacing ", ElementType.UnsignedInteger),
            new ElementDescriptor(0x6de7, "MinCache", ElementType.UnsignedInteger),
            new ElementDescriptor(0x6df8, "MaxCache", ElementType.UnsignedInteger),
            new ElementDescriptor(0x23e383, "DefaultDuration", ElementType.UnsignedInteger),
            new ElementDescriptor(0x23314f, "TrackTimecodeScale", ElementType.Float),
            new ElementDescriptor(0x537f, "TrackOffset", ElementType.SignedInteger),
            new ElementDescriptor(0x55ee, "MaxBlockAdditionID", ElementType.UnsignedInteger),
            new ElementDescriptor(0x536e, "Name", ElementType.Utf8String),
            new ElementDescriptor(0x22b59c, "Language", ElementType.AsciiString),
            new ElementDescriptor(0x86, "CodecID", ElementType.AsciiString),
            new ElementDescriptor(0x63a2, "CodecPrivate", ElementType.Binary),
            new ElementDescriptor(0x258688, "CodecName", ElementType.Utf8String),
            new ElementDescriptor(0x7446, "AttachmentLink", ElementType.UnsignedInteger),
            new ElementDescriptor(0x3a9697, "CodecSettings", ElementType.Utf8String),
            new ElementDescriptor(0x3b4040, "CodecInfoURL", ElementType.AsciiString),
            new ElementDescriptor(0x26b240, "CodecDownloadURL", ElementType.AsciiString),
            new ElementDescriptor(0xaa, "CodecDecodeAll", ElementType.UnsignedInteger),
            new ElementDescriptor(0x6fab, "TrackOverlay", ElementType.UnsignedInteger),
            new ElementDescriptor(0x6624, "TrackTranslate", ElementType.MasterElement),
            new ElementDescriptor(0x66fc, "TrackTranslateEditionUID", ElementType.UnsignedInteger),
            new ElementDescriptor(0x66bf, "TrackTranslateCodec", ElementType.UnsignedInteger),
            new ElementDescriptor(0x66a5, "TrackTranslateTrackID", ElementType.Binary),

            VideoElement,
            new ElementDescriptor(0x9a, "FlagInterlaced", ElementType.UnsignedInteger),
            new ElementDescriptor(0x53b8, "StereoMode", ElementType.UnsignedInteger),
            new ElementDescriptor(0xb0, "PixelWidth", ElementType.UnsignedInteger),
            new ElementDescriptor(0xba, "PixelHeight", ElementType.UnsignedInteger),
            new ElementDescriptor(0x54aa, "PixelCropBottom", ElementType.UnsignedInteger),
            new ElementDescriptor(0x54bb, "PixelCropTop", ElementType.UnsignedInteger),
            new ElementDescriptor(0x54cc, "PixelCropLeft", ElementType.UnsignedInteger),
            new ElementDescriptor(0x54dd, "PixelCropRight", ElementType.UnsignedInteger),
            new ElementDescriptor(0x54b0, "DisplayWidth", ElementType.UnsignedInteger),
            new ElementDescriptor(0x54ba, "DisplayHeight", ElementType.UnsignedInteger),
            new ElementDescriptor(0x54b2, "DisplayUnit", ElementType.UnsignedInteger),
            new ElementDescriptor(0x54b3, "AspectRatioType", ElementType.UnsignedInteger),
            new ElementDescriptor(0x2eb524, "ColourSpace", ElementType.Binary),
            new ElementDescriptor(0x2fb523, "GammaValue", ElementType.Float),

            AudioElement,
            new ElementDescriptor(0xb5, "SamplingFrequency", ElementType.Float),
            new ElementDescriptor(0x78b5, "OutputSamplingFrequency", ElementType.Float),
            new ElementDescriptor(0x9f, "Channels", ElementType.UnsignedInteger),
            new ElementDescriptor(0x7d7b, "ChannelPositions", ElementType.Binary),
            new ElementDescriptor(0x6264, "BitDepth", ElementType.UnsignedInteger),

            ContentEncodingsElement,
            new ElementDescriptor(0x6240, "ContentEncoding", ElementType.MasterElement),
            new ElementDescriptor(0x5031, "ContentEncodingOrder", ElementType.UnsignedInteger),
            new ElementDescriptor(0x5032, "ContentEncodingScope", ElementType.UnsignedInteger),
            new ElementDescriptor(0x5033, "ContentEncodingType", ElementType.UnsignedInteger),
            new ElementDescriptor(0x5034, "ContentCompression", ElementType.MasterElement),
            new ElementDescriptor(0x4254, "ContentCompAlgo", ElementType.UnsignedInteger),
            new ElementDescriptor(0x4255, "ContentCompSettings", ElementType.Binary),
            new ElementDescriptor(0x5035, "ContentEncryption", ElementType.MasterElement),
            new ElementDescriptor(0x47e1, "ContentEncAlgo", ElementType.UnsignedInteger),
            new ElementDescriptor(0x47e2, "ContentEncKeyID", ElementType.Binary),
            new ElementDescriptor(0x47e3, "ContentSignature", ElementType.Binary),
            new ElementDescriptor(0x47e4, "ContentSigKeyID", ElementType.Binary),
            new ElementDescriptor(0x47e5, "ContentSigAlgo", ElementType.UnsignedInteger),
            new ElementDescriptor(0x47e6, "ContentSigHashAlgo", ElementType.UnsignedInteger),

            CuesElement,
            new ElementDescriptor(0xbb, "CuePoint", ElementType.MasterElement),
            new ElementDescriptor(0xb3, "CueTime", ElementType.UnsignedInteger),
            new ElementDescriptor(0xb7, "CueTrackPositions", ElementType.MasterElement),
            new ElementDescriptor(0xf7, "CueTrack", ElementType.UnsignedInteger),
            new ElementDescriptor(0xf1, "CueClusterPosition", ElementType.UnsignedInteger),
            new ElementDescriptor(0x5378, "CueBlockNumber", ElementType.UnsignedInteger),
            new ElementDescriptor(0xea, "CueCodecState", ElementType.UnsignedInteger),
            new ElementDescriptor(0xdb, "CueReference", ElementType.MasterElement),
            new ElementDescriptor(0x96, "CueRefTime", ElementType.UnsignedInteger),
            new ElementDescriptor(0x97, "CueRefCluster", ElementType.UnsignedInteger),
            new ElementDescriptor(0x535f, "CueRefNumber", ElementType.UnsignedInteger),
            new ElementDescriptor(0xeb, "CueRefCodecState", ElementType.UnsignedInteger),

            AttachmentsElement,
            new ElementDescriptor(0x61a7, "AttachedFile", ElementType.MasterElement),
            new ElementDescriptor(0x467e, "FileDescription", ElementType.Utf8String),
            new ElementDescriptor(0x466e, "FileName", ElementType.Utf8String),
            new ElementDescriptor(0x4660, "FileMimeType", ElementType.AsciiString),
            new ElementDescriptor(0x465c, "FileData", ElementType.Binary),
            new ElementDescriptor(0x46ae, "FileUID", ElementType.UnsignedInteger),
            new ElementDescriptor(0x4675, "FileReferral", ElementType.Binary),

            ChaptersElement,
            new ElementDescriptor(0x45b9, "EditionEntry", ElementType.MasterElement),
            new ElementDescriptor(0x45bc, "EditionUID", ElementType.UnsignedInteger),
            new ElementDescriptor(0x45bd, "EditionFlagHidden", ElementType.UnsignedInteger),
            new ElementDescriptor(0x45db, "EditionFlagDefault", ElementType.UnsignedInteger),
            new ElementDescriptor(0x45dd, "EditionFlagOrdered", ElementType.UnsignedInteger),
            new ElementDescriptor(0xb6, "ChapterAtom", ElementType.MasterElement),
            new ElementDescriptor(0x73c4, "ChapterUID", ElementType.UnsignedInteger),
            new ElementDescriptor(0x91, "ChapterTimeStart", ElementType.UnsignedInteger),
            new ElementDescriptor(0x92, "ChapterTimeEnd", ElementType.UnsignedInteger),
            new ElementDescriptor(0x98, "ChapterFlagHidden", ElementType.UnsignedInteger),
            new ElementDescriptor(0x4598, "ChapterFlagEnabled", ElementType.UnsignedInteger),
            new ElementDescriptor(0x6e67, "ChapterSegmentUID", ElementType.Binary),
            new ElementDescriptor(0x6ebc, "ChapterSegmentEditionUID", ElementType.Binary),
            new ElementDescriptor(0x63c3, "ChapterPhysicalEquiv", ElementType.UnsignedInteger),
            new ElementDescriptor(0x8f, "ChapterTrack", ElementType.MasterElement),
            new ElementDescriptor(0x89, "ChapterTrackNumber", ElementType.UnsignedInteger),
            new ElementDescriptor(0x80, "ChapterDisplay", ElementType.MasterElement),
            new ElementDescriptor(0x85, "ChapString", ElementType.Utf8String),
            new ElementDescriptor(0x437c, "ChapLanguage", ElementType.AsciiString),
            new ElementDescriptor(0x437e, "ChapCountry", ElementType.AsciiString),
            new ElementDescriptor(0x6944, "ChapProcess", ElementType.MasterElement),
            new ElementDescriptor(0x6955, "ChapProcessCodecID", ElementType.UnsignedInteger),
            new ElementDescriptor(0x450d, "ChapProcessPrivate", ElementType.Binary),
            new ElementDescriptor(0x6911, "ChapProcessCommand", ElementType.MasterElement),
            new ElementDescriptor(0x6922, "ChapProcessTime", ElementType.UnsignedInteger),
            new ElementDescriptor(0x6933, "ChapProcessData", ElementType.Binary),

            TagsElement,
            new ElementDescriptor(0x7373, "Tag", ElementType.MasterElement),
            new ElementDescriptor(0x63c0, "Targets", ElementType.MasterElement),
            new ElementDescriptor(0x68ca, "TargetTypeValue", ElementType.UnsignedInteger),
            new ElementDescriptor(0x63ca, "TargetType", ElementType.AsciiString),
            new ElementDescriptor(0x63c5, "TrackUID", ElementType.UnsignedInteger),
            new ElementDescriptor(0x63c9, "EditionUID", ElementType.UnsignedInteger),
            new ElementDescriptor(0x63c4, "ChapterUID", ElementType.UnsignedInteger),
            new ElementDescriptor(0x63c6, "AttachmentUID", ElementType.UnsignedInteger),
            new ElementDescriptor(0x67c8, "SimpleTag", ElementType.MasterElement),
            new ElementDescriptor(0x45a3, "TagName", ElementType.Utf8String),
            new ElementDescriptor(0x447a, "TagLanguage", ElementType.AsciiString),
            new ElementDescriptor(0x4484, "TagDefault", ElementType.UnsignedInteger),
            new ElementDescriptor(0x4487, "TagString", ElementType.Utf8String),
            new ElementDescriptor(0x4485, "TagBinary", ElementType.Binary),
        };

    #endregion

    #region Properties

    /// <summary>
    /// Gets a list of attachments, is created after <see cref="ReadAttachmentsAsync"/> was called once.
    /// </summary>
    public IList<MatroskaConsts.MatroskaAttachment> Attachments
    {
      get { return _attachmentList; }
    }

    #endregion

    #region Constructor

    /// <summary>
    /// Constructs a new <see cref="MatroskaBinaryReader"/>.
    /// </summary>
    /// <param name="lfsra">
    /// <see cref="ILocalFsResourceAccessor"/> pointing to the MKV file to extract information from; The caller
    /// is responsible that it is valid while this class is used and that it is disposed afterwards.
    /// </param>
    public MatroskaBinaryReader(ILocalFsResourceAccessor lfsra)
    {
      _lfsra = lfsra;
      _descriptorsMap = _elementDescriptors.Where(d => d != null).ToDictionary(d => d.Identifier.EncodedValue, d => d);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Reads all tags from matroska file and parses the XML for the requested tags (<paramref name="tagsToExtract"/>).
    /// </summary>
    /// <param name="tagsToExtract">Dictionary with tag names as keys.</param>
    public Task ReadTagsAsync(IDictionary<string, IList<string>> tagsToExtract)
    {
      ReadFile(SegmentInfoElement, TagsElement, TracksElement);
      string[] keys = tagsToExtract.Keys.ToArray();
      foreach (var tag in keys)
      {
        if (tag == "TITLE")
        {
          if (_infoProps?.ContainsKey("Title") ?? false)
            tagsToExtract[tag] = new List<string>(_infoProps["Title"]);
        }
        else if (tag == "DURATION")
        {
          if (_infoProps?.ContainsKey("Duration") ?? false)
            tagsToExtract[tag] = new List<string>(_infoProps["Duration"]);
        }
        else if (_tags?.ContainsKey(tag) ?? false)
        {
          tagsToExtract[tag] = new List<string>(_tags[tag]);
        }
      }
      return Task.CompletedTask;
    }

    /// <summary>
    /// Reads the stereoscopic information from the matroska file.
    /// </summary>
    public Task<MatroskaConsts.StereoMode> ReadStereoModeAsync()
    {
      ReadFile(TracksElement);
      string val = null;
      if (_videoProps?.ContainsKey("StereoMode") ?? false)
        val = _videoProps["StereoMode"].FirstOrDefault();

      if (!string.IsNullOrEmpty(val))
        return Task.FromResult((MatroskaConsts.StereoMode)Convert.ToInt32(val));

      return Task.FromResult(MatroskaConsts.StereoMode.Mono);
    }

    /// <summary>
    /// Reads the attachment information from the matroska file.
    /// </summary>
    public Task ReadAttachmentsAsync()
    {
      ReadFile(AttachmentsElement);

      return Task.CompletedTask;
    }

    /// <summary>
    /// Tries to extract an embedded cover from the matroska file. It checks attachments for a matching filename (cover.jpg/png).
    /// <para>
    /// <seealso cref="http://www.matroska.org/technical/cover_art/index.html"/>:
    /// The way to differentiate between all these versions is by the filename. The default filename is cover.(png/jpg) for backward compatibility reasons. 
    /// That is the "big" version of the file (600) in square or portrait mode. It should also be the first file in the attachments. 
    /// The smaller resolution should be prefixed with "small_", ie small_cover.(jpg/png). The landscape variant should be suffixed with "_land", ie cover_land.jpg. 
    /// The filenames are case sensitive and should all be lower case.
    /// In the end a file could contain these 4 basic cover art files:
    ///  cover.jpg (portrait/square 600)
    ///  small_cover.png (portrait/square 120)
    ///  cover_land.png (landscape 600)
    ///  small_cover_land.jpg (landscape 120)
    /// </para>
    /// </summary>
    /// <returns>Returns the binary data if successful, else <c>null</c></returns>
    public Task<byte[]> GetCoverAsync()
    {
      return Task.FromResult(_attachments?.Where(a => a.Key.StartsWith("cover.", StringComparison.InvariantCultureIgnoreCase)).Select(a => a.Value).FirstOrDefault());
    }

    /// <summary>
    /// Tries to extract an embedded landscape cover from the matroska file. <seealso cref="GetCoverAsync"/> for more information about naming conventions.
    /// </summary>
    /// <returns>Returns the binary data if successful, else <c>null</c></returns>
    public Task<byte[]> GetCoverLandscape()
    {
      return Task.FromResult(_attachments?.Where(a => a.Key.StartsWith("cover_land.", StringComparison.InvariantCultureIgnoreCase)).Select(a => a.Value).FirstOrDefault());
    }

    /// <summary>
    /// Tries to extract an attachment by its name.
    /// </summary>
    /// <param name="fileNamePart">Beginning of filename</param>
    /// <returns>Returns the binary data if successful, else <c>null</c></returns>
    public Task<byte[]> GetAttachmentByNameAsync(string fileNamePart)
    {
      return Task.FromResult(_attachments?.Where(a => a.Key.StartsWith(fileNamePart, StringComparison.InvariantCultureIgnoreCase)).Select(a => a.Value).FirstOrDefault());
    }

    #endregion

    #region Private methods

    private bool ShouldReadElement(VInt element, params ElementDescriptor[] wantedElements)
    {
      if (_elementsRead.Contains(element.EncodedValue))
        return false;
      if ((wantedElements.Length == 0 || wantedElements.Any(e => e.Identifier.EncodedValue == element.EncodedValue)))
        return true;

      return false;
    }

    private void ReadFile(params ElementDescriptor[] elements)
    {
      IEnumerable<ElementDescriptor> wantedElements = elements;
      if (wantedElements.Count() > 0 && wantedElements.All(e => _elementsRead.Contains(e.Identifier.EncodedValue)))
        return;

      SortedSet<long> wantedElementPositions = new SortedSet<long>();
      using (_lfsra.EnsureLocalFileSystemAccess())
      using (var fs = new FileStream(_lfsra.LocalFileSystemPath, FileMode.Open, FileAccess.Read))
      using (EbmlReader ebmlReader = new EbmlReader(fs))
      {
        if (fs.Length == 0)
          return;

        try
        {
          while (ebmlReader.ReadNext())
          {
            //Segment element contains all the data we need
            if (ebmlReader.ElementId == SegmentElement.Identifier)
            {
              ebmlReader.EnterContainer();
              while (ebmlReader.ReadNext())
              {
                //Find file position of relevant elements via seek heads
                if (ebmlReader.ElementId == SeekHeadElement.Identifier)
                {
                  long elementOffset = ebmlReader.ElementPosition;
                  var seekElements = ReadSeekHeads(ebmlReader);
                  if (seekElements?.Count > 0)
                  {
                    //Find all the wanted or possible elements
                    var availableSegments = seekElements.Select(a => a.Element);
                    var newElements = wantedElements.Count() > 0 ? wantedElements.Intersect(availableSegments) : availableSegments.AsEnumerable();
                    if (newElements != null)
                      wantedElements = newElements;

                    //Find file positions of wanted elements
                    foreach (var e in seekElements.Where(e => wantedElements.Any(n => n.Identifier == e.Element.Identifier)))
                      wantedElementPositions.Add(Convert.ToInt64(e.Position) + elementOffset);
                  }
                }
                else if (ShouldReadElement(ebmlReader.ElementId, wantedElements.ToArray()))
                {
                  //Read wanted elements
                  if (ebmlReader.ElementId == SegmentInfoElement.Identifier)
                    ReadInfo(ebmlReader);
                  else if (ebmlReader.ElementId == TagsElement.Identifier)
                    ReadTags(ebmlReader);
                  else if (ebmlReader.ElementId == TracksElement.Identifier)
                    ReadTracks(ebmlReader);
                  else if (ebmlReader.ElementId == AttachmentsElement.Identifier)
                    ReadAttachments(ebmlReader);

                  _elementsRead.Add(ebmlReader.ElementId.EncodedValue);
                }

                //All wanted elements read so exit
                if (wantedElements.Count() == 0 || wantedElements.All(e => _elementsRead.Contains(e.Identifier.EncodedValue)))
                  return;

                //Skip to next position in file if possible
                wantedElementPositions.RemoveWhere(p => p < (fs.Position));
                if (fs.Position < wantedElementPositions.FirstOrDefault())
                  fs.Position = wantedElementPositions.First();

                //Exit if we reading past the file length
                if ((fs.Position + ebmlReader.ElementSize) >= fs.Length)
                  return;
              }
              ebmlReader.LeaveContainer();
              break;
            }
            //Exit if we reading past the file length
            if ((fs.Position + ebmlReader.ElementSize) >= fs.Length)
              break;
          }
        }
        catch (EbmlDataFormatException)
        {
          //Rest of the EBML seems to be invalid so ignore it
          ServiceRegistration.Get<ILogger>().Warn("MatroskaInfoReader: Matroska file '{0}' has invalid EBML elements", _lfsra.LocalFileSystemPath);
        }
      }
    }

    private List<(ElementDescriptor Element, ulong Position)> ReadSeekHeads(EbmlReader ebmlReader)
    {
      List<(ElementDescriptor, ulong)> availableElements = null;
      ElementDescriptor desc;
      ElementDescriptor availDesc = null;
      ulong pos = 0;
      ebmlReader.EnterContainer();
      while (ebmlReader.ReadNext())
      {
        if (_descriptorsMap.TryGetValue(ebmlReader.ElementId.EncodedValue, out desc))
        {
          if (desc.Name == "Seek")
          {
            ebmlReader.EnterContainer();
            try
            {
              while (ebmlReader.ReadNext())
              {

                if (_descriptorsMap.TryGetValue(ebmlReader.ElementId.EncodedValue, out desc))
                {
                  if (desc.Name == "SeekID")
                  {
                    byte[] data = new byte[ebmlReader.ElementSize];
                    ebmlReader.ReadBinary(data, 0, data.Length);
                    MemoryStream mem = new MemoryStream(data);
                    byte[] temp = new byte[data.Length];
                    VInt id = VInt.Read(mem, data.Length, temp);
                    _descriptorsMap.TryGetValue(id.EncodedValue, out availDesc);
                  }
                  else if (desc.Name == "SeekPosition")
                  {
                    pos = ebmlReader.ReadUInt();
                  }
                }
              }

              if (availableElements == null)
                availableElements = new List<(ElementDescriptor, ulong)>();
              if (availDesc != null && pos > 0)
                availableElements.Add((availDesc, pos));
            }
            catch { }
          }
          ebmlReader.LeaveContainer();
        }
      }
      ebmlReader.LeaveContainer();
      return availableElements;
    }

    private void ReadInfo(EbmlReader ebmlReader)
    {
      _infoProps = new Dictionary<string, IList<string>>();
      ElementDescriptor desc;
      ebmlReader.EnterContainer();
      while (ebmlReader.ReadNext())
      {
        if (_descriptorsMap.TryGetValue(ebmlReader.ElementId.EncodedValue, out desc))
        {
          if (!_infoProps.ContainsKey(desc.Name))
            _infoProps.Add(desc.Name, new List<string>());

          if (desc.Type == ElementType.AsciiString)
            _infoProps[desc.Name].Add(ebmlReader.ReadAscii());
          else if (desc.Type == ElementType.Date)
            _infoProps[desc.Name].Add(ebmlReader.ReadDate().ToString(CultureInfo.InvariantCulture));
          else if (desc.Type == ElementType.Float)
            _infoProps[desc.Name].Add(ebmlReader.ReadFloat().ToString(CultureInfo.InvariantCulture));
          else if (desc.Type == ElementType.SignedInteger)
            _infoProps[desc.Name].Add(ebmlReader.ReadInt().ToString(CultureInfo.InvariantCulture));
          else if (desc.Type == ElementType.UnsignedInteger)
            _infoProps[desc.Name].Add(ebmlReader.ReadUInt().ToString(CultureInfo.InvariantCulture));
          else if (desc.Type == ElementType.Utf8String)
            _infoProps[desc.Name].Add(ebmlReader.ReadUtf());
        }
      }
      ebmlReader.LeaveContainer();
    }

    private void ReadTags(EbmlReader ebmlReader)
    {
      _tags = new Dictionary<string, IList<string>>();
      ElementDescriptor desc;
      ebmlReader.EnterContainer();
      while (ebmlReader.ReadNext())
      {
        if (_descriptorsMap.TryGetValue(ebmlReader.ElementId.EncodedValue, out desc) && desc.Name == "Tag")
          ReadTag(ebmlReader);
      }
      ebmlReader.LeaveContainer();
    }

    private void ReadTag(EbmlReader ebmlReader)
    {
      ulong targetValue = 0;
      ElementDescriptor desc;
      ebmlReader.EnterContainer();
      while (ebmlReader.ReadNext())
      {
        if (_descriptorsMap.TryGetValue(ebmlReader.ElementId.EncodedValue, out desc))
        {
          if (desc.Name == "Targets")
          {
            ebmlReader.EnterContainer();
            while (ebmlReader.ReadNext())
            {
              if (_descriptorsMap.TryGetValue(ebmlReader.ElementId.EncodedValue, out desc) && desc.Name == "TargetTypeValue")
                targetValue = ebmlReader.ReadUInt();
            }
            ebmlReader.LeaveContainer();
          }
          else if (desc.Name == "SimpleTag")
          {
            string tagName = null;
            ebmlReader.EnterContainer();
            while (ebmlReader.ReadNext())
            {
              if (_descriptorsMap.TryGetValue(ebmlReader.ElementId.EncodedValue, out desc))
              {
                if (desc.Name == "TagName")
                  tagName = ebmlReader.ReadUtf();
                else if (desc.Name == "TagString")
                {
                  string key = $"{targetValue}.{tagName}";
                  if (!_tags.ContainsKey(key))
                    _tags.Add(key, new List<string> { ebmlReader.ReadUtf() });
                  else
                    _tags[key].Add(ebmlReader.ReadUtf());
                }
              }
            }
            ebmlReader.LeaveContainer();
          }
        }
      }
      ebmlReader.LeaveContainer();
    }

    private void ReadTracks(EbmlReader ebmlReader)
    {
      _audioProps = new Dictionary<string, IList<string>>();
      _videoProps = new Dictionary<string, IList<string>>();
      ElementDescriptor desc;
      ebmlReader.EnterContainer();
      while (ebmlReader.ReadNext())
      {
        if (_descriptorsMap.TryGetValue(ebmlReader.ElementId.EncodedValue, out desc) && desc.Name == "TrackEntry")
          ReadTrack(ebmlReader);
      }
      ebmlReader.LeaveContainer();
    }

    private void ReadTrack(EbmlReader ebmlReader)
    {
      ElementDescriptor desc;
      ebmlReader.EnterContainer();
      while (ebmlReader.ReadNext())
      {
        if (ebmlReader.ElementId == VideoElement.Identifier)
        {
          ebmlReader.EnterContainer();
          while (ebmlReader.ReadNext())
          {
            if (_descriptorsMap.TryGetValue(ebmlReader.ElementId.EncodedValue, out desc))
            {
              if (!_videoProps.ContainsKey(desc.Name))
                _videoProps.Add(desc.Name, new List<string>());

              if (desc.Type == ElementType.AsciiString)
                _videoProps[desc.Name].Add(ebmlReader.ReadAscii());
              else if (desc.Type == ElementType.Date)
                _videoProps[desc.Name].Add(ebmlReader.ReadDate().ToString(CultureInfo.InvariantCulture));
              else if (desc.Type == ElementType.Float)
                _videoProps[desc.Name].Add(ebmlReader.ReadFloat().ToString(CultureInfo.InvariantCulture));
              else if (desc.Type == ElementType.SignedInteger)
                _videoProps[desc.Name].Add(ebmlReader.ReadInt().ToString(CultureInfo.InvariantCulture));
              else if (desc.Type == ElementType.UnsignedInteger)
                _videoProps[desc.Name].Add(ebmlReader.ReadUInt().ToString(CultureInfo.InvariantCulture));
              else if (desc.Type == ElementType.Utf8String)
                _videoProps[desc.Name].Add(ebmlReader.ReadUtf());
            }
          }
          ebmlReader.LeaveContainer();
        }
        else if (ebmlReader.ElementId == AudioElement.Identifier)
        {
          ebmlReader.EnterContainer();
          while (ebmlReader.ReadNext())
          {
            if (_descriptorsMap.TryGetValue(ebmlReader.ElementId.EncodedValue, out desc))
            {
              if (!_audioProps.ContainsKey(desc.Name))
                _audioProps.Add(desc.Name, new List<string>());

              if (desc.Type == ElementType.AsciiString)
                _audioProps[desc.Name].Add(ebmlReader.ReadAscii());
              else if (desc.Type == ElementType.Date)
                _audioProps[desc.Name].Add(ebmlReader.ReadDate().ToString(CultureInfo.InvariantCulture));
              else if (desc.Type == ElementType.Float)
                _audioProps[desc.Name].Add(ebmlReader.ReadFloat().ToString(CultureInfo.InvariantCulture));
              else if (desc.Type == ElementType.SignedInteger)
                _audioProps[desc.Name].Add(ebmlReader.ReadInt().ToString(CultureInfo.InvariantCulture));
              else if (desc.Type == ElementType.UnsignedInteger)
                _audioProps[desc.Name].Add(ebmlReader.ReadUInt().ToString(CultureInfo.InvariantCulture));
              else if (desc.Type == ElementType.Utf8String)
                _audioProps[desc.Name].Add(ebmlReader.ReadUtf());
            }
          }
          ebmlReader.LeaveContainer();
        }
      }
      ebmlReader.LeaveContainer();
    }

    private void ReadAttachments(EbmlReader ebmlReader)
    {
      _attachments = new Dictionary<string, byte[]>();
      _attachmentList = new List<MatroskaConsts.MatroskaAttachment>();
      ElementDescriptor desc;
      ebmlReader.EnterContainer();
      while (ebmlReader.ReadNext())
      {
        if (_descriptorsMap.TryGetValue(ebmlReader.ElementId.EncodedValue, out desc) && desc.Name == "AttachedFile")
          ReadAttachment(ebmlReader);
      }
      ebmlReader.LeaveContainer();
    }

    private void ReadAttachment(EbmlReader ebmlReader)
    {
      MatroskaConsts.MatroskaAttachment attachment = new MatroskaConsts.MatroskaAttachment();
      ElementDescriptor desc;
      ebmlReader.EnterContainer();
      while (ebmlReader.ReadNext())
      {
        if (_descriptorsMap.TryGetValue(ebmlReader.ElementId.EncodedValue, out desc))
        {
          if (desc.Name == "FileName")
            attachment.FileName = ebmlReader.ReadUtf();
          else if (desc.Name == "FileMimeType")
            attachment.MimeType = ebmlReader.ReadAscii();
          else if (desc.Name == "FileData")
          {
            byte[] data = new byte[ebmlReader.ElementSize];
            ebmlReader.ReadBinary(data, 0, data.Length);
            _attachments.Add(attachment.FileName, data);

            attachment.FileSize = ebmlReader.ElementSize;
          }
        }
      }
      _attachmentList.Add(attachment);
      ebmlReader.LeaveContainer();
    }

    #endregion
  }
}
