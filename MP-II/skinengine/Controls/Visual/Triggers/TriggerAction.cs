using System;
using System.Collections.Generic;
using System.Text;

namespace SkinEngine.Controls.Visuals.Triggers
{
  public class TriggerAction : ICloneable
  {
    public TriggerAction()
    {
    }
    public TriggerAction(TriggerAction action)
    {
    }

    public virtual object Clone()
    {

      return new TriggerAction(this);
    }

  
    public virtual void Execute(UIElement element, Trigger trigger)
    {
    }

    public virtual object GetOriginalValue(UIElement element)
    {
      return null;
    }
    public virtual void Setup(UIElement element)
    {
    }
  }
}
