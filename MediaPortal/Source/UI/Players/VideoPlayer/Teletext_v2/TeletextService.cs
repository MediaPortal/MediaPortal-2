using System.Collections.Generic;

namespace MediaPortal.UI.Players.Video.Teletext_v2
{
  public class TeletextService
  {
    //TODO: Actually support differences between parallel and serial...
    public const byte TransmissionModeParallel = 0;
    public const byte TransmissionModeSerial = 1;

    /// <summary>
    /// Byte indicating the TransmissionMode of the teletext pages (serial or parrallel)
    /// </summary>
    public byte TransmissionMode { get; set; }

    /// <summary>
    /// Reference PTS, used to calulate and display relative time offsets for data within stream
    /// </summary>
    public long ReferencePts { get; set; }

    /// <summary>
    /// The TS Packet ID that has been selected as the elementary stream containing teletext data
    /// </summary>
    public short TeletextPid { get; set; } = -1;

    /// <summary>
    /// The Program Number ID to which the selected teletext PID belongs, if any
    /// </summary>
    public ushort ProgramNumber { get; set; } = 0;

    /// <summary>
    /// The associated TeletextDescriptor for the service, if any
    /// </summary>
   // public TeletextDescriptor AssociatedDescriptor { get; set; }

    /// <summary>
    /// Optional value to restrict decoded pages to the specified magazine (reduces callback events)
    /// </summary>
    public int MagazineFilter { get; set; } = -1;

    //TODO: Implement
    /// <summary>
    /// Optional value to restrice decodec packets to the specified page number (tens and units element only - reduces callback events)
    /// </summary>
    public int PageFilter { get; set; } = -1;

    //TODO: Implement
    /// <summary>
    /// If true, only pages marked with the 'subtitle' control field will be returned via events
    /// </summary>
    public bool SubtitleFilter { get; set; }

    /// <summary>
    /// A Dictionary of Teletext Magazines, which themselves may contain a collection of pages
    /// </summary>
    public Dictionary<int, TeletextMagazine> Magazines { get; set; } = new Dictionary<int, TeletextMagazine>(9);

    public void OnTeletextPageReady(TeletextPage completedPage)
    {
      throw new System.NotImplementedException();
    }

    public void OnTeletextPageCleared(int currentPageNumber, long packetPts)
    {
      throw new System.NotImplementedException();
    }
  }
}
