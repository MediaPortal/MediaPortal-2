#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System.Collections.Generic;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
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
        db.Length.Length = db.Length.IsAbsolute ? db.Length.Value : 0.0;
      }
    }

    public void SetDesiredLength(int cellIndex, int cellSpan, double desiredLength)
    {
      // Not set, don't bother.
      if (double.IsNaN(desiredLength))
        return;

      int relativeCount = 0;
      if (cellIndex < 0 || cellIndex >= Count)
      {
        ServiceRegistration.Get<ILogger>().Warn("{0}: Invalid cell index {1}; allowed range is {2}-{3}", GetType().Name, cellIndex, 0, Count-1);
        if (cellIndex < 0)
          cellIndex = 0;
        else
          cellIndex = Count-1;
      }
      if (cellSpan < 0 || cellSpan + cellIndex > Count)
      {
        ServiceRegistration.Get<ILogger>().Warn("{0}: Invalid cell span {1} in cell {2}; allowed range is {3}-{4}",
            GetType().Name, cellSpan, cellIndex, 1, Count-cellIndex);
        if (cellSpan < 0)
          cellSpan = 0;
        else
          cellSpan = Count-cellIndex;
      }
      for (int i = 0; i < cellSpan; i++)
      {
        DefinitionBase cell = this[i + cellIndex];
        if (cell.Length.IsAbsolute)
          desiredLength -= cell.Length.Length;
        else
          relativeCount++;
      }
      for (int i = 0; i < cellSpan; i++)
      {
        DefinitionBase cell = this[i + cellIndex];
        if (cell.Length.IsAuto || cell.Length.IsStar)
          if (cell.Length.Length < desiredLength / relativeCount)
            cell.Length.Length = desiredLength / relativeCount;
      }
    }

    public double TotalDesiredLength
    {
      get
      {
        double cumulated = 0;
        foreach (DefinitionBase cell in this)
          cumulated += cell.Length.Length;
        return cumulated;
      }
    }

    public void SetAvailableSize(double totalLength)
    {
      double fixedLength = 0;
      double totalStar = 0;
      foreach (DefinitionBase cell in this)
      {
        if (cell.Length.IsAbsolute || cell.Length.IsAuto) // Fixed size cells get size from cell length, auto sized cells should follow the child
          fixedLength += cell.Length.Length;
        else
        {
          cell.Length.Length = 0;
          totalStar += cell.Length.Value;
        }

        // Too much allocated
        if (fixedLength > totalLength)
        {
          cell.Length.Length -= fixedLength - totalLength;
          fixedLength = totalLength;
        }
      }
      if (totalStar == 0)
        return;
      double remainingLength = totalLength - fixedLength;
      foreach (DefinitionBase cell in this)
        if (cell.Length.IsStar)
          cell.Length.Length = remainingLength * (cell.Length.Value / totalStar);
    }

    public double GetLength(int cellIndex, int cellSpan)
    {
      double cumulated = 0;
      for (int i = 0; i < cellSpan; i++)
        cumulated += this[cellIndex + i].Length.Length;
      return cumulated;
    }

    public double GetOffset(int cellIndex)
    {
      double cumulated = 0;
      for (int i = 0; i < cellIndex; i++)
        cumulated += this[i].Length.Length;
      return cumulated;
    }
  }
}
