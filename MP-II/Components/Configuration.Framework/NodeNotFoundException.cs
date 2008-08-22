using System;
using System.Collections.Generic;
using System.Text;

namespace MediaPortal.Configuration
{
  public class NodeNotFoundException : Exception
  {

    public NodeNotFoundException()
      : base()
    {
    }

    public NodeNotFoundException(string message)
      : base(message)
    {
    }

    public NodeNotFoundException(string message, Exception innerException)
      : base(message, innerException)
    {
    }

  }
}
