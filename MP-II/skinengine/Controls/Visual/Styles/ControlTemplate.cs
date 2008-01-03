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

    /// <summary>
    /// Gets or sets the type of the target (not used here, but required for real xaml)
    /// </summary>
    /// <value>The type of the target.</value>
    public string TargetType
    {
      get
      {
        return "";
      }
      set
      {
      }
    }

  }
}
