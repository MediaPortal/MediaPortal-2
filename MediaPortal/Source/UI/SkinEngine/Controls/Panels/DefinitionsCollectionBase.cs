#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Panels
{
  public class DefinitionsCollectionBase : List<DefinitionBase>, IDeepCopyable
  {
    public virtual void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      DefinitionsCollectionBase dcb = (DefinitionsCollectionBase) source;
      foreach (DefinitionBase db in dcb)
        Add(copyManager.GetCopy(db));
    }

    public void ResetAllCellLengths()
    {
      foreach (DefinitionBase db in this)
      {
        double desiredLength = db.Length.IsAbsolute ? db.Length.Value : 0.0;
        db.Length.DesiredLength = desiredLength;
        db.Length.Length = desiredLength;
      }
    }

    public void SetDesiredLength(int cellIndex, int cellSpan, double desiredLength)
    {
      // Not set, don't bother.
      if (double.IsNaN(desiredLength))
        return;

      if (cellIndex < 0 || cellIndex >= Count)
      {
        ServiceRegistration.Get<ILogger>().Warn("{0}: Invalid cell index {1}; valid range is {2}-{3}", GetType().Name, cellIndex, 0, Count-1);
        if (cellIndex < 0)
          cellIndex = 0;
        else
          cellIndex = Count-1;
      }
      if (cellSpan < 0 || cellSpan + cellIndex > Count)
      {
        ServiceRegistration.Get<ILogger>().Warn("{0}: Invalid cell span {1} in cell {2}; valid range is {3}-{4}",
            GetType().Name, cellSpan, cellIndex, 1, Count-cellIndex);
        if (cellSpan < 0)
          cellSpan = 0;
        else
          cellSpan = Count-cellIndex;
      }
      int relativeCount = 0;
      for (int i = 0; i < cellSpan; i++)
      {
        GridLength length = this[i + cellIndex].Length;
        if (length.IsAbsolute)
          desiredLength -= length.DesiredLength;
        else
          relativeCount++;
      }
      bool starAvailable = false; // Determine if we have a Star column which will get the size primarily
      for (int i = 0; i < cellSpan; i++)
      {
        GridLength length = this[i + cellIndex].Length;
        starAvailable |= length.IsAutoStretch || length.IsStar;
      }
      for (int i = 0; i < cellSpan; i++)
      {
        GridLength length = this[i + cellIndex].Length;
        if (length.IsAutoStretch || length.IsStar || (length.IsAuto && !starAvailable))
          if (length.DesiredLength < desiredLength / relativeCount)
            length.DesiredLength = desiredLength / relativeCount;
      }
    }

    public double TotalDesiredLength
    {
      get
      {
        double relativeSum = this.Sum(cell => cell.Length.IsAbsolute ? 0 : cell.Length.Value);
        double result = 0;
        double min = 0;
        for (int i = 0; i < Count; i++)
        {
          GridLength length = this[i].Length;
          result += length.DesiredLength;
          if (!length.IsAbsolute)
            min = Math.Max(min, length.DesiredLength * relativeSum / length.Value);
        }
        return (double.IsInfinity(min) || double.IsNaN(min)) ? result : Math.Max(result, min);
      }
    }

    public void SetAvailableSize(double totalLength)
    {
      double fixedLength = 0;
      double totalStar = 0;
      foreach (DefinitionBase cell in this)
      {
        GridLength length = cell.Length;
        length.Length = length.DesiredLength;
        if (length.IsAbsolute || length.IsAuto) // Fixed size cells get size from cell length, auto sized cells should follow the child
          fixedLength += length.Length;
        else if (length.IsAutoStretch)
        {
          fixedLength += length.Length;
          totalStar += length.Value;
        }
        else
        {
          length.Length = 0;
          totalStar += length.Value;
        }

        // Too much allocated
        if (fixedLength > totalLength)
        {
          length.Length -= fixedLength - totalLength;
          fixedLength = totalLength;
        }
      }
      if (totalStar == 0)
        return;
      double remainingLength = totalLength - fixedLength;
      foreach (DefinitionBase cell in this)
      {
        GridLength length = cell.Length;
        if (length.IsStar)
          length.Length = remainingLength*(length.Value/totalStar);
        else if (length.IsAutoStretch)
          length.Length += remainingLength*(length.Value/totalStar);
      }
    }

    public double GetLength(int cellIndex, int cellSpan)
    {
      double cumulated = 0;
      cellIndex = Math.Min(cellIndex, Count - 1);
      cellSpan = Math.Min(cellSpan, Count - cellIndex);
      for (int i = 0; i < cellSpan; i++)
        cumulated += this[cellIndex + i].Length.Length;
      return cumulated;
    }

    public double GetOffset(int cellIndex)
    {
      double cumulated = 0;
      cellIndex = Math.Min(cellIndex, Count);
      for (int i = 0; i < cellIndex; i++)
        cumulated += this[i].Length.Length;
      return cumulated;
    }
  }
}
