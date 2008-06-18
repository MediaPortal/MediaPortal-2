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
using System.Collections.Generic;
using Presentation.SkinEngine.Controls.Visuals;
using Presentation.SkinEngine.XamlParser;
using MediaPortal.Utilities.DeepCopy;

namespace Presentation.SkinEngine.Controls.Bindings
{
  using System.Collections;

  public class CommandGroup : DependencyObject, IAddChild<InvokeCommand>, IEnumerable<InvokeCommand>, IDeepCopyable
  {
    #region Protected fields

    protected IList<InvokeCommand> _elements;

    #endregion

    #region Ctor

    public CommandGroup()
    {
      Init();
    }

    public CommandGroup(UIElement owner)
    {
      Owner = owner;
      Init();
    }

    void Init()
    {
      _elements = new List<InvokeCommand>();
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      CommandGroup cg = source as CommandGroup;
      foreach (InvokeCommand ic in cg._elements)
        _elements.Add(copyManager.GetCopy(ic));
    }

    #endregion

    public UIElement Owner
    {
      get { return LogicalParent as UIElement; }
      set { LogicalParent = value; }
    }

    public void Execute(UIElement element)
    {
      foreach (InvokeCommand cmd in this)
        cmd.Execute(element);
    }

    #region IAddChild Members

    public void AddChild(InvokeCommand o)
    {
      if (o == null)
        throw new ArgumentException(string.Format("Can only add elements of type {0} to {1}",
          typeof(InvokeCommand).Name, typeof(CommandGroup).Name));
      o.Setup(Owner);
      _elements.Add(o);
    }

    #endregion

    #region IEnumerable<InvokeCommand> implementation

    IEnumerator<InvokeCommand> IEnumerable<InvokeCommand>.GetEnumerator()
    {
      return _elements.GetEnumerator();
    }

    #endregion

    #region IEnumerable implementation

    public IEnumerator GetEnumerator()
    {
      return ((IEnumerable<InvokeCommand>) this).GetEnumerator();
    }

    #endregion
  }
}
