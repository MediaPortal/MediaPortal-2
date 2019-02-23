using System.IO;

namespace MediaPortal.UI.Players.Video.Teletext_v2
{
  public class TeletextHeaderPacket : TeletextPacket
  {
    public ushort Page { get; }

    public bool EraseFlag { get; }

    public int Subcode { get; }

    public TeletextHeaderPacket(TeletextPacket basePacket) : base(basePacket)
    {
      if (basePacket.Row != 0)
        throw new InvalidDataException("Non-header packet passed to constructor of header packet class");

      Page = (ushort)((Utils.UnHam84(Data[7]) << 4) + Utils.UnHam84(Data[6]));
      EraseFlag = (Utils.UnHam84(Data[9]) & 0x08) == 8;

      Subcode = (Utils.UnHam84(Data[11]) << 24) + (Utils.UnHam84(Data[10]) << 16) + (Utils.UnHam84(Data[9]) << 8) + Utils.UnHam84(Data[8]);
    }
  }
}
