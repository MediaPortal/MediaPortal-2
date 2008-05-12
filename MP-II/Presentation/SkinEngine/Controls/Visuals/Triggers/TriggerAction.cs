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
using Presentation.SkinEngine.Controls;
using Presentation.SkinEngine.MarkupExtensions;

namespace Presentation.SkinEngine.Controls.Visuals.Triggers
{
  public class TriggerAction: DependencyObject, ICloneable
  {
    public TriggerAction(): base()
    {
    }

    public TriggerAction(TriggerAction action): base(action)
    {
    }

    public virtual object Clone()
    {
      TriggerAction result = new TriggerAction(this);
      BindingMarkupExtension.CopyBindings(this, result);
      return result;
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
      GetOrCreateDataContext().Source = element;
    }
  }
}
