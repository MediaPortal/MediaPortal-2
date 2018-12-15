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
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.SimApiV1.Data
{
  //{
  //  "id": "5013056",
  //  "title": "Dunkirk",
  //  "director": "Cora Aarnoutse,Jean-Luc Baillet,Nicolas Baldino,Lola Berteloot,Amaury Capel,Alexis Chelli,Cl\u00e9ment Comet,Frederic Dagmey,Nicolas de Lumb\u00e9e,Melissa Feuchtinger,Alina Gatti,Andrea Hachuel,Bruce Isler,Kelly-Joanne Jenkins,Paul Jolliot,Eric Richard Lasko,Candy Marlowe,Nilo Otero,Elsa Payen,Michael Pontvert,William Pruss,Willem Quarles van Ufford,Jesse Schouw,Toby Spanton,Tony van der Veer,John Papsidera,Toby Whale,Christopher Nolan,",
  //  "writer": "Christopher Nolan,",
  //  "cined": "Hoyte Van Hoytema,",
  //  "prod": "John Bernard,Erwin Godschalk,Jake Myers,Christopher Nolan,Maarten Swart,Emma Thomas,Andy Thompson,",
  //  "type": "movie",
  //  "year": "2017",
  //  "countries": "Netherlands,UK,France,USA,",
  //  "dur": "106,",
  //  "mpaa": "Rated PG-13 for intense war experience and some language",
  //  "rate": "9.0",
  //  "cov": "https://images-na.ssl-images-amazon.com/images/M/MV5BN2YyZjQ0NTEtNzU5MS00NGZkLTg0MTEtYzJmMWY3MWRhZjM2XkEyXkFqcGdeQXVyMDA4NzMyOA@@.jpg",
  //  "gen": "Action,Drama,History,War,",
  //  "plot": "Evacuation of Allied soldiers from Belgium, the British Empire, and France, who were cut off and surrounded by the German army from the beaches and harbor of Dunkirk, France, between May 26- June 04, 1940, during Battle of France in World War II.",
  //  "plotout": "Allied soldiers from Belgium, the British Empire and France are surrounded by the German army and evacuated during a fierce battle in World War II.",
  //  "cast": "Fionn Whitehead,Damien Bonnard,Aneurin Barnard,Lee Armstrong,James Bloor,Barry Keoghan,Mark Rylance,Tom Glynn-Carney,Tom Hardy,Jack Lowden,Luke Thompson,Michel Biel,Constantin Balsan,Billy Howle,Mikey Collins,Callum Blake,Dean Ridge,Bobby Lockwood,Will Attenborough,Kenneth Branagh,Tom Nolan,James D'Arcy,Matthew Marsh,Cillian Murphy,Adam Long,Harry Styles,Miranda Nolan,Bradley Hall,Jack Cutmore-Scott,Brett Lorenzini,Michael Fox,Brian Vernel,Elliott Tittensor,Kevin Guthrie,Harry Richardson,Jochum ten Haaf,Johnny Gibbon,Richard Sanderson,Kim Hartman,Calam Lynch,Charley Palmer Rothwell,Tom Gill,John Nolan,Bill Milner,Jack Riddiford,Harry Collett,Eric Richard,Sam Aronow,Simon Ates,Caleb Bailey,Michael Caine,Niels Dek van 't,Paul Riley Fox,Jack Gover,Sander Huisman,Christian Janner,Jedediah Jenk,Davey Jones,Han Leopold,Valiant Michael,Johnny Otto,Christian Roberts,Jan-Michael Rosner,Connor Ryan,Michiel van Ieperen,Olav Vollebregt,Nick Vorsselman,Merlijn Willemsen,Nirman Wolf,"
  //}
  [DataContract]
  public class SimApiMovie
  {
    private string _title;
    private string _plot;
    private string _plotOutline;

    [DataMember(Name = "title")]
    public string Title
    {
      get { return CleanString(_title); }
      set { _title = value; }
    }

    [DataMember(Name = "mpaa")]
    public string Rated { get; set; }

    [DataMember(Name = "year")]
    public string StrYear { get; set; }

    [DataMember(Name = "dur")]
    public string StrDuration { get; set; }

    [DataMember(Name = "gen")]
    public string StrGenre { get; set; }

    [DataMember(Name = "director")]
    public string StrDirectors { get; set; }

    [DataMember(Name = "cined")]
    public string StrCinematogrophers { get; set; }

    [DataMember(Name = "writer")]
    public string StrWriters { get; set; }

    [DataMember(Name = "prod")]
    public string StrProducers { get; set; }

    [DataMember(Name = "cast")]
    public string StrActors { get; set; }

    [DataMember(Name = "plot")]
    public string Plot
    {
      get { return CleanString(_plot); }
      set { _plot = value; }
    }

    [DataMember(Name = "plotout")]
    public string PlotOutline
    {
      get { return CleanString(_plotOutline); }
      set { _plotOutline = value; }
    }

    [DataMember(Name = "countries")]
    public string StrCountries { get; set; }

    [DataMember(Name = "cov")]
    public string PosterUrl { get; set; }

    [DataMember(Name = "rate")]
    public string StrImdbRating { get; set; }

    [DataMember(Name = "id")]
    public string ID { get; set; }

    [DataMember(Name = "type")]
    public string Type { get; set; }


    public double? ImdbRating
    {
      get
      {
        double rating;
        if (double.TryParse(StrImdbRating, NumberStyles.Float, CultureInfo.InvariantCulture, out rating) && rating > 0)
          return rating;
        return null;
      }
    }

    public int? Year
    {
      get
      {
        int year;
        if (int.TryParse(StrYear, out year) && year > 1000)
          return year;
        return null;
      }
    }

    public int? Duration
    {
      get
      {
        if (string.IsNullOrEmpty(StrDuration))
          return null;
        string[] strings = null;
        if (!string.IsNullOrEmpty(StrDuration)) strings = StrDuration.Split(',');
        if (strings != null)
        {
          foreach(string s in strings)
          {
            string dur = s;
            if(s.Contains(':'))
            {
              dur = s.Split(':')[1];
            }
            int duration;
            if (int.TryParse(s, out duration))
              return duration;
          }
        }
        return null;
      }
    }

    public List<string> Genres
    {
      get
      {
        if (string.IsNullOrEmpty(StrGenre))
          return null;
        string[] strings = null;
        if (!string.IsNullOrEmpty(StrGenre)) strings = StrGenre.Split(',');
        if (strings != null)
          return  new List<string>(strings).Where(s => !string.IsNullOrEmpty(s)).Select(s => CleanString(s)).Distinct().ToList();
        return null;
      }
    }

    public List<string> Directors
    {
      get
      {
        if (string.IsNullOrEmpty(StrDirectors))
          return null;
        string[] strings = null;
        if (!string.IsNullOrEmpty(StrDirectors)) strings = StrDirectors.Split(',');
        if (strings != null)
          return new List<string>(strings).Where(s => !string.IsNullOrEmpty(s)).Select(s => CleanString(s)).Distinct().ToList();
        return null;
      }
    }

    public List<string> Writers
    {
      get
      {
        if (string.IsNullOrEmpty(StrWriters))
          return null;
        string[] strings = null;
        if (!string.IsNullOrEmpty(StrWriters)) strings = StrWriters.Split(',');
        if (strings != null)
          return new List<string>(strings).Where(s => !string.IsNullOrEmpty(s)).Select(s => CleanString(s)).Distinct().ToList();
        return null;
      }
    }

    public List<string> Actors
    {
      get
      {
        if (string.IsNullOrEmpty(StrActors))
          return null;
        string[] strings = null;
        if (!string.IsNullOrEmpty(StrActors)) strings = StrActors.Split(',');
        if (strings != null)
          return new List<string>(strings).Where(s => !string.IsNullOrEmpty(s)).Select(s => CleanString(s)).Distinct().ToList();
        return null;
      }
    }

    public List<string> Producers
    {
      get
      {
        if (string.IsNullOrEmpty(StrProducers))
          return null;
        string[] strings = null;
        if (!string.IsNullOrEmpty(StrProducers)) strings = StrProducers.Split(',');
        if (strings != null)
          return new List<string>(strings).Where(s => !string.IsNullOrEmpty(s)).Select(s => CleanString(s)).Distinct().ToList();
        return null;
      }
    }

    public List<string> Cinematogrophers
    {
      get
      {
        if (string.IsNullOrEmpty(StrCinematogrophers))
          return null;
        string[] strings = null;
        if (!string.IsNullOrEmpty(StrCinematogrophers)) strings = StrCinematogrophers.Split(',');
        if (strings != null)
          return new List<string>(strings).Where(s => !string.IsNullOrEmpty(s)).Select(s => CleanString(s)).Distinct().ToList();
        return null;
      }
    }

    public List<string> Contries
    {
      get
      {
        if (string.IsNullOrEmpty(StrCountries))
          return null;
        string[] strings = null;
        if (!string.IsNullOrEmpty(StrCountries)) strings = StrCountries.Split(',');
        if (strings != null)
          return new List<string>(strings).Where(s => !string.IsNullOrEmpty(s)).Select(s => CleanString(s)).Distinct().ToList();
        return null;
      }
    }

    public string ImdbID
    {
      get
      {
        if (ID != null && !ID.StartsWith("tt", StringComparison.InvariantCultureIgnoreCase))
          return "tt" + ID;
        return ID;
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
