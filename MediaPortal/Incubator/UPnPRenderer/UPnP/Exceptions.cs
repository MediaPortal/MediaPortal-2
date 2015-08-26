using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.Extensions.UPnPRenderer
{
  class UPnPRendererExceptions : Exception
  {
    public UPnPRendererExceptions()
    {
    }

    public UPnPRendererExceptions(string message)
        : base(message)
    {
    }

  }
}
