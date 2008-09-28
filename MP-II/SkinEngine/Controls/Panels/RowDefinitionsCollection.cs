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

using System.Collections.Generic;
using MediaPortal.SkinEngine.Xaml.Interfaces;
using MediaPortal.Utilities.DeepCopy;
using MediaPortal.SkinEngine.SkinManagement;

namespace MediaPortal.SkinEngine.Controls.Panels
{
  public class RowDefinitionsCollection : List<RowDefinition>, IAddChild<RowDefinition>, IDeepCopyable
  {

    public virtual void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      RowDefinitionsCollection r = (RowDefinitionsCollection) source;
      foreach (RowDefinition rd in r)
        Add(copyManager.GetCopy(rd));
    }

    public float GetHeight(int row, int rowSpan)
    {
      float height = 0.0f;
      for (int i = 0; i < rowSpan; ++i)
      {
        height += (float)this[row + i].Height.Length;
      }
      return height;
    }

    public float GetOffset(int row)
    {
      float height = 0.0f;
      for (int i = 0; i < row; ++i)
      {
        height += (float)this[i].Height.Length;
      }
      return height;
    }

    public void ResetHeight()
    {
      foreach (RowDefinition rowDef in this)
      {
        if (rowDef.Height.IsAbsolute) // Fixed size can never change.
          rowDef.Height.Length = rowDef.Height.Value * SkinContext.Zoom.Height;
        else
          rowDef.Height.Length = 0.0;
      }
    }

    public double TotalHeight
    {
      get
      {
        double height = 0.0;
        foreach (RowDefinition rowDef in this)
        {
          height += rowDef.Height.Length;
        }
        return height;
      }
    }

    public void SetHeight(int row, int rowSpan, float height)
    {
      // Not set, don't bother.
      if (double.IsNaN(height))
        return;

      int relativeCount = 0;
      for (int i = 0; i < rowSpan; ++i)
      {
        RowDefinition rowDef = this[i + row];
        if (rowDef.Height.IsAbsolute)
        {
          height -= (float)rowDef.Height.Length;
        }
        else 
          relativeCount++;
      }
      for (int i = 0; i < rowSpan; ++i)
      {
        RowDefinition rowDef = this[i + row];
        if (rowDef.Height.IsAuto || rowDef.Height.IsStar)
        {

          if (rowDef.Height.Length < height / relativeCount)
          {
            rowDef.Height.Length = height / relativeCount;
          }
        }
      }
    }

    public void SetAvailableSize(double height)
    {
      double fixedHeight = 0.0f;
      int relativeCount = 0;
      double totalStar = 0;
      foreach (RowDefinition row in this)
      {
        if (row.Height.IsAbsolute)  // Fixed size controls get size from Height property
        {
          fixedHeight += row.Height.Length;
        }
        else if (row.Height.IsAuto) // Auto sized control should follow the child
        {
          fixedHeight += row.Height.Length;
        }
        else
        {
          row.Height.Length = 0;
          totalStar += row.Height.Value;
          relativeCount++;
        }

        // Too much allocated
        if (fixedHeight > height)
        {
          row.Height.Length -= fixedHeight - height;
          fixedHeight = height;
        }
      }
      if (height == 0.0) 
        return;
      height -= fixedHeight;
      foreach (RowDefinition row in this)
      {
        if (row.Height.IsStar)
        {
          row.Height.Length = height * (row.Height.Value / totalStar);
        }
      }
    }

    #region IAddChild Members

    public void AddChild(RowDefinition o)
    {
      Add(o);
    }

    #endregion

  }
}
