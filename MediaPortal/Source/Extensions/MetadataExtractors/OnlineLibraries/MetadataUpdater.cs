#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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

using MediaPortal.Common.MediaManagement.Helpers;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace MediaPortal.Extensions.OnlineLibraries
{
  /// <summary>
  /// <see cref="MetadataUpdater"/> contains methods to merge metadata to get the most complete data.
  /// </summary>
  public class MetadataUpdater
  {
    public static void SetOrUpdateList<T>(List<T> currentList, List<T> newList, bool addMissing, bool isDefaultLanguage)
    {
      if (newList == null)
      {
        return;
      }
      if (currentList == null)
      {
        currentList = new List<T>();
      }
      for(int iNew = 0; iNew < newList.Count; iNew++)
      {
        int iCurrent = currentList.IndexOf(newList[iNew]);
        if (iCurrent >= 0)
        {
          object currentObj = (object)currentList[iCurrent];
          object newObj = (object)newList[iNew];

          FieldInfo[] fields = currentObj.GetType().GetFields();
          if (fields != null)
          {
            foreach (FieldInfo field in fields)
            {
              if (field.Name.EndsWith("Id", StringComparison.InvariantCultureIgnoreCase))
              {
                object currentVal = field.GetValue(currentObj);
                object newVal = field.GetValue(newObj);
                SetOrUpdateId(ref currentVal, newVal);
                field.SetValue(currentObj, currentVal);
              }
              else if (field.GetValue(currentObj) is string || field.GetValue(newObj) is string)
              {
                string currentVal = (string)field.GetValue(currentObj);
                string newVal = (string)field.GetValue(newObj);
                SetOrUpdateString(ref currentVal, newVal, isDefaultLanguage);
                field.SetValue(currentObj, currentVal);
              }
              else
              {
                object currentVal = field.GetValue(currentObj);
                object newVal = field.GetValue(newObj);
                SetOrUpdateValue(ref currentVal, newVal);
                field.SetValue(currentObj, currentVal);
              }
            }
          }
        }
        else if(addMissing)
        { 
          currentList.Add(newList[iNew]);
        }
      }
      currentList.Sort();
    }

    public static void SetOrUpdateString(ref string currentString, string newString, bool isDefaultLanguage)
    {
      if (string.IsNullOrEmpty(newString))
        return;
      if (string.IsNullOrEmpty(currentString))
      {
        currentString = newString.Trim();
        return;
      }
      //Avoid overwriting strings in the correct language with that of the default language
      if(isDefaultLanguage == false)
      {
        currentString = newString.Trim();
      }
    }

    public static void SetOrUpdateId<T>(ref T currentId, T newId)
    {
      if (currentId is string || newId is string)
      {
        if(string.IsNullOrEmpty(currentId as string))
          currentId = newId;
        return;
      }
      if (currentId is long || currentId is int || currentId is short)
      {
        if (Convert.ToInt64(newId) > 0)
          currentId = newId;
        return;
      }
    }

    public static void SetOrUpdateRatings(ref double currentRating, ref int currentRatingCount, double? newRating, int? newRatingCount)
    {
      if(currentRating <= 0)
      {
        currentRating = newRating.HasValue ? newRating.Value : 0;
        currentRatingCount = newRatingCount.HasValue ? newRatingCount.Value : 0;
      }
      else if (newRating.HasValue && newRatingCount.HasValue && currentRatingCount < newRatingCount.Value)
      {
        currentRating = newRating.Value;
        currentRatingCount = newRatingCount.Value;
      }
    }

    public static void SetOrUpdateValue<T>(ref T currentNumber, T newNumber)
    {
      if(Nullable.GetUnderlyingType(typeof(T)) != null)
      {
        if (currentNumber == null)
          currentNumber = newNumber;
        else if(currentNumber.GetType() == typeof(DateTime?))
        {
          //Some dates are missing the day and month component which are then both set to 1
          if (newNumber != null)
          {
            if (((DateTime?)(object)currentNumber).Value.Year == ((DateTime?)(object)newNumber).Value.Year &&
              ((DateTime?)(object)currentNumber).Value < ((DateTime?)(object)newNumber).Value)
              currentNumber = newNumber;
          }
        }
        else if (currentNumber.GetType() == typeof(DateTime))
        {
          //Some dates are missing the day and month component which are then both set to 1
          if (newNumber != null)
          {
            if (((DateTime)(object)currentNumber).Year == ((DateTime)(object)newNumber).Year &&
              ((DateTime)(object)currentNumber) < ((DateTime)(object)newNumber))
              currentNumber = newNumber;
          }
        }
      }
      else if (currentNumber is long || currentNumber is int || currentNumber is short || 
        currentNumber is long? || currentNumber is int? || currentNumber is short?)
      {
        if (Convert.ToInt64(currentNumber) == 0 && newNumber != null)
          currentNumber = newNumber;
      }
      else if (currentNumber is ulong || currentNumber is uint || currentNumber is ushort ||
        currentNumber is ulong? || currentNumber is uint? || currentNumber is ushort?)
      {
        if (Convert.ToUInt64(currentNumber) == 0 && newNumber != null)
          currentNumber = newNumber;
      }
      else if (currentNumber is float || currentNumber is double || currentNumber is float? || 
        currentNumber is double?)
      {
        if (Convert.ToDouble(currentNumber) == 0 && newNumber != null)
          currentNumber = newNumber;
      }
      else if (currentNumber is bool || currentNumber is bool?)
      {
        if (newNumber != null)
        currentNumber = newNumber;
      }
    }
  }
}
