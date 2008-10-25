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
using System.Runtime.Serialization;

namespace Components.UPnPServer
{
  [Serializable]
  internal class DvRootContainer2 : DvMediaContainer2
  {
    // Events
    internal event Delegate_OnContainerChanged OnContainerChanged;

    // Methods
    public DvRootContainer2() { }

    protected DvRootContainer2(SerializationInfo info, StreamingContext context) :
        base(info, context) { }

    internal void FireOnContainerChanged(DvMediaContainer2 thisChanged)
    {
      if (OnContainerChanged != null)
      {
        OnContainerChanged(this, thisChanged);
      }
    }

    // Properties
    public override bool IsRootContainer
    {
      get { return true; }
    }

    // Nested Types
    internal delegate void Delegate_OnContainerChanged(DvRootContainer2 sender, DvMediaContainer2 thisChanged);
  }
}
