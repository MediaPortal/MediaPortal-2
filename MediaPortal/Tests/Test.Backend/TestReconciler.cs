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
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Mock;
using NUnit.Framework;

namespace Test.Backend
{
  [TestFixture]
  public class TestReconciler
  {
    private string[] CreateAttributeIdList(int firstId, int maxId)
    {
      IList<string> ids = new List<string>();

      for (int id = firstId; id <= maxId; id++)
        ids.Add("A" + id);

      for (int id = 0; id < firstId; id++)
        ids.Add("A" + id);

      return ids.ToArray();
    }

    [SetUp]
    public void SetUp()
    {
      MockDBUtils.Reset();
      MockCore.Reset();
    }

    private bool EpisodeSeasonMatcher(IDictionary<Guid, IList<MediaItemAspect>> aspects, IDictionary<Guid, IList<MediaItemAspect>> linkedAspects)
    {
      int season;
      int linkedSeason;

      if (!MediaItemAspect.TryGetAttribute<int>(aspects, EpisodeAspect.ATTR_SEASON, out season))
      {
        return false;
      }

      if (!MediaItemAspect.TryGetAttribute(linkedAspects, SeasonAspect.ATTR_SEASON, out linkedSeason))
      {
        return false;
      }

      return season == linkedSeason;
    }

    [Test]
    public void TestReconcileMediaItem()
    {
      //TODO: Update below code to work with ML changes
      return;

      MockCore.SetupLibrary(true);

      ServiceRegistration.Set<IPluginManager>(new MockPluginManager());

      MockRelationshipExtractor extractor = new MockRelationshipExtractor();

      MockMediaAccessor accessor = new MockMediaAccessor();
      accessor.AddRelationshipExtractor(extractor);
      ServiceRegistration.Set<IMediaAccessor>(accessor);
      ServiceRegistration.Get<IMediaAccessor>().Initialize();

      MockCore.Management.AddMediaItemAspectStorage(EpisodeAspect.Metadata);
      MockCore.Management.AddMediaItemAspectStorage(ExternalIdentifierAspect.Metadata);
      MockCore.Management.AddMediaItemAspectStorage(ImporterAspect.Metadata);
      MockCore.Management.AddMediaItemAspectStorage(MediaAspect.Metadata);
      MockCore.Management.AddMediaItemAspectStorage(ProviderResourceAspect.Metadata);
      MockCore.Management.AddMediaItemAspectStorage(RelationshipAspect.Metadata);
      MockCore.Management.AddMediaItemAspectStorage(SeasonAspect.Metadata);
      MockCore.Management.AddMediaItemAspectStorage(SeriesAspect.Metadata);

      Guid episodeItemId = new Guid("aaaaaaaa-1111-1111-1111-aaaaaaaaaaaa");
      Guid seasonItemId = new Guid("bbbbbbbb-2222-2222-2222-bbbbbbbbbbbb");
      Guid seriesItemId = new Guid("cccccccc-3333-3333-3333-cccccccccccc");

      string seriesName = "The Series";
      string seriesDescription = "The adventures of some characters";

      int episode = 1;
      string episodeName = "The Episode";
      string episodeTitle = seriesName + ": " + episodeName;
      Guid parentDirectoryId = new Guid("dddddddd-4444-4444-4444-dddddddddddd");

      int season = 2;
      string seriesSeasonName = seriesName + " " + season;
      string seasonDescription = "Continuing adventures of some characters, several story arcs etc";

      string externalSource = "TEST";
      string externalSeriesId = "345";

      MockCore.Library.AddMediaItemId(episodeItemId);
      MockCore.Library.AddMediaItemId(seasonItemId);
      MockCore.Library.AddMediaItemId(seriesItemId);

      string systemId = "local";
      string mimeType = "video/mkv";
      string pathStr = @"c:\item.mkv";
      ResourcePath path = LocalFsResourceProviderBase.ToResourcePath(pathStr);
      DateTime importDate;
      DateTime.TryParse("2014-10-11 12:34:56", out importDate);

      IDictionary<Guid, IList<MediaItemAspect>> episodeAspects = new Dictionary<Guid, IList<MediaItemAspect>>();
      MediaItemAspect.SetAttribute(episodeAspects, MediaAspect.ATTR_TITLE, episodeTitle);
      MediaItemAspect.SetCollectionAttribute(episodeAspects, EpisodeAspect.ATTR_EPISODE, new[] { episode });
      MediaItemAspect.SetAttribute(episodeAspects, EpisodeAspect.ATTR_SEASON, season);
      MultipleMediaItemAspect providerResourceAspect = MediaItemAspect.CreateAspect(episodeAspects, ProviderResourceAspect.Metadata);
      providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_MIME_TYPE, mimeType);
      MediaItemAspect.AddOrUpdateExternalIdentifier(episodeAspects, externalSource, ExternalIdentifierAspect.TYPE_SERIES, externalSeriesId);
      ServiceRegistration.Get<ILogger>().Debug("Episode:");
      MockCore.ShowMediaAspects(episodeAspects, MockCore.Library.GetManagedMediaItemAspectMetadata());

      IDictionary<Guid, IList<MediaItemAspect>> seasonAspects = new Dictionary<Guid, IList<MediaItemAspect>>();
      MediaItemAspect.SetAttribute(seasonAspects, SeasonAspect.ATTR_SERIES_NAME, seriesName);
      MediaItemAspect.SetAttribute(seasonAspects, SeasonAspect.ATTR_SEASON, season);
      MediaItemAspect.SetAttribute(seasonAspects, SeasonAspect.ATTR_DESCRIPTION, seasonDescription);
      MediaItemAspect.AddOrUpdateExternalIdentifier(seasonAspects, externalSource, ExternalIdentifierAspect.TYPE_SERIES, externalSeriesId);
      ServiceRegistration.Get<ILogger>().Debug("Season:");
      MockCore.ShowMediaAspects(seasonAspects, MockCore.Library.GetManagedMediaItemAspectMetadata());

      Guid[] matchAspects = new[] { SeasonAspect.ASPECT_ID, ExternalIdentifierAspect.ASPECT_ID, MediaAspect.ASPECT_ID };
      extractor.AddRelationship(EpisodeAspect.ROLE_EPISODE, new[] { EpisodeAspect.ASPECT_ID }, SeasonAspect.ROLE_SEASON, new[] { SeasonAspect.ASPECT_ID }, matchAspects, externalSource, ExternalIdentifierAspect.TYPE_SERIES, externalSeriesId, new List<IDictionary<Guid, IList<MediaItemAspect>>>() { seasonAspects }, EpisodeSeasonMatcher, episode);

      IDictionary<Guid, IList<MediaItemAspect>> seriesAspects = new Dictionary<Guid, IList<MediaItemAspect>>();
      MediaItemAspect.SetAttribute(seriesAspects, SeasonAspect.ATTR_SERIES_NAME, seriesName);
      MediaItemAspect.SetAttribute(seriesAspects, SeasonAspect.ATTR_DESCRIPTION, seriesDescription);
      MediaItemAspect.AddOrUpdateExternalIdentifier(seriesAspects, externalSource, ExternalIdentifierAspect.TYPE_SERIES, externalSeriesId);
      ServiceRegistration.Get<ILogger>().Debug("Series:");
      MockCore.ShowMediaAspects(seriesAspects, MockCore.Library.GetManagedMediaItemAspectMetadata());

      matchAspects = new[] { SeriesAspect.ASPECT_ID, ExternalIdentifierAspect.ASPECT_ID, MediaAspect.ASPECT_ID };
      extractor.AddRelationship(SeasonAspect.ROLE_SEASON, new[] { SeasonAspect.ASPECT_ID }, SeriesAspect.ROLE_SERIES, new[] { SeriesAspect.ASPECT_ID }, matchAspects, externalSource, ExternalIdentifierAspect.TYPE_SERIES, externalSeriesId, new List<IDictionary<Guid, IList<MediaItemAspect>>>() { seriesAspects }, null, season);

      MockDBUtils.AddReader(1, "SELECT MEDIA_ITEM_ID FROM M_PROVIDERRESOURCE WHERE SYSTEM_ID = @SYSTEM_ID AND PATH = @PATH", "MEDIA_ITEM_ID");

      MockDBUtils.AddReader(2, "SELECT MEDIA_ITEM_ID FROM M_PROVIDERRESOURCE WHERE MEDIA_ITEM_ID = @MEDIA_ITEM_ID", "MEDIA_ITEM_ID");

      // Readers used by UpdateRelationships to find episode item

      MockReader reader2 = MockDBUtils.AddReader(3,
        "SELECT T6.MEDIA_ITEM_ID A30, T0.MEDIA_ITEM_ID A31, T1.MEDIA_ITEM_ID A32, T2.MEDIA_ITEM_ID A33, T3.MEDIA_ITEM_ID A34, T4.MEDIA_ITEM_ID A35, T5.MEDIA_ITEM_ID A36, " +
        "T0.SERIESNAME A0, T0.SEASON A1, T0.SERIESSEASONNAME A2, T0.EPISODENAME A3, T0.FIRSTAIRED A4, T0.TOTALRATING A5, T0.RATINGCOUNT A6, " +
        "T1.LASTIMPORTDATE A7, T1.DIRTY A8, T1.DATEADDED A9, " +
        "T2.TITLE A10, T2.RECORDINGTIME A11, T2.RATING A12, T2.COMMENT A13, T2.PLAYCOUNT A14, T2.LASTPLAYED A15, " +
        "T3.SYSTEM_ID A16, T3.MIMETYPE A17, T3.SIZE A18, T3.PATH A19, T3.PARENTDIRECTORY A20, " +
        "T4.SERIESNAME_0 A21, T4.SEASON_0 A22, T4.SERIESSEASONNAME_0 A23, T4.DESCRIPTION A24, T4.FIRSTAIRED_0 A25, T4.TOTALRATING_0 A26, T4.RATINGCOUNT_0 A27, " +
        "T5.SERIESNAME_1 A28, T5.DESCRIPTION_0 A29 " +
        "FROM MEDIA_ITEMS T6 " +
        "LEFT OUTER JOIN M_EPISODEITEM T0 ON T0.MEDIA_ITEM_ID = T6.MEDIA_ITEM_ID " +
        "LEFT OUTER JOIN M_IMPORTEDITEM T1 ON T1.MEDIA_ITEM_ID = T6.MEDIA_ITEM_ID " +
        "LEFT OUTER JOIN M_MEDIAITEM T2 ON T2.MEDIA_ITEM_ID = T6.MEDIA_ITEM_ID " +
        "LEFT OUTER JOIN M_PROVIDERRESOURCE T3 ON T3.MEDIA_ITEM_ID = T6.MEDIA_ITEM_ID " +
        "LEFT OUTER JOIN M_SEASONITEM T4 ON T4.MEDIA_ITEM_ID = T6.MEDIA_ITEM_ID " +
        "LEFT OUTER JOIN M_SERIESITEM T5 ON T5.MEDIA_ITEM_ID = T6.MEDIA_ITEM_ID  " +
        "WHERE T6.MEDIA_ITEM_ID = @V0",
        CreateAttributeIdList(30, 36));
      reader2.AddResult(
        episodeItemId, episodeItemId, episodeItemId, episodeItemId, episodeItemId, null, null,
        null, season, null, null, null, null, null,
        importDate, false, importDate,
        episodeTitle, null, null, null, null, null,
        systemId, mimeType, 100, @"c:\", parentDirectoryId,
        null, null, null, null, null, null, null,
        null, null
      );

      MockReader reader3 = MockDBUtils.AddReader(4,
        "SELECT T0.MEDIA_ITEM_ID A0, " +
        "T1.VALUE A1 " +
        "FROM NM_EPISODE T0 " +
        "INNER JOIN V_EPISODE T1 ON T0.VALUE_ID = T1.VALUE_ID " +
        "WHERE T0.MEDIA_ITEM_ID = @V0",
        "A0", "A1");
      reader3.AddResult(
        episodeItemId,
        episode);

      MockDBUtils.AddReader(5, "SELECT T0.MEDIA_ITEM_ID A0, T1.VALUE A1 FROM NM_DVDEPISODE T0 INNER JOIN V_DVDEPISODE T1 ON T0.VALUE_ID = T1.VALUE_ID WHERE T0.MEDIA_ITEM_ID = @V0", "A0", "A1");

      MockReader reader5 = MockDBUtils.AddReader(6,
        "SELECT T0.MEDIA_ITEM_ID A3, T0.MEDIA_ITEM_ID A4, " +
        "T0.SOURCE A0, T0.TYPE A1, T0.ID A2 " +
        "FROM M_EXTERNALIDENTIFIER T0  " +
        "WHERE T0.MEDIA_ITEM_ID = @V0",
        CreateAttributeIdList(3, 4));
      reader5.AddResult(
        episodeItemId, episodeItemId,
        externalSource, ExternalIdentifierAspect.TYPE_SERIES, externalSeriesId);

      MockDBUtils.AddReader(7,
        "SELECT T0.MEDIA_ITEM_ID A4, T0.MEDIA_ITEM_ID A5, " +
        "T0.ROLE A0, T0.LINKEDROLE A1, T0.LINKEDID A2, T0.RELATIONSHIPINDEX A3 " +
        "FROM M_RELATIONSHIP T0  " +
        "WHERE T0.MEDIA_ITEM_ID = @V0",
        CreateAttributeIdList(3, 4));

      MockDBUtils.AddReader(8,
        "SELECT T0.MEDIA_ITEM_ID A4, T0.MEDIA_ITEM_ID A5, T0.ROLE A0, T0.LINKEDROLE A1, T0.LINKEDID A2, T0.RELATIONSHIPINDEX A3 FROM M_RELATIONSHIP T0  WHERE T0.LINKEDID IN (@V0)");

      // Readers used by UpdateRelationships to find season item

      MockDBUtils.AddReader(9,
        "SELECT T0.MEDIA_ITEM_ID A30, T0.MEDIA_ITEM_ID A31, T1.MEDIA_ITEM_ID A32, T2.MEDIA_ITEM_ID A33, T3.MEDIA_ITEM_ID A34, T4.MEDIA_ITEM_ID A35, T5.MEDIA_ITEM_ID A36, " +
        "T0.SERIESNAME_0 A0, T0.SEASON_0 A1, T0.SERIESSEASONNAME_0 A2, T0.DESCRIPTION A3, T0.FIRSTAIRED_0 A4, T0.TOTALRATING_0 A5, T0.RATINGCOUNT_0 A6, " +
        "T1.TITLE A7, T1.RECORDINGTIME A8, T1.RATING A9, T1.COMMENT A10, T1.PLAYCOUNT A11, T1.LASTPLAYED A12, " +
        "T2.SERIESNAME A13, T2.SEASON A14, T2.SERIESSEASONNAME A15, T2.EPISODENAME A16, T2.FIRSTAIRED A17, T2.TOTALRATING A18, T2.RATINGCOUNT A19, " +
        "T3.LASTIMPORTDATE A20, T3.DIRTY A21, T3.DATEADDED A22, " +
        "T4.SYSTEM_ID A23, T4.MIMETYPE A24, T4.SIZE A25, T4.PATH A26, T4.PARENTDIRECTORY A27, " +
        "T5.SERIESNAME_1 A28, T5.DESCRIPTION_0 A29 " +
        "FROM M_SEASONITEM T0 " +
        "INNER JOIN M_MEDIAITEM T1 ON T1.MEDIA_ITEM_ID = T0.MEDIA_ITEM_ID " +
        "LEFT OUTER JOIN M_EPISODEITEM T2 ON T2.MEDIA_ITEM_ID = T0.MEDIA_ITEM_ID " +
        "LEFT OUTER JOIN M_IMPORTEDITEM T3 ON T3.MEDIA_ITEM_ID = T0.MEDIA_ITEM_ID " +
        "LEFT OUTER JOIN M_PROVIDERRESOURCE T4 ON T4.MEDIA_ITEM_ID = T0.MEDIA_ITEM_ID " +
        "LEFT OUTER JOIN M_SERIESITEM T5 ON T5.MEDIA_ITEM_ID = T0.MEDIA_ITEM_ID  " +
        "WHERE T0.MEDIA_ITEM_ID IN(SELECT MEDIA_ITEM_ID FROM M_EXTERNALIDENTIFIER WHERE SOURCE = @V0 AND TYPE = @V1 AND ID = @V2)",
        CreateAttributeIdList(30, 36));

      MockDBUtils.AddReader(10, "SELECT MEDIA_ITEM_ID FROM M_PROVIDERRESOURCE WHERE SYSTEM_ID = @SYSTEM_ID AND PATH = @PATH", "MEDIA_ITEM_ID");

      MockReader reader10 = MockDBUtils.AddReader(11,
        "SELECT T6.MEDIA_ITEM_ID A30, T0.MEDIA_ITEM_ID A31, T1.MEDIA_ITEM_ID A32, T2.MEDIA_ITEM_ID A33, T3.MEDIA_ITEM_ID A34, T4.MEDIA_ITEM_ID A35, T5.MEDIA_ITEM_ID A36, " +
        "T0.SERIESNAME A0, T0.SEASON A1, T0.SERIESSEASONNAME A2, T0.EPISODENAME A3, T0.FIRSTAIRED A4, T0.TOTALRATING A5, T0.RATINGCOUNT A6, " +
        "T1.LASTIMPORTDATE A7, T1.DIRTY A8, T1.DATEADDED A9, " +
        "T2.TITLE A10, T2.RECORDINGTIME A11, T2.RATING A12, T2.COMMENT A13, T2.PLAYCOUNT A14, T2.LASTPLAYED A15, " +
        "T3.SYSTEM_ID A16, T3.MIMETYPE A17, T3.SIZE A18, T3.PATH A19, T3.PARENTDIRECTORY A20, " +
        "T4.SERIESNAME_0 A21, T4.SEASON_0 A22, T4.SERIESSEASONNAME_0 A23, T4.DESCRIPTION A24, T4.FIRSTAIRED_0 A25, T4.TOTALRATING_0 A26, T4.RATINGCOUNT_0 A27, " +
        "T5.SERIESNAME_1 A28, T5.DESCRIPTION_0 A29 " +
        "FROM MEDIA_ITEMS T6 " +
        "LEFT OUTER JOIN M_EPISODEITEM T0 ON T0.MEDIA_ITEM_ID = T6.MEDIA_ITEM_ID " +
        "LEFT OUTER JOIN M_IMPORTEDITEM T1 ON T1.MEDIA_ITEM_ID = T6.MEDIA_ITEM_ID " +
        "LEFT OUTER JOIN M_MEDIAITEM T2 ON T2.MEDIA_ITEM_ID = T6.MEDIA_ITEM_ID " +
        "LEFT OUTER JOIN M_PROVIDERRESOURCE T3 ON T3.MEDIA_ITEM_ID = T6.MEDIA_ITEM_ID " +
        "LEFT OUTER JOIN M_SEASONITEM T4 ON T4.MEDIA_ITEM_ID = T6.MEDIA_ITEM_ID " +
        "LEFT OUTER JOIN M_SERIESITEM T5 ON T5.MEDIA_ITEM_ID = T6.MEDIA_ITEM_ID  " +
        "WHERE T6.MEDIA_ITEM_ID = @V0",
        CreateAttributeIdList(30, 36));
      reader10.AddResult(
        seasonItemId, null, seasonItemId, seasonItemId, seasonItemId, seasonItemId, null,
        null, null, null, null, null, null, null,
        importDate, false, importDate,
        seriesSeasonName, null, null, null, null, null,
        null, null, 0, null, Guid.Empty,
        seriesName, season, seriesSeasonName, seasonDescription, null, null, null,
        null, null
      );

      MockReader reader11 = MockDBUtils.AddReader(12,
        "SELECT T0.MEDIA_ITEM_ID A3, T0.MEDIA_ITEM_ID A4, " +
        "T0.SOURCE A0, T0.TYPE A1, T0.ID A2 " +
        "FROM M_EXTERNALIDENTIFIER T0  " +
        "WHERE T0.MEDIA_ITEM_ID = @V0",
        CreateAttributeIdList(3, 4));
      reader11.AddResult(
        seasonItemId, seasonItemId,
        externalSource, ExternalIdentifierAspect.TYPE_SERIES, externalSeriesId);

      MockDBUtils.AddReader(13,
        "SELECT T0.MEDIA_ITEM_ID A4, T0.MEDIA_ITEM_ID A5, " +
        "T0.ROLE A0, T0.LINKEDROLE A1, T0.LINKEDID A2, T0.RELATIONSHIPINDEX A3 " +
        "FROM M_RELATIONSHIP T0  " +
        "WHERE T0.MEDIA_ITEM_ID = @V0",
        CreateAttributeIdList(3, 4));

      MockDBUtils.AddReader(14,
        "SELECT T0.MEDIA_ITEM_ID A4, T0.MEDIA_ITEM_ID A5, T0.ROLE A0, T0.LINKEDROLE A1, T0.LINKEDID A2, T0.RELATIONSHIPINDEX A3 FROM M_RELATIONSHIP T0  WHERE T0.LINKEDID IN (@V0)");

      MockDBUtils.AddReader(15,
        "SELECT T0.MEDIA_ITEM_ID A30, T0.MEDIA_ITEM_ID A31, T1.MEDIA_ITEM_ID A32, T2.MEDIA_ITEM_ID A33, T3.MEDIA_ITEM_ID A34, T4.MEDIA_ITEM_ID A35, T5.MEDIA_ITEM_ID A36, " +
        "T0.SERIESNAME_1 A0, T0.DESCRIPTION_0 A1, " +
        "T1.TITLE A2, T1.RECORDINGTIME A3, T1.RATING A4, T1.COMMENT A5, T1.PLAYCOUNT A6, T1.LASTPLAYED A7, " +
        "T2.SERIESNAME A8, T2.SEASON A9, T2.SERIESSEASONNAME A10, T2.EPISODENAME A11, T2.FIRSTAIRED A12, T2.TOTALRATING A13, T2.RATINGCOUNT A14, " +
        "T3.LASTIMPORTDATE A15, T3.DIRTY A16, T3.DATEADDED A17, " +
        "T4.SYSTEM_ID A18, T4.MIMETYPE A19, T4.SIZE A20, T4.PATH A21, T4.PARENTDIRECTORY A22, " +
        "T5.SERIESNAME_0 A23, T5.SEASON_0 A24, T5.SERIESSEASONNAME_0 A25, T5.DESCRIPTION A26, T5.FIRSTAIRED_0 A27, T5.TOTALRATING_0 A28, T5.RATINGCOUNT_0 A29 " +
        "FROM M_SERIESITEM T0 " +
        "INNER JOIN M_MEDIAITEM T1 ON T1.MEDIA_ITEM_ID = T0.MEDIA_ITEM_ID " +
        "LEFT OUTER JOIN M_EPISODEITEM T2 ON T2.MEDIA_ITEM_ID = T0.MEDIA_ITEM_ID " +
        "LEFT OUTER JOIN M_IMPORTEDITEM T3 ON T3.MEDIA_ITEM_ID = T0.MEDIA_ITEM_ID " +
        "LEFT OUTER JOIN M_PROVIDERRESOURCE T4 ON T4.MEDIA_ITEM_ID = T0.MEDIA_ITEM_ID " +
        "LEFT OUTER JOIN M_SEASONITEM T5 ON T5.MEDIA_ITEM_ID = T0.MEDIA_ITEM_ID  " +
        "WHERE T0.MEDIA_ITEM_ID IN(SELECT MEDIA_ITEM_ID FROM M_EXTERNALIDENTIFIER WHERE SOURCE = @V0 AND TYPE = @V1 AND ID = @V2)",
        CreateAttributeIdList(30, 36));

      MockDBUtils.AddReader(16, "SELECT MEDIA_ITEM_ID FROM M_PROVIDERRESOURCE WHERE SYSTEM_ID = @SYSTEM_ID AND PATH = @PATH", "MEDIA_ITEM_ID");

      // Readers used by UpdateRelationships to find series item

      MockReader reader16 = MockDBUtils.AddReader(17,
        "SELECT T6.MEDIA_ITEM_ID A30, T0.MEDIA_ITEM_ID A31, T1.MEDIA_ITEM_ID A32, T2.MEDIA_ITEM_ID A33, T3.MEDIA_ITEM_ID A34, T4.MEDIA_ITEM_ID A35, T5.MEDIA_ITEM_ID A36, " +
        "T0.SERIESNAME A0, T0.SEASON A1, T0.SERIESSEASONNAME A2, T0.EPISODENAME A3, T0.FIRSTAIRED A4, T0.TOTALRATING A5, T0.RATINGCOUNT A6, " +
        "T1.LASTIMPORTDATE A7, T1.DIRTY A8, T1.DATEADDED A9, " +
        "T2.TITLE A10, T2.RECORDINGTIME A11, T2.RATING A12, T2.COMMENT A13, T2.PLAYCOUNT A14, T2.LASTPLAYED A15, " +
        "T3.SYSTEM_ID A16, T3.MIMETYPE A17, T3.SIZE A18, T3.PATH A19, T3.PARENTDIRECTORY A20, " +
        "T4.SERIESNAME_0 A21, T4.SEASON_0 A22, T4.SERIESSEASONNAME_0 A23, T4.DESCRIPTION A24, T4.FIRSTAIRED_0 A25, T4.TOTALRATING_0 A26, T4.RATINGCOUNT_0 A27, " +
        "T5.SERIESNAME_1 A28, T5.DESCRIPTION_0 A29 " +
        "FROM MEDIA_ITEMS T6 " +
        "LEFT OUTER JOIN M_EPISODEITEM T0 ON T0.MEDIA_ITEM_ID = T6.MEDIA_ITEM_ID " +
        "LEFT OUTER JOIN M_IMPORTEDITEM T1 ON T1.MEDIA_ITEM_ID = T6.MEDIA_ITEM_ID " +
        "LEFT OUTER JOIN M_MEDIAITEM T2 ON T2.MEDIA_ITEM_ID = T6.MEDIA_ITEM_ID " +
        "LEFT OUTER JOIN M_PROVIDERRESOURCE T3 ON T3.MEDIA_ITEM_ID = T6.MEDIA_ITEM_ID " +
        "LEFT OUTER JOIN M_SEASONITEM T4 ON T4.MEDIA_ITEM_ID = T6.MEDIA_ITEM_ID " +
        "LEFT OUTER JOIN M_SERIESITEM T5 ON T5.MEDIA_ITEM_ID = T6.MEDIA_ITEM_ID  " +
        "WHERE T6.MEDIA_ITEM_ID = @V0",
        CreateAttributeIdList(30, 36));
      reader16.AddResult(
        seriesItemId, null, seasonItemId, seasonItemId, seasonItemId, null, seriesItemId,
        null, null, null, null, null, null, null,
        importDate, false, importDate,
        seriesName, null, null, null, null, null,
        null, null, 0, null, Guid.Empty,
        null, null, null, null, null, null, null,
        seriesName, seriesDescription
      );

      MockReader reader17 = MockDBUtils.AddReader(18,
        "SELECT T0.MEDIA_ITEM_ID A3, T0.MEDIA_ITEM_ID A4, " +
        "T0.SOURCE A0, T0.TYPE A1, T0.ID A2 " +
        "FROM M_EXTERNALIDENTIFIER T0  " +
        "WHERE T0.MEDIA_ITEM_ID = @V0",
        CreateAttributeIdList(3, 4));
      reader17.AddResult(
        seriesItemId, seriesItemId,
        externalSource, ExternalIdentifierAspect.TYPE_SERIES, externalSeriesId);

      MockDBUtils.AddReader(19,
        "SELECT T0.MEDIA_ITEM_ID A4, T0.MEDIA_ITEM_ID A5, " +
        "T0.ROLE A0, T0.LINKEDROLE A1, T0.LINKEDID A2, T0.RELATIONSHIPINDEX A3 " +
        "FROM M_RELATIONSHIP T0  " +
        "WHERE T0.MEDIA_ITEM_ID = @V0",
        CreateAttributeIdList(4, 5));

      MockDBUtils.AddReader(20,
        "SELECT T0.MEDIA_ITEM_ID A4, T0.MEDIA_ITEM_ID A5, T0.ROLE A0, T0.LINKEDROLE A1, T0.LINKEDID A2, T0.RELATIONSHIPINDEX A3 FROM M_RELATIONSHIP T0  WHERE T0.LINKEDID IN (@V0)");

      MockReader reader20 = MockDBUtils.AddReader(21,
        "SELECT MEDIA_ITEM_ID " +
        "FROM M_EXTERNALIDENTIFIER " +
        "WHERE MEDIA_ITEM_ID = @MEDIA_ITEM_ID AND SOURCE = @SOURCE AND TYPE = @TYPE",
        "MEDIA_ITEM_ID");
      reader20.AddResult(
        seasonItemId,
        externalSource, ExternalIdentifierAspect.TYPE_SERIES, externalSeriesId);

      MockDBUtils.AddReader(22,
        "SELECT MEDIA_ITEM_ID " +
        "FROM M_RELATIONSHIP " +
        "WHERE MEDIA_ITEM_ID = @MEDIA_ITEM_ID AND ROLE = @ROLE AND LINKEDROLE = @LINKEDROLE AND LINKEDID = @LINKEDID",
        "MEDIA_ITEM_ID");

      MockReader reader22 = MockDBUtils.AddReader(23,
        "SELECT MEDIA_ITEM_ID " +
        "FROM M_EXTERNALIDENTIFIER " +
        "WHERE MEDIA_ITEM_ID = @MEDIA_ITEM_ID AND SOURCE = @SOURCE AND TYPE = @TYPE",
        "MEDIA_ITEM_ID");
      reader22.AddResult(
        seasonItemId,
        externalSource, ExternalIdentifierAspect.TYPE_SERIES, externalSeriesId);

      MockDBUtils.AddReader(24,
        "SELECT MEDIA_ITEM_ID " +
        "FROM M_RELATIONSHIP " +
        "WHERE MEDIA_ITEM_ID = @MEDIA_ITEM_ID AND ROLE = @ROLE AND LINKEDROLE = @LINKEDROLE AND LINKEDID = @LINKEDID",
        "MEDIA_ITEM_ID");

      MockCore.Library.AddOrUpdateMediaItem(parentDirectoryId, systemId, path, episodeAspects.Values.SelectMany(x => x), true);

      MockCore.ShutdownLibrary();
    }
  }
}
