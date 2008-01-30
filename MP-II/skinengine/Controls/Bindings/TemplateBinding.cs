#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion
using System;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Text;
using SkinEngine.Controls.Visuals;
using SkinEngine.Controls.Visuals.Styles;
using MediaPortal.Core;
using MediaPortal.Core.Properties;
using MediaPortal.Core.Logging;
using MediaPortal.Core.Collections;
using MediaPortal.Core.WindowManager;

namespace SkinEngine.Controls.Bindings
{
  public class TemplateBinding : Binding
  {
    public TemplateBinding()
    {
    }

    public TemplateBinding(TemplateBinding bind)
      : base(bind)
    {
    }

    public override object Clone()
    {
      return new TemplateBinding(this);
    }

    /// <summary>
    /// Gets or sets the expression.
    /// </summary>
    /// <value>The expression.</value>
    public override string Expression
    {
      get
      {
        return _expression;
      }
      set
      {
        if (!value.StartsWith("this."))
          _expression = "this." + value;
        else
          _expression = value;
      }
    }
    public override void Initialize(object bindingDestinationObject)
    {
      Visual visual = bindingDestinationObject as Visual;
      while (visual.VisualParent != null && !(visual is SkinEngine.Controls.Visuals.Control))
        visual = visual.VisualParent;
      //visual = visual.VisualParent;

      object bindingSourceProperty = GetBindingSourceObject((UIElement)visual, Expression);
      if (bindingSourceProperty == null)
      {
        bindingSourceProperty = VisualTreeHelper.Instance.FindElement((UIElement)visual, Expression + "Property");
        if (bindingSourceProperty == null)
        {
          bindingSourceProperty = VisualTreeHelper.Instance.FindElement((UIElement)visual, Expression);
          if (bindingSourceProperty == null)
          {
            ServiceScope.Get<ILogger>().Warn("Binding:'{0}' cannot find binding source element '{1}' on {2}", Expression, PropertyInfo.Name, visual);
            return;
          }
        }
      }

      object bindingDestProperty = GetBindingSourceObject((UIElement)bindingDestinationObject, PropertyInfo.Name);
      if (bindingDestProperty == null)
      {
        bindingDestProperty = VisualTreeHelper.Instance.FindElement((UIElement)bindingDestinationObject, "this." + PropertyInfo.Name + "Property");
        if (bindingDestProperty == null)
        {
          bindingDestProperty = VisualTreeHelper.Instance.FindElement((UIElement)bindingDestinationObject, "this." + PropertyInfo.Name);
          if (bindingDestProperty == null)
          {
            ServiceScope.Get<ILogger>().Warn("Binding:'{0}' cannot find binding Dest element '{1}' on {2}", PropertyInfo.Name, PropertyInfo.Name, bindingDestinationObject);
            return;
          }
        }
      
      }


      if (bindingSourceProperty is Property && bindingDestProperty is Property)
      {
        //create a new dependency..
        _dependency = new BindingDependency((Property)bindingSourceProperty, (Property)bindingDestProperty);

      }
    }
  }
}
