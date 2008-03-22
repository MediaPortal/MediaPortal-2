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
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using MediaPortal.Presentation.Properties;
using SkinEngine.Controls.Visuals;
using MyXaml.Core;
namespace SkinEngine.Controls.Bindings
{
  public class CommandGroup : List<InvokeCommand>, IAddChild
  {
    public CommandGroup()
    {
    }

    public CommandGroup(CommandGroup c)
    {
      foreach (InvokeCommand cmd in c)
      {
        Add((InvokeCommand)cmd.Clone());
      }
    }


    public virtual object Clone()
    {
      return new CommandGroup(this);
    }

    void Init()
    {
    }

    public void Execute(UIElement element)
    {
      foreach (InvokeCommand cmd in this)
      {
        cmd.Execute(element);
      }
    }


    #region IAddChild Members

    public void AddChild(object o)
    {
      Add((InvokeCommand)o);
    }

    #endregion
  }
}
