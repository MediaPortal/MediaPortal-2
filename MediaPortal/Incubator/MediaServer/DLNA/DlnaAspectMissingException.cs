using System;

namespace MediaPortal.Plugins.MediaServer.DLNA
{
  class DlnaAspectMissingException : Exception
  {
    public DlnaAspectMissingException(string message) :
      base(message)
    { }
  }
}
