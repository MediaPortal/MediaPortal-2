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

using System.Reflection;
using MediaPortal.Utilities.DeepCopy;

namespace Presentation.SkinEngine.Controls.Bindings
{
  public class Command : IDeepCopyable
  {
    MethodInfo _info;
    object _object;
    object _parameter;

    #region Ctor

    public Command()
    { }

    public virtual void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Command c = source as Command;
      _info = copyManager.GetCopy(c._info);
      _object = copyManager.GetCopy(c._object);
      _parameter = copyManager.GetCopy(c._parameter);
    }

    #endregion

    #region Public properties

    public MethodInfo Method
    {
      get { return _info; }
      set { _info = value; }
    }

    public object Object
    {
      get { return _object; }
      set { _object = value; }
    }

    public object Parameter
    {
      get { return _parameter; }
      set { _parameter = value; }
    }

    #endregion

    public virtual void Execute(object commandParameter, bool hasParameter)
    {
      // FIXME Albert78: set hasParameter in Parameter property setter?
      if (commandParameter != null || hasParameter)
        _info.Invoke(_object, new object[] { commandParameter });
      else
        _info.Invoke(_object, null);
    }

    public virtual void Execute()
    {
      if (Parameter != null)
        _info.Invoke(_object, new object[] { Parameter });
      else
        _info.Invoke(_object, null);
    }
  }
}
