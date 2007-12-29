using System;
using System.Collections.Generic;
using System.Text;
using SkinEngine.Controls.Visuals;
using SkinEngine.Controls.Panels;
namespace SkinEngine.Controls.Visuals.Styles
{
  public class ControlTemplate : Canvas
  {
    public ControlTemplate()
    {
    }
    public ControlTemplate(ControlTemplate t)
      : base(t)
    {
    }

    public override object Clone()
    {
      return new ControlTemplate(this);
    }
  }
}
