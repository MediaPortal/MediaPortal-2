using System;
using System.Collections.Generic;
using System.Text;

namespace MyXaml.Core
{
  public class CustomTypeEventArgs : EventArgs
  {
    public Type PropertyType;
    public object Value;
    public object Result;

    public CustomTypeEventArgs()
    {
      Result = null;
    }
  }
}
