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

using System.Collections.Generic;
using System.IO;
using System.Reflection;
using MediaPortal.Common;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.PathManager;
using MediaPortal.Common.Services.PathManager;
using MediaPortal.Common.Services.Settings;
using MediaPortal.Common.Settings;
using MediaPortal.Extensions.MetadataExtractors.SeriesMetadataExtractor.NameMatchers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test.OnlineLibraries
{
  [TestClass]
  public class SeriesNameMatcher
  {
    private void Init()
    {
      PathManager pathManager = new PathManager();
      // Use only local folder for temporary config
      string testDataLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
      pathManager.SetPath("<CONFIG>", testDataLocation);
      ServiceRegistration.Set<IPathManager>(pathManager);
      ServiceRegistration.Set<ILocalization>(new NoLocalization());
      ServiceRegistration.Set<ILogger>(new NoLogger());
      ServiceRegistration.Set<ISettingsManager>(new SettingsManager());
    }

    [TestMethod]
    public void SeriesMatchesAreCorrect()
    {
      Init();

      List<string> shouldMatchPaths = new List<string>
                          {
                            @"\\SAGESERVER\TV Series\Almost Human\Almost.Human.S01E01.720p.HDTV.X264-DIMENSION.mkv",
                            @"F:\TVSeries\The Bill Engvall Show\Season 1\The Bill Engvall Show - 101 - Good People.avi" ,
                            @"G:\Serien\The Big Bang Theory\Staffel 1 [DEU-ENG][720p]\S01E01 Penny und die Physiker.mkv" ,
                            @"G:\Serien\The Big Bang Theory\Staffel 1 [DEU-ENG][720p]\1x01 Penny und die Physiker.mkv" ,
                            @"D:\Capture\EUReKA - Die geheime Stadt\EUReKA - Die geheime Stadt - (Lyncht Fargo) S4 E3 - 2011-05-02 - 21_15.ts",
                            @"M:\Serien\Stargate Universe Season 1\Stargate Universe S01.E01_02 - Die Destiny.avi",
                            @"M:\Serien\Caprica\Caprica 1x01 - Pilot (1).mkv",
                            @"M:\\Serien\\Fringe Season 3\\Fringe S03E04 - Träumen Gestaltwandler von elektrischen Schafen.avi",
                            @"\The.Vampire.Diaries.S03-E13.720p.WEB-DL.mkv",
                            @"\Nikita.S02_E02.720p.WEB-DL.mkv",
                            @"\Switched.at.Birth.S01E10.The Homecoming.720p.WEB-DL.DD5.1.H.264-NT.mkv",
                            @"\The.Lying.Game.111.WEB.DL.mkv",
                            @"\Unforgettable.S01E02.720p.WEB-DL.mkv",
                            @"\Grimm.S01E06.720p.HDTV.X264-DIMENSION.mkv",
                            @"\The.Secret.Circle.S01E01.720p.WEB-DL.mkv",
                            @"\Lost.Girl.S02E06.720p.WEB-DL.mkv",
                            @"\Fringe.S04E08.720p.WEB-DL.mkv",
                            @"\the_fades.1x01.720p_hdtv_x264-fov.mkv",
                            @"\Once.Upon.A.Time.S01E13.720p.WEB-DL.mkv",
                            @"\The.Lying.Game.114.WEB.DL.mkv",
                            @"X:\USB_Backup\Filme\CodecTest\Dr. House - S01E01 Irgendwas.mkv",
                            @"Z:\Serien\Hawaii Five-O\Staffel 02\11 - In Der Falle.mkv",
                            @"Z:\Serien\Hawaii Five-O\Season 02\11 - In Der Falle.mkv",
                            @"Z:\Serien\Hawaii Five-O\02. Staffel\12 - In Der Falle.mkv",
                            @"Z:\Serien\Hawaii Five-O\Temporada 02\11 - In Der Falle.mkv",
                            @"Z:\Serien\Hawaii Five-O\02\11 - In Der Falle.mkv",
                            //@"Z:\Serien\Hawaii Five-O\2\11 In Der Falle.mkv",
                            @"Z:\Serien\The Mentalist\4\11 - Die Geister, Die Wir Riefen.mkv",
                            //@"Z:\Serien\The Mentalist\4\11 Die Geister, Die Wir Riefen.mkv",
                            @"\\BNAS\TV\All\Comedy\Farther Ted\Series 1\01 - Good Luke Father Ted.mkv",
                            @"\\BNAS\TV\All\Comedy\Black Adder\Black Adder 1\01 - The Foretelling.avi",
                          };

      SeriesMatcher matcher = new SeriesMatcher();
      foreach (string folderOrFileName in shouldMatchPaths)
      {
        SeriesInfo seriesInfo;
        bool match = matcher.MatchSeries(folderOrFileName, out seriesInfo);
        Assert.IsTrue(match, string.Format("Failed to match path '{0}'", folderOrFileName));
      }
    }

    [TestMethod]
    public void SeriesFalseMatches()
    {
      Init();

      List<string> shouldNotMatchPaths = new List<string>
                          {
                            @"\\Fool's Gold (2008).mkv",
                            @"Into the Blue (2005)",
                            @"\\Iron Man 2 Disc 1.mkv",
                            @"\\Salt - Blu-ray™.mkv",
                            @"\\Transformers Revenge of the Fallen Disc 1.mkv",
                            // FIXME: this one is currently wrongly matched
                            // @"Polizeiruf 110\Polizeiruf 110 - SE - Liebeswahn (Das Erste HD - 2014-01-12 20_15).ts",
                          };
      SeriesMatcher matcher = new SeriesMatcher();
      foreach (string folderOrFileName in shouldNotMatchPaths)
      {
        SeriesInfo seriesInfo;
        bool match = matcher.MatchSeries(folderOrFileName, out seriesInfo);
        Assert.IsFalse(match, string.Format("Wrong match for '{0}' should not be matched!", folderOrFileName));
      }
    }
  }
}
