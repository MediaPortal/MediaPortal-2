#region Copyright (C) 2007 Team MediaPortal

/*
    Copyright (C) 2007 Team MediaPortal
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
using System.Diagnostics;
using MediaPortal.Core.Properties;
using Microsoft.DirectX;
using SkinEngine.Controls;
using SkinEngine.Properties;

namespace SkinEngine.Skin.Layout
{
  public class StackLayout : ILayout
  {
    private Group _group;

    public StackLayout(Group group)
    {
      _group = group;
      Perform();
    }


    public void Perform()
    {
      if (_group.Controls.Count <= 1)
      {
        return;
      }
      float height = 0;

      for (int i = 0; i < _group.Controls.Count; ++i)
      {
        height += _group.Controls[i].Height;
      }
      int items = _group.Controls.Count;
      float offset = 0;

      //Trace.WriteLine(String.Format("group:{0} {1},{2}", _group.Name,_group.Position.X, _group.Position.Y));
      for (int i = 0; i < _group.Controls.Count; ++i)
      {
        Control c = _group.Controls[i];
        PositionDependency depend=c.PositionProperty as PositionDependency;
        if (depend != null)
        {
          depend.Offset = new Vector3(0, offset, 0);
        }
        else
        {
          Property newProp = new Property(new Vector3(c.OriginalPosition.X, offset + c.OriginalPosition.Y, c.OriginalPosition.Z));
          c.PositionProperty = new PositionDependency(_group.PositionProperty, newProp);
        }
        offset += c.Height;// (height / ((float)items));

       // Trace.WriteLine(String.Format("  cntl:{0},{1} h:{2}", c.Position.X, c.Position.Y,c.Height));
      }
    }
  }
}