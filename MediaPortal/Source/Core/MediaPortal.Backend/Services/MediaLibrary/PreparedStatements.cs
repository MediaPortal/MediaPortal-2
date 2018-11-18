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

using MediaPortal.Backend.Services.UserProfileDataManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;

namespace MediaPortal.Backend.Services.MediaLibrary
{
  public class PreparedStatements
  {
    private MIA_Management _miaManagement;

    private string _selectMediaItemRelationshipsFromIdSQL;
    private string _updateMediaItemsDirtyAttributeFromIdSQL;
    private string _selectParentIdsFromChildIdsSQL;
    private string _selectPlayDataFromParentIdSQL;
    private string _selectUserDataFromParentIdSQL;
    private string _selectMediaItemIdFromPathSQL;
    private string _insertUserPlayCountSQL;
    private string _deleteMediaItemRelationshipsFromIdSQL;
    private string _selectOrphanCountSQL;
    private string _updateSetsSQL;
    private string _updateSetsForIdSQL;
    private string _updateUserPlayDataFromIdSQL;
    private string _insertUserPlayDataForIdSQL;
    private string _selectMediaItemUserDataFromIdsSQL;

    public PreparedStatements(MIA_Management miaManagement)
    {
      _miaManagement = miaManagement;
    }

    public string SelectMediaItemRelationshipsFromIdSQL
    {
      get
      {
        if (_selectMediaItemRelationshipsFromIdSQL == null)
          _selectMediaItemRelationshipsFromIdSQL = "SELECT " + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME +
          " FROM " + _miaManagement.GetMIATableName(RelationshipAspect.Metadata) +
          " WHERE " + _miaManagement.GetMIAAttributeColumnName(RelationshipAspect.ATTR_LINKED_ID) + " = @ITEM_ID" +
          " UNION" +
          " SELECT " + _miaManagement.GetMIAAttributeColumnName(RelationshipAspect.ATTR_LINKED_ID) +
          " FROM " + _miaManagement.GetMIATableName(RelationshipAspect.Metadata) +
          " WHERE " + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + " = @ITEM_ID";
        return _selectMediaItemRelationshipsFromIdSQL;
      }
    }

    public string UpdateMediaItemsDirtyAttributeFromIdSQL
    {
      get
      {
        if (_updateMediaItemsDirtyAttributeFromIdSQL == null)
          _updateMediaItemsDirtyAttributeFromIdSQL = "UPDATE " + _miaManagement.GetMIATableName(ImporterAspect.Metadata) +
          " SET " + _miaManagement.GetMIAAttributeColumnName(ImporterAspect.ATTR_DIRTY) + " = 1" +
          " WHERE " + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + " IN ({0})";
        return _updateMediaItemsDirtyAttributeFromIdSQL;
      }
    }

    public string SelectParentIdsFromChildIdsSQL
    {
      get
      {
        if (_selectParentIdsFromChildIdsSQL == null)
          _selectParentIdsFromChildIdsSQL = "SELECT DISTINCT " + _miaManagement.GetMIAAttributeColumnName(RelationshipAspect.ATTR_LINKED_ID) +
          " FROM " + _miaManagement.GetMIATableName(RelationshipAspect.Metadata) +
          " WHERE " + _miaManagement.GetMIAAttributeColumnName(RelationshipAspect.ATTR_PLAYABLE) + " = 1" +
          " AND " + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + " IN({0})";
        return _selectParentIdsFromChildIdsSQL;
      }
    }

    public string SelectPlayDataFromParentIdSQL
    {
      get
      {
        if (_selectPlayDataFromParentIdSQL == null)
          _selectPlayDataFromParentIdSQL = "SELECT M." + _miaManagement.GetMIAAttributeColumnName(MediaAspect.ATTR_ISVIRTUAL) +
          ", U." + UserProfileDataManagement_SubSchema.USER_DATA_VALUE_COL_NAME +
          ", U2." + UserProfileDataManagement_SubSchema.USER_DATA_VALUE_COL_NAME +
          " FROM " + _miaManagement.GetMIATableName(MediaAspect.Metadata) + " M" +
          " LEFT OUTER JOIN " + UserProfileDataManagement_SubSchema.USER_MEDIA_ITEM_DATA_TABLE_NAME + " U" +
          " ON U." + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + " = M." + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME +
          " AND U." + UserProfileDataManagement_SubSchema.USER_PROFILE_ID_COL_NAME + " = @USER_PROFILE_ID" +
          " AND U." + UserProfileDataManagement_SubSchema.USER_DATA_KEY_COL_NAME + " = @USER_DATA_KEY" +
          " LEFT OUTER JOIN " + UserProfileDataManagement_SubSchema.USER_MEDIA_ITEM_DATA_TABLE_NAME + " U2" +
          " ON U2." + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + " = M." + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME +
          " AND U2." + UserProfileDataManagement_SubSchema.USER_PROFILE_ID_COL_NAME + " = @USER_PROFILE_ID" +
          " AND U2." + UserProfileDataManagement_SubSchema.USER_DATA_KEY_COL_NAME + " = @USER_DATA_KEY2" +
          " WHERE M." + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + " IN (" +
          " SELECT " + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME +
          " FROM " + _miaManagement.GetMIATableName(RelationshipAspect.Metadata) + " WHERE " + _miaManagement.GetMIAAttributeColumnName(RelationshipAspect.ATTR_LINKED_ID) + " = @ITEM_ID" +
          " AND " + _miaManagement.GetMIAAttributeColumnName(RelationshipAspect.ATTR_PLAYABLE) + " = 1" +
          ")";
        return _selectPlayDataFromParentIdSQL;
      }
    }

    public string SelectUserDataFromParentIdSQL
    {
      get
      {
        if (_selectUserDataFromParentIdSQL == null)
          _selectUserDataFromParentIdSQL = "SELECT R." + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME +
          ", U." + UserProfileDataManagement_SubSchema.USER_DATA_VALUE_COL_NAME +
          " FROM " + _miaManagement.GetMIATableName(RelationshipAspect.Metadata) + " R" +
          " JOIN " + _miaManagement.GetMIATableName(MediaAspect.Metadata) + " M" +
          " ON M." + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + " = R." + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME +
          " LEFT OUTER JOIN " + UserProfileDataManagement_SubSchema.USER_MEDIA_ITEM_DATA_TABLE_NAME + " U" +
          " ON U." + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + " = R." + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME +
          " AND U." + UserProfileDataManagement_SubSchema.USER_PROFILE_ID_COL_NAME + " = @USER_PROFILE_ID" +
          " AND U." + UserProfileDataManagement_SubSchema.USER_DATA_KEY_COL_NAME + " = @USER_DATA_KEY" +
          " WHERE R." + _miaManagement.GetMIAAttributeColumnName(RelationshipAspect.ATTR_LINKED_ID) + " = @ITEM_ID" +
          " AND R." + _miaManagement.GetMIAAttributeColumnName(RelationshipAspect.ATTR_PLAYABLE) + " = 1" +
          " AND M." + _miaManagement.GetMIAAttributeColumnName(MediaAspect.ATTR_ISVIRTUAL) + " = 0";
        return _selectUserDataFromParentIdSQL;
      }
    }

    public string SelectMediaItemIdFromPathSQL
    {
      get
      {
        if (_selectMediaItemIdFromPathSQL == null)
          _selectMediaItemIdFromPathSQL = "SELECT " + MIA_Management.MIA_MEDIA_ITEM_ID_COL_NAME + " FROM " + _miaManagement.GetMIATableName(ProviderResourceAspect.Metadata) +
          " WHERE " + _miaManagement.GetMIAAttributeColumnName(ProviderResourceAspect.ATTR_SYSTEM_ID) + " = @SYSTEM_ID AND " +
          _miaManagement.GetMIAAttributeColumnName(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH) + " = @PATH";
        return _selectMediaItemIdFromPathSQL;
      }
    }

    public string InsertUserPlayCountSQL
    {
      get
      {
        if (_insertUserPlayCountSQL == null)
          _insertUserPlayCountSQL = "INSERT INTO " + UserProfileDataManagement_SubSchema.USER_MEDIA_ITEM_DATA_TABLE_NAME +
          "(" + UserProfileDataManagement_SubSchema.USER_PROFILE_ID_COL_NAME + ", " +
          MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + ", " + UserProfileDataManagement_SubSchema.USER_DATA_KEY_COL_NAME + ", " +
          UserProfileDataManagement_SubSchema.USER_DATA_VALUE_COL_NAME + ") " +
          "SELECT " + UserProfileDataManagement_SubSchema.USER_PROFILE_ID_COL_NAME + ", @MEDIA_ITEM_ID, @DATA_KEY, @MEDIA_ITEM_DATA FROM " +
          UserProfileDataManagement_SubSchema.USER_TABLE_NAME;
        return _insertUserPlayCountSQL;
      }
    }

    public string DeleteMediaItemRelationshipsFromIdSQL
    {
      get
      {
        if (_deleteMediaItemRelationshipsFromIdSQL == null)
          _deleteMediaItemRelationshipsFromIdSQL = "DELETE FROM " + _miaManagement.GetMIATableName(RelationshipAspect.Metadata) +
          " WHERE " + _miaManagement.GetMIAAttributeColumnName(RelationshipAspect.ATTR_LINKED_ID) + " = @ITEM_ID";
        return _deleteMediaItemRelationshipsFromIdSQL;
      }
    }

    public string SelectOrphanCountSQL
    {
      get
      {
        if (_selectOrphanCountSQL == null)
          _selectOrphanCountSQL = "SELECT COUNT(*) FROM " + MediaLibrary_SubSchema.MEDIA_ITEMS_TABLE_NAME +
          " WHERE " + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + " IN (" +
          " SELECT T0." + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME +
          " FROM " + _miaManagement.GetMIATableName(MediaAspect.Metadata) + " T0" +
          " JOIN " + _miaManagement.GetMIATableName(ProviderResourceAspect.Metadata) + " T1 ON " +
          " T1." + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + " = " +
          " T0." + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME +
          " WHERE T0." + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + " = @ITEM_ID" +
          " AND T1." + _miaManagement.GetMIAAttributeColumnName(ProviderResourceAspect.ATTR_TYPE) + " = " + ProviderResourceAspect.TYPE_VIRTUAL +
          " AND NOT EXISTS (" +
          "SELECT " + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME +
          " FROM " + _miaManagement.GetMIATableName(RelationshipAspect.Metadata) +
          " WHERE " + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + " = @ITEM_ID" +
          " OR " + _miaManagement.GetMIAAttributeColumnName(RelationshipAspect.ATTR_LINKED_ID) + " = @ITEM_ID" +
          "))";
        return _selectOrphanCountSQL;
      }
    }

    public string UpdateSetsSQL
    {
      get
      {
        if (_updateSetsSQL == null)
          _updateSetsSQL = "UPDATE " + _miaManagement.GetMIATableName(VideoStreamAspect.Metadata) +
          " SET " + _miaManagement.GetMIAAttributeColumnName(VideoStreamAspect.ATTR_VIDEO_PART_SET) + " = -1" +
          " WHERE " + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + " NOT IN (" +
          " SELECT " + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME +
          " FROM " + _miaManagement.GetMIATableName(VideoStreamAspect.Metadata) +
          " WHERE " + _miaManagement.GetMIAAttributeColumnName(VideoStreamAspect.ATTR_VIDEO_PART) + " >= 0" +
          " UNION " +
          " SELECT " + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + 
          " FROM " + _miaManagement.GetMIATableName(ProviderResourceAspect.Metadata) +
          " WHERE " + _miaManagement.GetMIAAttributeColumnName(ProviderResourceAspect.ATTR_TYPE) + " IN (" +
          ProviderResourceAspect.TYPE_PRIMARY + "," + ProviderResourceAspect.TYPE_STUB + ")" +
          " GROUP BY " + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + " HAVING COUNT(*) > 1" +
          ")";
        return _updateSetsSQL;
      }
    }

    public string UpdateSetsForIdSQL
    {
      get
      {
        if (_updateSetsForIdSQL == null)
          _updateSetsForIdSQL = "UPDATE " + _miaManagement.GetMIATableName(VideoStreamAspect.Metadata) +
          " SET " + _miaManagement.GetMIAAttributeColumnName(VideoStreamAspect.ATTR_VIDEO_PART_SET) + " = -1" +
          " WHERE " + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + " = @ITEM_ID" +
          " AND NOT EXISTS(" + 
          " SELECT " + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME +
          " WHERE " + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + " = @ITEM_ID" +
          " FROM " + _miaManagement.GetMIATableName(VideoStreamAspect.Metadata) +
          " WHERE " + _miaManagement.GetMIAAttributeColumnName(VideoStreamAspect.ATTR_VIDEO_PART) + " >= 0" +
          ") AND NOT EXISTS(" +
          " SELECT " + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME +
          " FROM " + _miaManagement.GetMIATableName(ProviderResourceAspect.Metadata) +
          " WHERE " + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + " = @ITEM_ID" +
          " AND " + _miaManagement.GetMIAAttributeColumnName(ProviderResourceAspect.ATTR_TYPE) + " IN (" +
          ProviderResourceAspect.TYPE_PRIMARY + "," + ProviderResourceAspect.TYPE_STUB + ")" +
          " GROUP BY " + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + " HAVING COUNT(*) > 1" +
          ")";
        return _updateSetsForIdSQL;
      }
    }

    public string UpdateUserPlayDataFromIdSQL
    {
      get
      {
        if (_updateUserPlayDataFromIdSQL == null)
          _updateUserPlayDataFromIdSQL = "UPDATE " + UserProfileDataManagement_SubSchema.USER_MEDIA_ITEM_DATA_TABLE_NAME +
          " SET " + UserProfileDataManagement_SubSchema.USER_DATA_VALUE_COL_NAME + " = @USER_DATA_VALUE" +
          " WHERE " + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + " = @ITEM_ID" +
          " AND " + UserProfileDataManagement_SubSchema.USER_PROFILE_ID_COL_NAME + " = @USER_PROFILE_ID" +
          " AND " + UserProfileDataManagement_SubSchema.USER_DATA_KEY_COL_NAME + " = @USER_DATA_KEY";
        return _updateUserPlayDataFromIdSQL;
      }
    }

    public string InsertUserPlayDataForIdSQL
    {
      get
      {
        if (_insertUserPlayDataForIdSQL == null)
          _insertUserPlayDataForIdSQL = "INSERT INTO " + UserProfileDataManagement_SubSchema.USER_MEDIA_ITEM_DATA_TABLE_NAME +
          " (" + UserProfileDataManagement_SubSchema.USER_PROFILE_ID_COL_NAME + ", " + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + ", " +
          UserProfileDataManagement_SubSchema.USER_DATA_KEY_COL_NAME + ", " + UserProfileDataManagement_SubSchema.USER_DATA_VALUE_COL_NAME + ")" +
          " VALUES (@USER_PROFILE_ID, @ITEM_ID, @USER_DATA_KEY, @USER_DATA_VALUE)";
        return _insertUserPlayDataForIdSQL;
      }
    }

    public string SelectMediaItemUserDataFromIdsSQL
    {
      get
      {
        if (_selectMediaItemUserDataFromIdsSQL == null)
          _selectMediaItemUserDataFromIdsSQL = "SELECT " + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + "," + UserProfileDataManagement_SubSchema.USER_DATA_KEY_COL_NAME + "," +
          UserProfileDataManagement_SubSchema.USER_DATA_VALUE_COL_NAME +
          " FROM " + UserProfileDataManagement_SubSchema.USER_MEDIA_ITEM_DATA_TABLE_NAME +
          " WHERE " + UserProfileDataManagement_SubSchema.USER_PROFILE_ID_COL_NAME + " = @USER_PROFILE_ID AND " + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME + " IN({0})";
        return _selectMediaItemUserDataFromIdsSQL;
      }
    }
  }
}
