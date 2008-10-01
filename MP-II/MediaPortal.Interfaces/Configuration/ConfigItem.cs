using System.Collections.Generic;
using MediaPortal.Presentation.Localisation;

namespace MediaPortal.Configuration
{
  public class ConfigItem : ConfigBase
  {

    #region Variables

    protected StringId _help;
    protected ICollection<string> _listenTo;
    protected int _columns;
    protected int _rows;
    protected object _settingsObject;

    #endregion

    #region Properties

    public StringId Help
    {
      get { return _help; }
      set { _help = value; }
    }

    public int Columns
    {
      get { return _columns; }
      set { _columns = value; }
    }

    public int Rows
    {
      get { return _rows; }
      set { _rows = value; }
    }

    public object SettingsObject
    {
      get { return _settingsObject; }
    }

    public ICollection<string> ListenTo
    {
      get { return _listenTo; }
    }

    #endregion

    #region Constructors

    protected ConfigItem()
      : base()
    {
      _listenTo = new List<string>();
    }

    public ConfigItem(string location, StringId text, StringId help, ICollection<string> listenTo)
      : base(location, text)
    {
      _help = help;
      _listenTo = listenTo;
    }

    public ConfigItem(string location, StringId text, StringId help, ICollection<string> listenTo, int columns, int rows)
      : this(location, text, help, listenTo)
    {
      _columns = columns;
      _rows = rows;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Loads the setting from the specified object.
    /// </summary>
    /// <param name="settingsObject">Object to extract setting from.</param>
    public virtual void Load(object settingsObject) { }

    /// <summary>
    /// Saves the setting.
    /// </summary>
    /// <param name="settingsObject">Object to save setting to.</param>
    public virtual void Save(object settingsObject) { }

    /// <summary>
    /// Applies the setting.
    /// </summary>
    public virtual void Apply() { }

    /// <summary>
    /// Registers an other instance of ConfigBase.
    /// The current object will notify the registered object on a change.
    /// </summary>
    /// <param name="other"></param>
    public void Register(ConfigItem other)
    {
      OnChangeEvent += other.ConfigChangedMainHandler;
    }

    #endregion

    #region Protected Methods

    /// <summary>
    /// Sets the type of object which can be used to extract settings from.
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    protected void SetSettingsObject(object obj)
    {
      _settingsObject = obj;
    }

    #endregion

  }
}
