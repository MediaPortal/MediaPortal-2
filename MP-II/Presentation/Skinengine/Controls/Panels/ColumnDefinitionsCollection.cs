using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using MyXaml.Core;
namespace Presentation.SkinEngine.Controls.Panels
{
  public class ColumnDefinitionsCollection : List<ColumnDefinition>, IAddChild
  {

    #region IAddChild Members

    public void AddChild(object o)
    {
      Add((ColumnDefinition)o);
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
          if (colDef.Width.Length == 0.0)
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
