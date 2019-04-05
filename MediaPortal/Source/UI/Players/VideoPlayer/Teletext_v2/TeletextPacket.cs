namespace MediaPortal.UI.Players.Video.Teletext_v2
{
  public class TeletextPacket
  {
    public const byte DataUnitEbuTeletextNonsubtitle = 0x02;
    public const byte DataUnitEbuTeletextSubtitle = 0x03;
    public const byte DataUnitEbuTeletextInverted = 0x0c;
    public const byte DataUnitVps = 0xc3;
    public const byte DataUnitClosedCaptions = 0xc5;
    public const byte DataUnitStuffing = 0xff;

    private byte[] _data;

    public TeletextPacket(byte dataUnitId, byte dataUnitLength, long pts)
    {
      DataUnitId = dataUnitId;
      DataUnitLength = dataUnitLength;
      Pts = pts;
    }

    internal TeletextPacket(TeletextPacket clone)
    {
      DataUnitId = clone.DataUnitId;
      DataUnitLength = clone.DataUnitLength;
      Pts = clone.Pts;
      _data = clone.Data;
      ParseDataField();
    }

    public ushort Magazine { get; private set; }

    public int Row { get; private set; } = -1;

    public byte DataUnitId { get; }

    public byte DataUnitLength { get; }

    public long Pts { get; set; }

    public byte[] Data
    {
      get { return _data; }
      set
      {
        if (DataUnitId == DataUnitStuffing) return;

        if (DataUnitId != DataUnitEbuTeletextNonsubtitle &&
            DataUnitId != DataUnitEbuTeletextSubtitle) return;

        _data = value;

        //ETS 300 706 7.1
        Utils.ReverseArray(ref _data, 2, DataUnitLength);

        ParseDataField();
      }
    }

    private void ParseDataField()
    {
      var address = (byte)((Utils.UnHam84(_data[5]) << 4) + Utils.UnHam84(_data[4]));

      Magazine = (byte)(address & 0x7);
      if (Magazine == 0)
        Magazine = 8;

      Row = (byte)((address >> 3) & 0x1f);
    }
  }
}
