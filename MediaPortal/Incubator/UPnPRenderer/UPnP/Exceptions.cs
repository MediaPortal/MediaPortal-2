using System;

namespace MediaPortal.UPnPRenderer.UPnP
{
  class UPnPRendererExceptions : Exception
  {
    public UPnPRendererExceptions()
    {
    }

    public UPnPRendererExceptions(string message, params object[] args)
      : this(string.Format(message, args))
    {
    }

    public UPnPRendererExceptions(string message)
      : base(message)
    {
    }
  }
}
