#region Copyright (C) 2007 Team MediaPortal

/*
    Copyright (C) 2007 Team MediaPortal
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

using System;
using System.Windows.Forms;

namespace dxEngine
{
  internal static class Program
  {
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    private static void Main()
    {
      /*
      if (System.IO.File.Exists(@"Databases\movies3.db3"))
      {
        System.IO.File.Delete(@"Databases\movies3.db3");
      }
      DatabaseNotifier databaseNotifier = new DatabaseNotifier();
      ServiceScope.Add<IDatabaseNotifier>(databaseNotifier);
      MediaManager m = new MediaManager();
      m.Register(new DatabaseProvider());
      ServiceScope.Add<IMediaManager>(m);
      DatabaseBuilderFactory f = new DatabaseBuilderFactory();
      ServiceScope.Add<IDatabaseBuilderFactory>(f);
      IDatabaseBuilderFactory builderFactory = ServiceScope.Get<IDatabaseBuilderFactory>();
      IDatabaseFactory factory = builderFactory.Create(@"sqlite:Data Source=Databases\movies3.db3");

      IDatabase movieDatabase = factory.Open("Movies");

      movieDatabase.Add("title", typeof(string), 1024);
      movieDatabase.Add("genre", typeof(List<string>), 40);
      movieDatabase.Add("director", typeof(string), 40);
      movieDatabase.Add("contentURI", typeof(string), 1024);
      movieDatabase.Add("CoverArt", typeof(string), 1024);
      movieDatabase.Add("date", typeof(int));
      movieDatabase.Add("rating", typeof(int));
      movieDatabase.Add("duration", typeof(int));
      movieDatabase.Add("tagline", typeof(string), 60);
      movieDatabase.Add("plot", typeof(string), 1024);
      movieDatabase.Add("actors", typeof(List<string>), 1024);
      //add some dummy movies..
      List<IDbItem> items = new List<IDbItem>();
      IDbItem movieItem = movieDatabase.CreateNew();
      movieItem["title"] = "The Matrix";
      movieItem["genre"] = "Action|Thriller|Sci-Fi";
      movieItem["director"] = "Andy Wachowski";
      movieItem["date"] = 1999;
      movieItem["rating"] = 71;
      movieItem["duration"] = 136;
      movieItem["tagline"] = "In a world of 1s and 0s...are you a zero, or The One?";
      movieItem["plot"] = "A computer hacker learns from mysterious rebels about the true nature of his reality and his role in the war against the controllers of it.";
      movieItem["CoverArt"] = "http://www.vwlepaulski.com/Pics3/matrix31.jpg";
      movieItem.Attributes["actors"].Values.Add("actor1");
      movieItem.Attributes["actors"].Values.Add("actor2");
      movieItem.Attributes["actors"].Values.Add("actor3");
      movieItem.Attributes["actors"].Values.Add("actor4");
      movieItem.Attributes["actors"].Values.Add("actor5");
      movieItem.Attributes["actors"].Values.Add("actor6");
      movieItem.Attributes["actors"].Values.Add("actor7");
      movieItem.Attributes["actors"].Values.Add("actor8");
      movieItem.Attributes["actors"].Values.Add("actor9");
      movieItem.Attributes["actors"].Values.Add("actor10");
      //movieItem["actors"] = "actor1|actor2|actor3|actor4|actor5|actor6|actor7|actor8|actor9|actor10|";
      items.Add(movieItem);

      movieItem = movieDatabase.CreateNew();
      movieItem["title"] = "Jurassic Park";
      movieItem["genre"] = "Action|Adventure|Horror|Sci-Fi|Thriller";
      movieItem["director"] = "Steven Spielberg";
      movieItem["date"] = 1993;
      movieItem["rating"] = 61;
      movieItem["duration"] = 127;
      movieItem["tagline"] = "An Adventure 65 Million Years In The Making";
      movieItem["plot"] = "Scientists clone dinosaurs to populate a theme park which suffers a major security breakdown and releases the dinosaurs";
      movieItem["CoverArt"] = "http://horroryearbook.com/wp-content/uploads/2006/10/jurassic_park4.jpg";
      movieItem["actors"] = "actor2|actor4|actor6|actor8|actor10|";
      items.Add(movieItem);

      movieItem = movieDatabase.CreateNew();
      movieItem["title"] = "The Bourne Identity";
      movieItem["genre"] = "Action|Adventure|Mystery|Thriller";
      movieItem["director"] = "Doug Liman";
      movieItem["date"] = 2002;
      movieItem["rating"] = 81;
      movieItem["duration"] = 119;
      movieItem["tagline"] = "He was the perfect weapon until he became the target.";
      movieItem["plot"] = "A man is picked up by a fishing boat, bullet-riddled and without memory, then races to elude assassins and recover from amnesia";
      movieItem["CoverArt"] = "http://www.impawards.com/2002/posters/bourne_identity.jpg";
      movieItem["actors"] = "actor1|actor3|actor5|actor7|actor9|";
      items.Add(movieItem);


      movieItem = movieDatabase.CreateNew();
      movieItem["title"] = "Brother Bear";
      movieItem["genre"] = "Animation|Adventure|Comedy|Drama|Family|Fantasy ";
      movieItem["director"] = "Aaron Blaise";
      movieItem["date"] = 2003;
      movieItem["rating"] = 65;
      movieItem["duration"] = 82;
      movieItem["tagline"] = "This November, see through another's eyes, feel through another's heart, and discover the meaning of brotherhood.";
      movieItem["plot"] = "A young Inuit hunter seeks vengence against a bear, only to be magically changed into a bear himself, and his only chance to change back is with a talkative bear cub.";
      movieItem["CoverArt"] = "http://disney.goochemnet.nl/film/brotherbear/Brother_Bear.jpg";
      movieItem["actors"] = "actor1|actor2|actor6|actor7|actor9|actor10|actor23";
      items.Add(movieItem);

      movieDatabase.Save(items);
      movieItem["actors"] = "actor1";
      movieItem.Save();
      */
      Application.EnableVisualStyles();
      Application.SetCompatibleTextRenderingDefault(false);
      MainForm mainform = new MainForm();

      Application.Run(mainform);
    }
  }
}