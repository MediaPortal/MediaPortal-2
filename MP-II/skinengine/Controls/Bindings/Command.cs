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
using System.Collections.Generic;
using System.Text;

namespace SkinEngine.Controls.Bindings
{
  public class Command : ICloneable
  {
    MethodInfo _info;
    object _object;
    object _parameter;

    public Command()
    {
    }

    public Command(Command c)
    {
      Object = c.Object;
      Method = c.Method;
      Parameter = c.Parameter;
    }

    #region ICloneable Members

    public virtual object Clone()
    {
      return new Command(this);
    }


    #endregion

    public MethodInfo Method
    {
      get
      {
        return _info;
      }
      set
      {
        _info = value;
      }
    }

    public object Object
    {
      get
      {
        return _object;
      }
      set
      {
        _object = value;
      }
    }

    public object Parameter
    {
      get
      {
        return _parameter;
      }
      set
      {
        _parameter = value;
      }
    }

    public virtual void Execute(object commandParameter, bool hasParameter)
    {
      if (commandParameter != null || hasParameter)
      {
        _info.Invoke(_object, new object[] { commandParameter });
      }
      else
      {
        _info.Invoke(_object, null);
      }
    }
    public virtual void Execute()
    {
      if (Parameter != null)
      {
        _info.Invoke(_object, new object[] { Parameter });
      }
      else
      {
        _info.Invoke(_object, null);
      }
    }
  }
}
