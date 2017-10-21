using System;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Serialization;
using MediaPortal.Plugins.MP2Extended.Authentication;
using MediaPortal.Plugins.SlimTv.Interfaces.UPnP.Items;

namespace MediaPortal.Plugins.MP2Extended.Settings
{
  /// <summary>
  /// This class holds one MP2Ext User.
  /// Also it takes care that everything is stored encrypted in the config file.
  /// Is this unnecessary? Yes! But it was fun to do so ;)
  /// </summary>
  [XmlRoot("MP2ExtendedUser")]
  public class MP2ExtendedUser
  {
    public MP2ExtendedUser()
    {
      Id = Guid.NewGuid();
    }
    
    private string _passwordEncrypted;
    private string _typeEncrypted;

    /// <summary>
    /// User id. This is generated automatically in the constructor.
    /// </summary>
    [XmlAttribute("Id")]
    public Guid Id { get; set; }

    /// <summary>
    /// Username
    /// </summary>
    [XmlAttribute("Name")]
    public string Name { get; set; }

    /// <summary>
    /// User password
    /// </summary>
    [XmlIgnore] // We don't want to have the plain PW in the config file
    public string Password { get; set; }

    /// <summary>
    /// Only for XML serialization! Don't use this!
    /// </summary>
    [XmlAttribute("PasswordEncrypted")]
    public string PasswordEncrypted
    {
      // also use the ID as key so that no one can see if two users have the same password
      get { return Encryption.Encrypt(Encryption.XorIt(Id.ToString(), MP2ExtendedUsers.KEY), Password); }
      set
      {
        _passwordEncrypted = value;
        Password = Encryption.Decrypt(Encryption.XorIt(Id.ToString(), MP2ExtendedUsers.KEY), value);
      }
    }

    // The order is important! We need the Password decrypted in order to process the Type
    // The Type is also encrypted with the user Password to ensure that nobody can copy&paste the Type from one account to another.

    /// <summary>
    /// User Type: Admin or normal user or ... ?
    /// </summary>
    [XmlIgnore] // We don't want to have the plain Type in the config file
    public UserTypes Type { get; set; }

    /// <summary>
    /// Only for XML serialization! Don't use this!
    /// </summary>
    [XmlAttribute("TypeEncrypted")]
    public string TypeEncrypted
    {
      // Use PasswordEncrypted as Key, because it should be unique
      get { return Encryption.Encrypt(Encryption.XorIt(PasswordEncrypted, MP2ExtendedUsers.KEY), Type.ToString()); }
      set
      {
        _typeEncrypted = value;
        Type = (UserTypes)Enum.Parse(typeof(UserTypes), Encryption.Decrypt(Encryption.XorIt(PasswordEncrypted, MP2ExtendedUsers.KEY), value));
      }
    }
  }
}
