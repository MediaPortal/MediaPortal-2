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
using MediaPortal.Core.Properties;
using SkinEngine.Controls.Visuals;

namespace SkinEngine.Controls.Bindings
{
  public class CommandGroup : IList
  {
    List<InvokeCommand> _commands;

    public CommandGroup()
    {
      Init();
    }

    public CommandGroup(CommandGroup c)
    {
      Init();
      foreach (InvokeCommand cmd in c._commands)
      {
        _commands.Add((InvokeCommand)cmd.Clone());
      }
    }


    public virtual object Clone()
    {
      return new CommandGroup(this);
    }

    void Init()
    {
      _commands = new List<InvokeCommand>();
    }

    public void Execute(UIElement element)
    {
      foreach (InvokeCommand cmd in _commands)
      {
        cmd.Execute(element);
      }
    }

    #region IList Members

    public int Add(object value)
    {
      _commands.Add((InvokeCommand)value);
      return _commands.Count;
    }

    public void Clear()
    {
      _commands.Clear();
    }

    public bool Contains(object value)
    {
      return _commands.Contains((InvokeCommand)value);
    }

    public int IndexOf(object value)
    {
      return _commands.IndexOf((InvokeCommand)value);
    }

    public void Insert(int index, object value)
    {
      _commands.Insert(index, (InvokeCommand)value);
    }

    public bool IsFixedSize
    {
      get
      {
        return false;
      }
    }

    public bool IsReadOnly
    {
      get
      {
        return false;
      }
    }

    public void Remove(object value)
    {
      _commands.Remove((InvokeCommand)value);
    }

    public void RemoveAt(int index)
    {
      _commands.RemoveAt(index);
    }

    public object this[int index]
    {
      get
      {
        return _commands[index];
      }
      set
      {
        _commands[index] = (InvokeCommand)value;
      }
    }

    #endregion

    #region ICollection Members

    public void CopyTo(Array array, int index)
    {
      throw new Exception("The method or operation is not implemented.");
    }

    public int Count
    {
      get
      {
        return _commands.Count;
      }
    }

    public bool IsSynchronized
    {
      get { throw new Exception("The method or operation is not implemented."); }
    }

    public object SyncRoot
    {
      get { throw new Exception("The method or operation is not implemented."); }
    }

    #endregion

    #region IEnumerable Members

    public IEnumerator GetEnumerator()
    {
      throw new Exception("The method or operation is not implemented.");
    }

    #endregion
  }
}
