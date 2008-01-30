using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace SkinEngine.Controls.Visuals.Triggers
{
  public class TriggerCollection : ArrayList
  {
    public void Merge(TriggerCollection triggers)
    {
      this.Clear();
      foreach(Trigger t in triggers)
      {
        this.Add(t);
      }
    }
  }
}
