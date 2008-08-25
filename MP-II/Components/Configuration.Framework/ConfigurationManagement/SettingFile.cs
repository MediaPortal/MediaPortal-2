using System;
using System.Collections.Generic;

using MediaPortal.Core;
using MediaPortal.Core.Settings;


namespace MediaPortal.Configuration
{

  /// <summary>
  /// SettingFile groups all configuration items which are linked to the same settingsclass,
  /// and lets them share one instance of that class.
  /// </summary>
  internal class SettingFile : IEquatable<SettingFile>
  {

    #region Variables

    /// <summary>
    /// Shared instance of the mutual settingsclass.
    /// </summary>
    private object _settingObject;
    /// <summary>
    /// All nodes linked to the same settingsclass.
    /// </summary>
    private IList<IConfigurationNode> _linkedNodes;

    #endregion

    #region Properties

    /// <summary>
    /// Gets the shared instance of he mutual settingsclass.
    /// </summary>
    public object SettingObject
    {
      get { return _settingObject; }
    }

    /// <summary>
    /// Gets all instances of IConfigurationNode which are linked to the same settingsclass.
    /// </summary>
    public IList<IConfigurationNode> LinkedNodes
    {
      get { return _linkedNodes; }
    }

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of SettingFile.
    /// </summary>
    /// <param name="settingObject"></param>
    public SettingFile(object settingObject)
    {
      _settingObject = settingObject;
      _linkedNodes = new List<IConfigurationNode>();
    }

    /// <summary>
    /// Initializes a new instance of SettingFile.
    /// </summary>
    /// <exception cref="ArgumentNullException">
    /// An ArgumentNullException is thrown when the linkedNodes parameter is null.
    /// </exception>
    /// <param name="settingObject"></param>
    /// <param name="linkedNodes"></param>
    public SettingFile(object settingObject, IList<IConfigurationNode> linkedNodes)
    {
      if (linkedNodes == null)
        throw new ArgumentNullException("The argument linkedNodes can't be null.");
      _settingObject = settingObject;
      _linkedNodes = linkedNodes;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Saves the file.
    /// </summary>
    public void Save()
    {
      lock (_linkedNodes)
      {
        foreach (IConfigurationNode node in _linkedNodes)
          node.Setting.Save(_settingObject);
      }
      if (_settingObject != null)
        ServiceScope.Get<ISettingsManager>().Save(_settingObject);
    }

    #endregion

    #region IEquatable<SettingFile> Members

    /// <summary>
    /// Determines whether the specified SettingFile object is equal to the current SettingFile object.
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public bool Equals(SettingFile other)
    {
      if (((other.SettingObject == null && _settingObject == null) || other._settingObject + "" == _settingObject + "")
          && other._linkedNodes.Count == _linkedNodes.Count)
      {
        for (int i = 0; i < _linkedNodes.Count; i++)
        {
          if (!_linkedNodes[i].Equals(other._linkedNodes[i]))
            return false;
        }
        return true;
      }
      return false;
    }

    #endregion

  }
}
