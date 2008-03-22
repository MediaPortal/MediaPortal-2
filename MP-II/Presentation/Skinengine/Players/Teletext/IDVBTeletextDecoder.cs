using System;
using System.Collections.Generic;
using System.Text;

namespace SkinEngine.Players.Teletext
{
  public interface IDVBTeletextDecoder
  {
    void OnTeletextPacket(byte[] data, UInt64 presentTime);

    void OnServiceInfo(int page, byte type, string iso_lang);

    bool AcceptsDataUnitID(byte id);

    // TODO:
    // We need different types of reset
    // if a new channel we need to reset subtitle
    // info, otherwise we just need to reset the teletext cache
    void Reset();
  }
}
