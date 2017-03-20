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

using MediaPortal.Common.MediaManagement.Helpers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MediaPortal.Extensions.OnlineLibraries
{
  /// <summary>
  /// <see cref="MetadataUpdater"/> contains methods to merge metadata to get the most complete data.
  /// </summary>
  public class MetadataUpdater
  {
    public static bool SetOrUpdateList<T>(List<T> currentList, List<T> newList, bool addMissing, bool overwriteShorterStrings = true)
    {
      bool itemAdded;
      return SetOrUpdateList(currentList, newList, addMissing, out itemAdded, overwriteShorterStrings);
    }

    public static bool SetOrUpdateList<T>(List<T> currentList, List<T> newList, bool addMissing, out bool itemWasAdded, bool overwriteShorterStrings = true)
    {
      itemWasAdded = false;
      bool changed = false;
      if (newList == null)
      {
        return false;
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
          if (currentObj == null)
            continue;
          Type objType = currentObj.GetType();
          if (objType.IsClass)
          {
            FieldInfo[] fields = currentObj.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);
            if (fields != null)
            {
              foreach (FieldInfo field in fields)
              {
                if (field.Name.EndsWith("Id", StringComparison.InvariantCultureIgnoreCase))
                {
                  object currentVal = field.GetValue(currentObj);
                  object newVal = field.GetValue(newObj);
                  changed |= SetOrUpdateId(ref currentVal, newVal);
                  field.SetValue(currentObj, currentVal);
                }
                else if (field.GetValue(currentObj) is string && field.GetValue(newObj) is string)
                {
                  string currentVal = (string)field.GetValue(currentObj);
                  string newVal = (string)field.GetValue(newObj);
                  changed |= SetOrUpdateString(ref currentVal, newVal, overwriteShorterStrings);
                  field.SetValue(currentObj, currentVal);
                }
                else if (field.GetValue(currentObj) is SimpleTitle && field.GetValue(newObj) is string)
                {
                  SimpleTitle currentVal = (SimpleTitle)field.GetValue(currentObj);
                  string newVal = (string)field.GetValue(newObj);
                  changed |= SetOrUpdateString(ref currentVal, newVal, overwriteShorterStrings);
                  field.SetValue(currentObj, currentVal);
                }
                else if (field.GetValue(currentObj) is string && field.GetValue(newObj) is SimpleTitle)
                {
                  string currentVal = (string)field.GetValue(currentObj);
                  SimpleTitle newVal = (SimpleTitle)field.GetValue(newObj);
                  changed |= SetOrUpdateString(ref currentVal, newVal, overwriteShorterStrings);
                  field.SetValue(currentObj, currentVal);
                }
                else if (field.GetValue(currentObj) is SimpleTitle && field.GetValue(newObj) is SimpleTitle)
                {
                  SimpleTitle currentVal = (SimpleTitle)field.GetValue(currentObj);
                  SimpleTitle newVal = (SimpleTitle)field.GetValue(newObj);
                  changed |= SetOrUpdateString(ref currentVal, newVal, overwriteShorterStrings);
                  field.SetValue(currentObj, currentVal);
                }
                else if (field.GetValue(currentObj) is SimpleRating && field.GetValue(newObj) is SimpleRating)
                {
                  SimpleRating currentVal = (SimpleRating)field.GetValue(currentObj);
                  SimpleRating newVal = (SimpleRating)field.GetValue(newObj);
                  changed |= SetOrUpdateRatings(ref currentVal, newVal);
                  field.SetValue(currentObj, currentVal);
                }
                else if (field.GetValue(currentObj) is SimpleRating && field.GetValue(newObj) is double)
                {
                  SimpleRating currentVal = (SimpleRating)field.GetValue(currentObj);
                  SimpleRating newVal = new SimpleRating((double)field.GetValue(newObj));
                  changed |= SetOrUpdateRatings(ref currentVal, newVal);
                  field.SetValue(currentObj, currentVal);
                }
                else if (field.GetValue(currentObj) is byte[])
                {
                  byte[] currentVal = (byte[])field.GetValue(currentObj);
                  byte[] newVal = (byte[])field.GetValue(newObj);
                  changed |= SetOrUpdateValue(ref currentVal, newVal);
                  field.SetValue(currentObj, currentVal);
                }
                else if (field.GetValue(currentObj) is IList)
                {
                  IList currentVal = (IList)field.GetValue(currentObj);
                  IList newVal = (IList)field.GetValue(newObj);
                  Type listElementType = currentVal.GetType().GetGenericArguments().Single();
                  bool itemAdded = false;
                  if (listElementType == typeof(long))
                  {
                    changed |= SetOrUpdateList((List<long>)currentVal, (List<long>)newVal, true, out itemAdded);
                  }
                  else if (listElementType == typeof(int))
                  {
                    changed |= SetOrUpdateList((List<int>)currentVal, (List<int>)newVal, true, out itemAdded);
                  }
                  else if (listElementType == typeof(short))
                  {
                    changed |= SetOrUpdateList((List<short>)currentVal, (List<short>)newVal, true, out itemAdded);
                  }
                  else if (listElementType == typeof(ulong))
                  {
                    changed |= SetOrUpdateList((List<ulong>)currentVal, (List<ulong>)newVal, true, out itemAdded);
                  }
                  else if (listElementType == typeof(uint))
                  {
                    changed |= SetOrUpdateList((List<uint>)currentVal, (List<uint>)newVal, true, out itemAdded);
                  }
                  else if (listElementType == typeof(ushort))
                  {
                    changed |= SetOrUpdateList((List<ushort>)currentVal, (List<ushort>)newVal, true, out itemAdded);
                  }
                  else if (listElementType == typeof(double))
                  {
                    changed |= SetOrUpdateList((List<double>)currentVal, (List<double>)newVal, true, out itemAdded);
                  }
                  else if (listElementType == typeof(float))
                  {
                    changed |= SetOrUpdateList((List<float>)currentVal, (List<float>)newVal, true, out itemAdded);
                  }
                  else if (listElementType == typeof(string))
                  {
                    changed |= SetOrUpdateList((List<string>)currentVal, (List<string>)newVal, true, out itemAdded);
                  }
                  else if (listElementType == typeof(PersonInfo))
                  {
                    changed |= SetOrUpdateList((List<PersonInfo>)currentVal, (List<PersonInfo>)newVal, true, out itemAdded);
                  }
                  else if (listElementType == typeof(CompanyInfo))
                  {
                    changed |= SetOrUpdateList((List<CompanyInfo>)currentVal, (List<CompanyInfo>)newVal, true, out itemAdded);
                  }
                  else if (listElementType == typeof(CharacterInfo))
                  {
                    changed |= SetOrUpdateList((List<CharacterInfo>)currentVal, (List<CharacterInfo>)newVal, true, out itemAdded);
                  }
                  else if (listElementType == typeof(SeasonInfo))
                  {
                    changed |= SetOrUpdateList((List<SeasonInfo>)currentVal, (List<SeasonInfo>)newVal, true, out itemAdded);
                  }
                  else if (listElementType == typeof(EpisodeInfo))
                  {
                    changed |= SetOrUpdateList((List<EpisodeInfo>)currentVal, (List<EpisodeInfo>)newVal, true, out itemAdded);
                  }
                  else if (listElementType == typeof(MovieInfo))
                  {
                    changed |= SetOrUpdateList((List<MovieInfo>)currentVal, (List<MovieInfo>)newVal, true, out itemAdded);
                  }
                  else if (listElementType == typeof(TrackInfo))
                  {
                    changed |= SetOrUpdateList((List<TrackInfo>)currentVal, (List<TrackInfo>)newVal, true, out itemAdded);
                  }
                  else
                  {
                    throw new ArgumentException("SetOrUpdateList: IList is of an unsupported type");
                  }
                  field.SetValue(currentObj, currentVal);
                  if (itemAdded)
                    itemWasAdded = true;
                }
                else
                {
                  object currentVal = field.GetValue(currentObj);
                  object newVal = field.GetValue(newObj);
                  if (Nullable.GetUnderlyingType(field.FieldType) != null)
                  {
                    if (currentVal == null && newVal != null)
                    {
                      field.SetValue(currentObj, newVal);
                      changed = true;
                    }
                  }
                  else
                  {
                    changed |= SetOrUpdateValue(ref currentVal, newVal);
                    field.SetValue(currentObj, currentVal);
                  }
                }
              }
            }
          }
        }
        else if(addMissing)
        { 
          currentList.Add(newList[iNew]);
          itemWasAdded = true;
          changed = true;
        }
      }
      currentList.Sort();
      return changed;
    }

    public static bool SetOrUpdateString(ref SimpleTitle currentString, string newString, bool isDefaultLanguage, bool overwriteShorterStrings = true)
    {
      if (string.IsNullOrEmpty(newString))
        return false;

      newString = newString.Trim();
      //Avoid overwriting strings in the correct language with that of the default language
      if (currentString.IsEmpty || (currentString.DefaultLanguage && !isDefaultLanguage))
      {
        currentString = new SimpleTitle(newString, isDefaultLanguage);
        return true;
      }
      else if (overwriteShorterStrings && currentString.DefaultLanguage && currentString.Text.Length <= newString.Length)
      {
        currentString = new SimpleTitle(newString, isDefaultLanguage);
        return true;
      }
      return false;
    }

    public static bool SetOrUpdateString(ref string currentString, string newString, bool overwriteShorterStrings = true)
    {
      if (string.IsNullOrEmpty(newString))
        return false;

      newString = newString.Trim();
      if (string.IsNullOrEmpty(currentString) || (overwriteShorterStrings && currentString.Length < newString.Length))
      {
        currentString = newString;
        return true;
      }
      return false;
    }

    public static bool SetOrUpdateString(ref string currentString, SimpleTitle newString, bool overwriteShorterStrings = true)
    {
      return SetOrUpdateString(ref currentString, newString.Text, overwriteShorterStrings);
    }

    public static bool SetOrUpdateString(ref SimpleTitle currentString, SimpleTitle newString, bool overwriteShorterStrings = true)
    {
      return SetOrUpdateString(ref currentString, newString.Text, newString.DefaultLanguage, overwriteShorterStrings);
    }

    public static bool SetOrUpdateId<T>(ref T currentId, T newId)
    {
      if (currentId is string || newId is string)
      {
        if (string.IsNullOrEmpty(currentId as string))
        {
          currentId = newId;
          return true;
        }
      }
      if (currentId is long || currentId is int || currentId is short)
      {
        if (Convert.ToInt64(currentId) == 0 && Convert.ToInt64(newId) > 0)
        {
          currentId = newId;
          return true;
        }
      }
      return false;
    }

    public static bool SetOrUpdateRatings(ref SimpleRating currentRating, SimpleRating newRating)
    {
      if(newRating.IsEmpty)
      {
        return false;
      }
      else if(newRating.RatingValue < 0)
      {
        return false;
      }
      else if(currentRating.IsEmpty)
      {
        currentRating = newRating;
        return true;
      }
      else if (!currentRating.VoteCount.HasValue && newRating.VoteCount.HasValue)
      {
        currentRating = newRating;
        return true;
      }
      else if (currentRating.VoteCount.HasValue && newRating.VoteCount.HasValue && 
        currentRating.VoteCount.Value < newRating.VoteCount.Value)
      {
        currentRating = newRating;
        return true;
      }
      return false;
    }

    public static bool SetOrUpdateValue<T>(ref T currentNumber, T newNumber)
    {
      if (newNumber == null)
      {
        return false;
      }
      else if (currentNumber == null)
      {
        currentNumber = newNumber;
        return true;
      }
      else if (Nullable.GetUnderlyingType(currentNumber.GetType()) != null)
      {
        if (currentNumber == null && newNumber != null)
        {
          currentNumber = newNumber;
          return true;
        }
      }
      else if (currentNumber is DateTime || newNumber is DateTime)
      {
        //Some dates are missing the day and month component which are then both set to 1
        if (newNumber != null)
        {
          if (((DateTime)(object)currentNumber).Year == ((DateTime)(object)newNumber).Year &&
            ((DateTime)(object)currentNumber) < ((DateTime)(object)newNumber))
          {
            currentNumber = newNumber;
            return true;
          }
        }
      }
      else if (currentNumber is long || currentNumber is int || currentNumber is short ||
        newNumber is long || newNumber is int || newNumber is short)
      {
        if ((currentNumber == null || Convert.ToInt64(currentNumber) == 0) && 
          (newNumber != null && Convert.ToInt64(newNumber) > 0))
        {
          currentNumber = newNumber;
          return true;
        }
      }
      else if (currentNumber is ulong || currentNumber is uint || currentNumber is ushort ||
        newNumber is ulong || newNumber is uint || newNumber is ushort)
      {
        if ((currentNumber == null || Convert.ToUInt64(currentNumber) == 0) &&
          (newNumber != null && Convert.ToUInt64(newNumber) > 0))
        {
          currentNumber = newNumber;
          return true;
        }
      }
      else if (currentNumber is float || currentNumber is double ||
        newNumber is float || newNumber is double)
      {
        if ((currentNumber == null || Convert.ToDouble(currentNumber) == 0) &&
          (newNumber != null && Convert.ToDouble(newNumber) > 0))
        {
          currentNumber = newNumber;
          return true;
        }
      }
      else if (currentNumber is bool || newNumber is bool)
      {
        if (newNumber != null && (Convert.ToBoolean(currentNumber) != Convert.ToBoolean(newNumber)))
        {
          currentNumber = newNumber;
          return true;
        }
      }
      else if (currentNumber is byte[] || newNumber is byte[])
      {
        if (currentNumber == null && newNumber != null)
        {
          currentNumber = newNumber;
          return true;
        }
      }
      else if (currentNumber == null || newNumber == null)
      {
        //Ignore
      }
      else
      {
        throw new ArgumentException("SetOrUpdateValue: currentNumber or newNumber is of an unsupported type");
      }
      return false;
    }
  }
}
