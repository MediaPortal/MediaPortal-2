#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.SimApiV1.Data
{
  //{
  //    "id": "0000288",
  //    "name": "Christian Bale",
  //    "photo": "https://images-na.ssl-images-amazon.com/images/M/MV5BMTkxMzk4MjQ4MF5BMl5BanBnXkFtZTcwMzExODQxOA@@._V1_UX214_CR0,0,214,317_AL_.jpg",
  //    "url": "http://akas.imdb.com/name/nm0000288/",
  //    "birthdate": "1974",
  //    "birthplace": "Haverfordwest, Pembrokeshire, Wales, UK",
  //    "movies_act": "Jungle Book (2018),Hostiles (2017),Untitled Dick Cheney Project () (????),The Promise (II) (2016),The Big Short (2015),Knight of Cups (2015),Exodus: Gods and Kings (2014),American Hustle (2013),Out of the Furnace (2013),The Dark Knight Rises (2012),The Flowers of War (2011),The Fighter (I) (2010),Public Enemies (2009),Terminator Salvation (2009),The Dark Knight (2008),I'm Not There. (2007),3:10 to Yuma (2007),The Prestige (2006),Rescue Dawn (2006),The New World (2005),Harsh Times (2005),Batman Begins (2005),Batman Begins (2005),Howl's Moving Castle (2004),The Machinist (2004),Equilibrium (2002),Reign of Fire (2002),Laurel Canyon (2002),Captain Corelli's Mandolin (2001),Shaft (2000),American Psycho (2000),Mary, Mother of Jesus (1999),A Midsummer Night's Dream (1999),All the Little Animals (1998),Velvet Goldmine (1998),Metroland (1997),The Secret Agent (1996),The Portrait of a Lady (1996),Pocahontas (I) (1995),Little Women (1994),Royal Deceit (1994),Swing Kids (1993),Newsies (1992),A Murder of Quality (1991),The Dreamstone (1990),Treasure Island (1990),Henry V (1989),Empire of the Sun (1987),Mio in the Land of Faraway (1987),Heart of the Country (1987),Anastasia: The Mystery of Anna (1986),",
  //    "dir": "",
  //    "wrt": "",
  //    "cin": "",
  //    "prod": "Harsh Times (2005),"
  //}
  [DataContract]
  public class SimApiPerson
  {
    private string _name;
    private string _birthPlace;

    [DataMember(Name = "name")]
    public string Name
    {
      get { return CleanString(_name); }
      set { _name = value; }
    }

    [DataMember(Name = "photo")]
    public string ImageUrl { get; set; }

    [DataMember(Name = "url")]
    public string ImdbUrl { get; set; }

    [DataMember(Name = "birthdate")]
    public string StrBirth { get; set; }

    [DataMember(Name = "birthplace")]
    public string BirthPlace
    {
      get { return CleanString(_birthPlace); }
      set { _birthPlace = value; }
    }

    [DataMember(Name = "id")]
    public string ID { get; set; }

    public string ImdbID
    {
      get
      {
        if (ID != null && !ID.StartsWith("nm", StringComparison.InvariantCultureIgnoreCase))
          return "nm" + ID;
        return ID;
      }
    }

    public int? BirthYear
    {
      get
      {
        int year;
        if (int.TryParse(StrBirth, out year) && year > 1000)
          return year;
        return null;
      }
    }

    private string CleanString(string orignString)
    {
      if (string.IsNullOrEmpty(orignString))
        return null;
      return Uri.UnescapeDataString(orignString).Trim();
    }
  }
}
