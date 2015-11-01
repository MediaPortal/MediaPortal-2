using System;

namespace MediaPortal.Extensions.MediaServer.DLNA
{
  class DlnaAspectMissingException : Exception
  {
    public DlnaAspectMissingException(string message) :
      base(message)
    { }
  }
}
