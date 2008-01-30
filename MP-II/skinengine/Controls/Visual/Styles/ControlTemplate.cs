using System;
using System.Collections.Generic;
using System.Text;
using SkinEngine.Controls.Visuals;
using SkinEngine.Controls.Panels;
using SkinEngine.Controls.Visuals.Triggers;
using MediaPortal.Core.Properties;

namespace SkinEngine.Controls.Visuals.Styles
{
  public class ControlTemplate : FrameworkTemplate
  {
    Property _triggerProperty;

    #region ctor
    /// <summary>
    /// pecifies the visual structure and behavioral aspects of a Control that can be shared across multiple instances of the control.
    /// </summary>
    public ControlTemplate()
    {
      Init();
    }

    public ControlTemplate(ControlTemplate ct)
      : base(ct)
    {
      Init();
      foreach (Trigger t in ct.Triggers)
      {
        Triggers.Add((Trigger)t.Clone());
      }
    }

    void Init()
    {
      _triggerProperty = new Property(new TriggerCollection());
    }

    public override object Clone()
    {
      return new ControlTemplate(this);
    }
    #endregion

    #region properties
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

    /// <summary>
    /// Gets or sets the triggers property.
    /// </summary>
    /// <value>The triggers property.</value>
    public Property TriggersProperty
    {
      get
      {
        return _triggerProperty;
      }
      set
      {
        _triggerProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the triggers.
    /// </summary>
    /// <value>The triggers.</value>
    public TriggerCollection Triggers
    {
      get
      {
        return (TriggerCollection)_triggerProperty.GetValue();
      }
    }
    #endregion

  }
}
