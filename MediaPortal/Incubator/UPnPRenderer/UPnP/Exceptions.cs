using System;

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
