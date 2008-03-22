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
using System.Diagnostics;
using System.Collections;
using System.Text;
using System.Drawing.Drawing2D;
using MediaPortal.Presentation.Properties;
using MediaPortal.Control.InputManager;
using Presentation.SkinEngine.Controls.Brushes;
using SlimDX;
using SlimDX.Direct3D;
using SlimDX.Direct3D9;
using Presentation.SkinEngine;
using Presentation.SkinEngine.DirectX;
using RectangleF = System.Drawing.RectangleF;
using PointF = System.Drawing.PointF;
using SizeF = System.Drawing.SizeF;
using Matrix = SlimDX.Matrix;

namespace Presentation.SkinEngine.Controls.Visuals
{
  public class TreeViewItem : HeaderedItemsControl
  {
    public TreeViewItem()
    {
      Init();
    }

    public TreeViewItem(TreeViewItem c)
      : base(c)
    {
      Init();
    }

    public override object Clone()
    {
      return new TreeViewItem(this);
    }


    void Init()
    {
    } 
  }
}
