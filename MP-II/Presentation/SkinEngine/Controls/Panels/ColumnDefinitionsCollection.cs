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
using Presentation.SkinEngine.XamlParser;
using MediaPortal.Utilities.DeepCopy;
using Presentation.SkinEngine.SkinManagement;

namespace Presentation.SkinEngine.Controls.Panels
{
  public class ColumnDefinitionsCollection : List<ColumnDefinition>, IAddChild<ColumnDefinition>, IDeepCopyable
  {
    public virtual void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      ColumnDefinitionsCollection c = source as ColumnDefinitionsCollection;
      foreach (ColumnDefinition cd in c)
        Add(copyManager.GetCopy(cd));
    }

    #region IAddChild Members

    public void AddChild(ColumnDefinition o)
    {
      Add(o);
    }

    #endregion

    public float GetWidth(int column, int columnSpan)
    {
      float width = 0.0f;
      for (int i = 0; i < columnSpan; ++i)
      {
        width += (float)this[column + i].Width.Length;
      }
      return width;
    }

    public float GetOffset(int column)
    {
      float width = 0.0f;
      for (int i = 0; i < column; ++i)
      {
        width += (float)this[i].Width.Length;
      }
      return width;
    }

    public double TotalWidth
    {
      get
      {
        double width = 0.0;
        foreach (ColumnDefinition colDef in this)
        {
          width += colDef.Width.Length;
        }
        return width;
      }
    }

    public void SetWidth(int column, int columnSpan, float width)
    {
      int relativeCount = 0;
      for (int i = 0; i < columnSpan; ++i)
      {
        ColumnDefinition colDef = this[i + column];
        if (colDef.Width.IsAbsolute)
        {
          width -= (float)colDef.Width.Length;
        }
        else relativeCount++;
      }
      for (int i = 0; i < columnSpan; ++i)
      {
        ColumnDefinition colDef = this[i + column];
        if (colDef.Width.IsAuto || colDef.Width.IsStar)
        {
          if (colDef.Width.Length < width / relativeCount)
          {
            colDef.Width.Length = width / relativeCount;
          }
        }
      }
    }

    public void SetAvailableSize(double width)
    {
      double fixedWidth = 0.0f;
      int relativeCount = 0;
      double totalStar = 0;
      foreach (ColumnDefinition column in this)
      {
        if (column.Width.IsAbsolute)
        {
          column.Width.Length = (column.Width.Value * SkinContext.Zoom.Width);
          fixedWidth += column.Width.Length;
        }
        else
        {
          column.Width.Length = 0;
          if (column.Width.IsAuto)
            totalStar += 1.0;
          else
            totalStar += column.Width.Value;
          relativeCount++;
        }
      }
      if (width == 0.0) return;
      width -= fixedWidth;
      foreach (ColumnDefinition column in this)
      {
        if (column.Width.IsStar)
        {
          column.Width.Length = width * (column.Width.Value / totalStar);
        }
        else if (column.Width.IsAuto)
        {
          column.Width.Length = width * (1.0 / totalStar);
        }
      }
    }
  }
}
