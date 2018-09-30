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

using MediaPortal.Utilities.UPnP;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace MediaPortal.Common.UserProfileDataManagement
{
  /// <summary>
  /// Defines the known user profile types.
  /// </summary>
  public enum UserProfileType
  {
    ClientProfile = 0,
    UserProfile = 1,
  }

  public class UserProfileTemplate
  {
    public Guid TemplateId { get; set; }
    public string TemplateName { get; set; }
    public bool EnableRestrictionGroups { get; set; }
    public ICollection<string> RestrictionGroups { get; set; }
    public bool RestrictAges { get; set; }
    public int? AllowedAge { get; set; }
  }

  /// <summary>
  /// Data object for a named user profile.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Note: This class is serialized/deserialized by the <see cref="XmlSerializer"/>.
  /// If changed, this has to be taken into consideration.
  /// </para>
  /// </remarks>
  public class UserProfile
  {
    protected Guid _profileId;
    protected string _name;
    protected string _password;
    protected DateTime? _lastLogin;
    protected UserProfileType _profileType;
    protected byte[] _image;
    protected IDictionary<string, IDictionary<int, string>> _userData = new Dictionary<string, IDictionary<int, string>>();
    protected readonly ICollection<string> EMPTY_COLLECTION = new HashSet<string>();

    // We could use some cache for this instance, if we would have one...
    protected static XmlSerializer _xmlSerializer = null; // Lazy initialized

    public UserProfile(Guid profileId, string name, UserProfileType profileType = UserProfileType.ClientProfile, string password = null, DateTime? lastLogin = null, byte[] image = null)
    {
      _profileId = profileId;
      _name = name;
      _password = password;
      _lastLogin = lastLogin;
      _profileType = profileType;
      _image = image;
    }

    public void Rename(string newName)
    {
      _name = newName;
    }

    public void AddAdditionalData(string key, string value)
    {
      if (!AdditionalData.ContainsKey(key))
        AdditionalData.Add(key, new Dictionary<int, string>());
      AdditionalData[key].Add(0, value);
    }

    public void AddAdditionalData(string key, int valueNo, string value)
    {
      if (!AdditionalData.ContainsKey(key))
        AdditionalData.Add(key, new Dictionary<int, string>());
      AdditionalData[key].Add(valueNo, value);
    }

    /// <summary>
    /// Indicates if restrictions should be applied for this user.
    /// </summary>
    [XmlIgnore]
    public bool EnableRestrictionGroups
    {
      get
      {
        IDictionary<int, string> values;
        if (!AdditionalData.TryGetValue(UserDataKeysKnown.KEY_ENABLE_RESTRICTION_GROUPS, out values) || values.Count == 0)
          return false;
        return values[0] == "1";
      }
      set
      {
        AddAdditionalData(UserDataKeysKnown.KEY_ENABLE_RESTRICTION_GROUPS, value ? "1" : "0");
      }
    }

    /// <summary>
    /// If <see cref="EnableRestrictionGroups"/> is <c>true</c>,  this property exposes all allowed group names.
    /// </summary>
    [XmlIgnore]
    public ICollection<string> RestrictionGroups
    {
      get
      {
        IDictionary<int, string> values;
        if (!AdditionalData.TryGetValue(UserDataKeysKnown.KEY_RESTRICTION_GROUPS, out values))
          return EMPTY_COLLECTION;

        return new HashSet<string>(values.Values, StringComparer.InvariantCultureIgnoreCase);
      }
      set
      {
        int idx = 0;
        foreach (string group in value)
          AddAdditionalData(UserDataKeysKnown.KEY_RESTRICTION_GROUPS, ++idx, group);
      }
    }

    /// <summary>
    /// Returns the globally unique id of this user profile.
    /// </summary>
    [XmlIgnore]
    public Guid ProfileId
    {
      get { return _profileId; }
    }

    /// <summary>
    /// Returns the template ID which was used when user was created.
    /// </summary>
    [XmlIgnore]
    public Guid TemplateId
    {
      get
      {
        IDictionary<int, string> value;
        Guid templateId;
        if (AdditionalData.TryGetValue(UserDataKeysKnown.KEY_TEMPLATE_ID, out value) &&
          Guid.TryParse(value.Values.FirstOrDefault(), out templateId))
          return templateId;
        return Guid.Empty;
      }
      set
      {
        AddAdditionalData(UserDataKeysKnown.KEY_TEMPLATE_ID, 0, value.ToString());
      }
    }

    /// <summary>
    /// Define if age restrictions should be applied.
    /// </summary>
    [XmlIgnore]
    public bool RestrictAges
    {
      get
      {
        IDictionary<int, string> value;
        return AdditionalData.TryGetValue(UserDataKeysKnown.KEY_ALLOW_ALL_AGES, out value) && value.Values.FirstOrDefault() == "0";
      }
      set
      {
        AddAdditionalData(UserDataKeysKnown.KEY_ALLOW_ALL_AGES, 0, value ? "0" : "1");
      }
    }
    /// <summary>
    /// Define the allowed age.
    /// </summary>
    [XmlIgnore]
    public int? AllowedAge
    {
      get
      {
        IDictionary<int, string> value;
        int age;
        if (AdditionalData.TryGetValue(UserDataKeysKnown.KEY_ALLOWED_AGE, out value) &&
            int.TryParse(value.Values.FirstOrDefault(), out age))
          return age;
        return null;
      }
      set
      {
        AddAdditionalData(UserDataKeysKnown.KEY_ALLOWED_AGE, 0, value.ToString());
      }
    }

    /// <summary>
    /// Define if share restrictions should be applied.
    /// </summary>
    [XmlIgnore]
    public bool RestrictShares
    {
      get
      {
        IDictionary<int, string> value;
        return AdditionalData.TryGetValue(UserDataKeysKnown.KEY_ALLOW_ALL_SHARES, out value) && value.Values.FirstOrDefault() == "0";
      }
      set
      {
        AddAdditionalData(UserDataKeysKnown.KEY_ALLOW_ALL_SHARES, 0, value ? "0" : "1");
      }
    }

    /// <summary>
    /// Define if PG content should be allowed.
    /// </summary>
    [XmlIgnore]
    public bool IncludeParentGuidedContent
    {
      get
      {
        IDictionary<int, string> value;
        return AdditionalData.TryGetValue(UserDataKeysKnown.KEY_INCLUDE_PARENT_GUIDED_CONTENT, out value) && value.Values.FirstOrDefault() == "1";
      }
      set
      {
        AddAdditionalData(UserDataKeysKnown.KEY_INCLUDE_PARENT_GUIDED_CONTENT, 0, value ? "1" : "0");
      }
    }


    /// <summary>
    /// Define if unrated content should be allowed.
    /// </summary>
    [XmlIgnore]
    public bool IncludeUnratedContent
    {
      get
      {
        IDictionary<int, string> value;
        return AdditionalData.TryGetValue(UserDataKeysKnown.KEY_INCLUDE_UNRATED_CONTENT, out value) && value.Values.FirstOrDefault() == "1";
      }
      set
      {
        AddAdditionalData(UserDataKeysKnown.KEY_INCLUDE_UNRATED_CONTENT, 0, value ? "1" : "0");
      }
    }

    /// <summary>
    /// Returns the user name of this profile.
    /// </summary>
    [XmlIgnore]
    public string Name
    {
      get { return _name; }
    }

    /// <summary>
    /// Returns the type this profile.
    /// </summary>
    [XmlIgnore]
    public UserProfileType ProfileType
    {
      get { return _profileType; }
    }

    /// <summary>
    /// Returns the hashed password of this profile.
    /// </summary>
    [XmlIgnore]
    public string Password
    {
      get { return _password; }
    }

    /// <summary>
    /// Returns the last login of this profile.
    /// </summary>
    [XmlIgnore]
    public DateTime? LastLogin
    {
      get { return _lastLogin; }
      set { _lastLogin = value; }
    }

    /// <summary>
    /// Returns the image of this profile.
    /// </summary>
    [XmlIgnore]
    public byte[] Image
    {
      get { return _image; }
      set { _image = value; }
    }

    /// <summary>
    /// Returns the data of this profile.
    /// </summary>
    [XmlIgnore]
    public IDictionary<string, IDictionary<int, string>> AdditionalData
    {
      get { return _userData; }
    }

    /// <summary>
    /// Serializes this user profile instance to XML.
    /// </summary>
    /// <returns>String containing an XML fragment with this instance's data.</returns>
    public string Serialize()
    {
      XmlSerializer xs = GetOrCreateXMLSerializer();
      StringBuilder sb = new StringBuilder(); // Will contain the data, formatted as XML
      using (XmlWriter writer = XmlWriter.Create(sb, new XmlWriterSettings { OmitXmlDeclaration = true }))
        xs.Serialize(writer, this);
      return sb.ToString();
    }

    /// <summary>
    /// Serializes this user profile instance to the given <paramref name="writer"/>.
    /// </summary>
    /// <param name="writer">Writer to write the XML serialization to.</param>
    public void Serialize(XmlWriter writer)
    {
      XmlSerializer xs = GetOrCreateXMLSerializer();
      xs.Serialize(writer, this);
    }

    /// <summary>
    /// Deserializes a user profile instance from a given XML fragment.
    /// </summary>
    /// <param name="str">XML fragment containing a serialized user profile instance.</param>
    /// <returns>Deserialized instance.</returns>
    public static UserProfile Deserialize(string str)
    {
      XmlSerializer xs = GetOrCreateXMLSerializer();
      using (StringReader reader = new StringReader(str))
        return xs.Deserialize(reader) as UserProfile;
    }

    /// <summary>
    /// Deserializes a user profile instance from a given <paramref name="reader"/>.
    /// </summary>
    /// <param name="reader">XML reader containing a serialized user profile instance.</param>
    /// <returns>Deserialized instance.</returns>
    public static UserProfile Deserialize(XmlReader reader)
    {
      XmlSerializer xs = GetOrCreateXMLSerializer();
      return xs.Deserialize(reader) as UserProfile;
    }

    #region Base overrides

    public override bool Equals(object obj)
    {
      if (!(obj is UserProfile))
        return false;
      UserProfile other = (UserProfile)obj;
      return _profileId == other._profileId;
    }

    public override int GetHashCode()
    {
      return _profileId.GetHashCode();
    }

    public override string ToString()
    {
      return string.Format("User profile {0}: Name='{1}'", _profileId, _name);
    }

    #endregion

    #region Additional members for the XML serialization

    internal UserProfile() { }

    protected static XmlSerializer GetOrCreateXMLSerializer()
    {
      return _xmlSerializer ?? (_xmlSerializer = new XmlSerializer(typeof(UserProfile)));
    }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlAttribute("Id")]
    public Guid XML_Id
    {
      get { return _profileId; }
      set { _profileId = value; }
    }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlAttribute("Name")]
    public string XML_Name
    {
      get { return _name; }
      set { _name = value; }
    }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlAttribute("ProfileType")]
    public UserProfileType XML_ProfileType
    {
      get { return _profileType; }
      set { _profileType = value; }
    }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlAttribute("Password")]
    public string XML_Password
    {
      get { return _password; }
      set { _password = value; }
    }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlAttribute("LastLogin")]
    public long XML_LastLogin
    {
      get { return _lastLogin.HasValue ? _lastLogin.Value.Ticks : 0; }
      set { _lastLogin = value == 0 ? (DateTime?)null : new DateTime(value); }
    }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlAttribute("Image")]
    public byte[] XML_Image
    {
      get { return _image; }
      set { _image = value; }
    }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlAttribute("AdditionalData")]
    public string XML_AdditionalData
    {
      get
      {
        List<Tuple<string, string, string>> convert = new List<Tuple<string, string, string>>();
        foreach (var key in _userData)
        {
          foreach (var val in key.Value)
          {
            convert.Add(new Tuple<string, string, string>(key.Key, val.Key.ToString(), val.Value));
          }
        }
        if (convert.Count > 0)
          return MarshallingHelper.SerializeTuple3EnumerationToCsv(convert);
        return null;
      }
      set
      {
        _userData = new Dictionary<string, IDictionary<int, string>>();
        IEnumerable<Tuple<string, string, string>> convert = MarshallingHelper.ParseCsvTuple3Collection(value);
        if (convert == null)
          return;

        foreach (var data in convert)
        {
          if (!_userData.ContainsKey(data.Item1))
            _userData.Add(data.Item1, new Dictionary<int, string>());
          _userData[data.Item1].Add(Convert.ToInt32(data.Item2), data.Item3);
        }
      }
    }

    #endregion
  }
}
