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
    public static void SetOrUpdateList<T>(List<T> currentList, List<T> newList, bool addMissing)
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
          if (currentObj == null)
            continue;
          Type objType = currentObj.GetType();
          if (objType.IsClass)
          {
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
                else if (field.GetValue(currentObj) is string && field.GetValue(newObj) is string)
                {
                  string currentVal = (string)field.GetValue(currentObj);
                  string newVal = (string)field.GetValue(newObj);
                  SetOrUpdateString(ref currentVal, newVal);
                  field.SetValue(currentObj, currentVal);
                }
                else if (field.GetValue(currentObj) is SimpleTitle && field.GetValue(newObj) is string)
                {
                  SimpleTitle currentVal = (SimpleTitle)field.GetValue(currentObj);
                  string newVal = (string)field.GetValue(newObj);
                  SetOrUpdateString(ref currentVal, newVal);
                  field.SetValue(currentObj, currentVal);
                }
                else if (field.GetValue(currentObj) is string && field.GetValue(newObj) is SimpleTitle)
                {
                  string currentVal = (string)field.GetValue(currentObj);
                  SimpleTitle newVal = (SimpleTitle)field.GetValue(newObj);
                  SetOrUpdateString(ref currentVal, newVal);
                  field.SetValue(currentObj, currentVal);
                }
                else if (field.GetValue(currentObj) is SimpleTitle && field.GetValue(newObj) is SimpleTitle)
                {
                  SimpleTitle currentVal = (SimpleTitle)field.GetValue(currentObj);
                  SimpleTitle newVal = (SimpleTitle)field.GetValue(newObj);
                  SetOrUpdateString(ref currentVal, newVal);
                  field.SetValue(currentObj, currentVal);
                }
                else if (field.GetValue(currentObj) is SimpleRating && field.GetValue(newObj) is SimpleRating)
                {
                  SimpleRating currentVal = (SimpleRating)field.GetValue(currentObj);
                  SimpleRating newVal = (SimpleRating)field.GetValue(newObj);
                  SetOrUpdateRatings(ref currentVal, newVal);
                  field.SetValue(currentObj, currentVal);
                }
                else if (field.GetValue(currentObj) is SimpleRating && field.GetValue(newObj) is double)
                {
                  SimpleRating currentVal = (SimpleRating)field.GetValue(currentObj);
                  SimpleRating newVal = new SimpleRating((double)field.GetValue(newObj));
                  SetOrUpdateRatings(ref currentVal, newVal);
                  field.SetValue(currentObj, currentVal);
                }
                else if (field.GetValue(currentObj) is byte[])
                {
                  byte[] currentVal = (byte[])field.GetValue(currentObj);
                  byte[] newVal = (byte[])field.GetValue(newObj);
                  SetOrUpdateValue(ref currentVal, newVal);
                  field.SetValue(currentObj, currentVal);
                }
                else if (field.GetValue(currentObj) is IList)
                {
                  IList currentVal = (IList)field.GetValue(currentObj);
                  IList newVal = (IList)field.GetValue(newObj);
                  Type listElementType = currentVal.GetType().GetGenericArguments().Single();
                  if (listElementType == typeof(long))
                  {
                    SetOrUpdateList((List<long>)currentVal, (List<long>)newVal, true);
                  }
                  else if (listElementType == typeof(int))
                  {
                    SetOrUpdateList((List<int>)currentVal, (List<int>)newVal, true);
                  }
                  else if (listElementType == typeof(short))
                  {
                    SetOrUpdateList((List<short>)currentVal, (List<short>)newVal, true);
                  }
                  else if (listElementType == typeof(ulong))
                  {
                    SetOrUpdateList((List<ulong>)currentVal, (List<ulong>)newVal, true);
                  }
                  else if (listElementType == typeof(uint))
                  {
                    SetOrUpdateList((List<uint>)currentVal, (List<uint>)newVal, true);
                  }
                  else if (listElementType == typeof(ushort))
                  {
                    SetOrUpdateList((List<ushort>)currentVal, (List<ushort>)newVal, true);
                  }
                  else if (listElementType == typeof(double))
                  {
                    SetOrUpdateList((List<double>)currentVal, (List<double>)newVal, true);
                  }
                  else if (listElementType == typeof(float))
                  {
                    SetOrUpdateList((List<float>)currentVal, (List<float>)newVal, true);
                  }
                  else if (listElementType == typeof(string))
                  {
                    SetOrUpdateList((List<string>)currentVal, (List<string>)newVal, true);
                  }
                  else if (listElementType == typeof(PersonInfo))
                  {
                    SetOrUpdateList((List<PersonInfo>)currentVal, (List<PersonInfo>)newVal, true);
                  }
                  else if (listElementType == typeof(CompanyInfo))
                  {
                    SetOrUpdateList((List<CompanyInfo>)currentVal, (List<CompanyInfo>)newVal, true);
                  }
                  else if (listElementType == typeof(CharacterInfo))
                  {
                    SetOrUpdateList((List<CharacterInfo>)currentVal, (List<CharacterInfo>)newVal, true);
                  }
                  else if (listElementType == typeof(SeasonInfo))
                  {
                    SetOrUpdateList((List<SeasonInfo>)currentVal, (List<SeasonInfo>)newVal, true);
                  }
                  else if (listElementType == typeof(EpisodeInfo))
                  {
                    SetOrUpdateList((List<EpisodeInfo>)currentVal, (List<EpisodeInfo>)newVal, true);
                  }
                  else if (listElementType == typeof(MovieInfo))
                  {
                    SetOrUpdateList((List<MovieInfo>)currentVal, (List<MovieInfo>)newVal, true);
                  }
                  else if (listElementType == typeof(TrackInfo))
                  {
                    SetOrUpdateList((List<TrackInfo>)currentVal, (List<TrackInfo>)newVal, true);
                  }
                  else
                  {
                    throw new ArgumentException("SetOrUpdateList: IList is of an unsupported type");
                  }
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
        }
        else if(addMissing)
        { 
          currentList.Add(newList[iNew]);
        }
      }
      currentList.Sort();
    }

    public static void SetOrUpdateString(ref SimpleTitle currentString, string newString, bool isDefaultLanguage)
    {
      if (string.IsNullOrEmpty(newString))
        return;
      if (currentString.Text == null || currentString.IsEmpty)
      {
        currentString = new SimpleTitle(newString.Trim(), isDefaultLanguage);
        return;
      }
      //Avoid overwriting strings in the correct language with that of the default language
      if(currentString.DefaultLanguage && !isDefaultLanguage)
      {
        currentString = new SimpleTitle(newString.Trim(), isDefaultLanguage);
      }
      else if (currentString.DefaultLanguage == true)
      {
        if(currentString.Text.Length <= newString.Trim().Length)
        {
          currentString = new SimpleTitle(newString.Trim(), isDefaultLanguage);
        }
      }
    }

    public static void SetOrUpdateString(ref string currentString, string newString)
    {
      if (string.IsNullOrEmpty(newString))
        return;
      if (string.IsNullOrEmpty(currentString))
      {
        currentString = newString.Trim();
        return;
      }
      if (currentString.Length < newString.Trim().Length)
      {
        currentString = newString.Trim();
      }
    }

    public static void SetOrUpdateString(ref string currentString, SimpleTitle newString)
    {
      if (newString.IsEmpty)
        return;
      if (string.IsNullOrEmpty(currentString))
      {
        currentString = newString.Text.Trim();
        return;
      }
      if (currentString.Length < newString.Text.Trim().Length)
      {
        currentString = newString.Text.Trim();
      }
    }

    public static void SetOrUpdateString(ref SimpleTitle currentString, SimpleTitle newString)
    {
      if (newString.IsEmpty)
        return;
      SetOrUpdateString(ref currentString, newString.Text, newString.DefaultLanguage);
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

    public static void SetOrUpdateRatings(ref SimpleRating currentRating, SimpleRating newRating)
    {
      if(currentRating.IsEmpty)
      {
        currentRating = newRating;
      }
      else if (!currentRating.VoteCount.HasValue && newRating.VoteCount.HasValue)
      {
        currentRating = newRating;
      }
      else if (currentRating.VoteCount.HasValue && newRating.VoteCount.HasValue && 
        currentRating.VoteCount.Value < newRating.VoteCount.Value)
      {
        currentRating = newRating;
      }
    }

    public static void SetOrUpdateValue<T>(ref T currentNumber, T newNumber)
    {
      if (currentNumber == null)
      {
        currentNumber = newNumber;
      }
      else if (currentNumber is DateTime || newNumber is DateTime)
      {
        //Some dates are missing the day and month component which are then both set to 1
        if (newNumber != null)
        {
          if (((DateTime)(object)currentNumber).Year == ((DateTime)(object)newNumber).Year &&
            ((DateTime)(object)currentNumber) < ((DateTime)(object)newNumber))
            currentNumber = newNumber;
        }
      }
      else if (currentNumber is long || currentNumber is int || currentNumber is short ||
        newNumber is long || newNumber is int || newNumber is short)
      {
        if ((currentNumber == null || Convert.ToInt64(currentNumber) == 0) && newNumber != null)
          currentNumber = newNumber;
      }
      else if (currentNumber is ulong || currentNumber is uint || currentNumber is ushort ||
        newNumber is ulong || newNumber is uint || newNumber is ushort)
      {
        if ((currentNumber == null || Convert.ToUInt64(currentNumber) == 0) && newNumber != null)
          currentNumber = newNumber;
      }
      else if (currentNumber is float || currentNumber is double ||
        newNumber is float || newNumber is double)
      {
        if ((currentNumber == null || Convert.ToDouble(currentNumber) == 0) && newNumber != null)
          currentNumber = newNumber;
      }
      else if (currentNumber is bool || newNumber is bool)
      {
        if (newNumber != null)
          currentNumber = newNumber;
      }
      else if (currentNumber is byte[] || newNumber is byte[])
      {
        if (currentNumber == null && newNumber != null)
          currentNumber = newNumber;
      }
      else if (currentNumber == null || newNumber == null)
      {
        //Ignore
      }
      else
      {
        throw new ArgumentException("SetOrUpdateValue: currentNumber or newNumber is of an unsupported type");
      }
    }
  }
}
