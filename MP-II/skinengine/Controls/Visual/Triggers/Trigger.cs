using System;
using System.Collections.Generic;
using System.Text;

namespace SkinEngine.Controls.Visuals.Triggers
{
  public class Trigger : ICloneable
  {

    public Trigger()
    {
    }

    public Trigger(Trigger trig)
    {
    }

    public virtual object Clone()
    {
      return new Trigger(this);
    }

  }
}
