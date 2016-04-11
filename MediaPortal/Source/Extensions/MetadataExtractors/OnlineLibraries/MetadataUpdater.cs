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

namespace MediaPortal.Extensions.OnlineLibraries
{
  /// <summary>
  /// <see cref="MetadataUpdater"/> contains methods to merge metadata to get the most complete data.
  /// </summary>
  public class MetadataUpdater
  {
    public static void SetOrUpdateList<T>(List<T> currentList, List<T> newList, bool addMissing)
    {
      if (currentList.Count == 0)
      {
        currentList.AddRange(newList);
        return;
      }
      for(int iNew = 0; iNew < newList.Count; iNew++)
      {
        int iCurrent = currentList.IndexOf(newList[iNew]);
        if (iCurrent >= 0)
        {
          if(typeof(T) == typeof(PersonInfo))
          {
            PersonInfo currentPerson = (PersonInfo)(object)currentList[iCurrent];
            PersonInfo newPerson = (PersonInfo)(object)newList[iNew];

            if (string.IsNullOrEmpty(currentPerson.ImdbId)) currentPerson.ImdbId = newPerson.ImdbId;
            if (currentPerson.MovieDbId == 0) currentPerson.MovieDbId = newPerson.MovieDbId;
            if (currentPerson.TvdbId == 0) currentPerson.TvdbId = newPerson.TvdbId;
          }
          else if (typeof(T) == typeof(CharacterInfo))
          {
            CharacterInfo currentCharacter = (CharacterInfo)(object)currentList[iCurrent];
            CharacterInfo newCharacter = (CharacterInfo)(object)newList[iNew];

            if (currentCharacter.MovieDbId == 0) currentCharacter.MovieDbId = newCharacter.MovieDbId;
            if (currentCharacter.TvdbId == 0) currentCharacter.TvdbId = newCharacter.TvdbId;
          }
          else if (typeof(T) == typeof(CompanyInfo))
          {
            CompanyInfo currentCompany = (CompanyInfo)(object)currentList[iCurrent];
            CompanyInfo newCompany = (CompanyInfo)(object)newList[iNew];

            if (string.IsNullOrEmpty(currentCompany.ImdbId)) currentCompany.ImdbId = newCompany.ImdbId;
            if (currentCompany.MovieDbId == 0) currentCompany.MovieDbId = newCompany.MovieDbId;
            if (currentCompany.TvdbId == 0) currentCompany.TvdbId = newCompany.TvdbId;
          }
        }
        else
        { 
          currentList.Add(newList[iNew]);
        }
      }
    }

    public static void SetOrUpdateString(ref string currentString, string newString, bool isDefaultLanguage)
    {
      if (string.IsNullOrEmpty(currentString))
      {
        currentString = newString;
        return;
      }
      //Avoid overwriting strings in the correct language with that of the default language
      if(isDefaultLanguage == false)
      {
        currentString = newString;
      }
    }

    public static void SetOrUpdateId<T>(ref T currentId, T newId)
    {
      if (currentId is string && string.IsNullOrEmpty(currentId as string))
      {
          currentId = newId;
          return;
      }
      if (currentId is long || currentId is int || currentId is short)
      {
        if (Convert.ToInt64(currentId) < Convert.ToInt64(newId))
          currentId = newId;
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
          if (((DateTime)(object)currentNumber).Year == ((DateTime)(object)newNumber).Year &&
              ((DateTime)(object)currentNumber) < ((DateTime)(object)newNumber))
            currentNumber = newNumber;
        }
      }
      else if (currentNumber is long || currentNumber is int || currentNumber is short || 
        currentNumber is long? || currentNumber is int? || currentNumber is short?)
      {
        if (Convert.ToInt64(currentNumber) == 0)
          currentNumber = newNumber;
      }
      else if (currentNumber is ulong || currentNumber is uint || currentNumber is ushort ||
        currentNumber is ulong? || currentNumber is uint? || currentNumber is ushort?)
      {
        if (Convert.ToUInt64(currentNumber) == 0)
          currentNumber = newNumber;
      }
      else if (currentNumber is float || currentNumber is double || currentNumber is float? || 
        currentNumber is double?)
      {
        if (Convert.ToDouble(currentNumber) == 0)
          currentNumber = newNumber;
      }
    }
  }
}
