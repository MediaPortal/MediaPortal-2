using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.Plugins.MediaServer.DLNA
{
  class DlnaAspectMissingException : Exception
  {
    public DlnaAspectMissingException(string message) :
      base(message)
    { }
  }
}
