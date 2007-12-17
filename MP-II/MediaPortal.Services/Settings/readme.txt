In order to use the SettingsManager service , 
you first need to create you custom settings class wich could look like this:

( note that this sample settings class is in svn already, under ProjectInfinity.Services/Settings/MySampleSettingsClass.cs )
using System;
using System.Collections.Generic;
using System.Text;

namespace ProjectInfinity.Settings
{
  /// <summary>
  /// Sample settings class wich will implement your own settings object in your code/plugin
  /// Only public properties are stored/retrieved
  /// </summary>
  class MySampleSettingsClass 
  {
    private int _myInt;
    private string _myString;
    private string _anotherString;
    private List<int> _alist = new List<int>();

    /// <summary>
    /// Default Ctor
    /// </summary>
    public MySampleSettingsClass()
    {
    }
    /// <summary>
    /// Scope and default value attribute
    /// </summary>
    [Setting(SettingScope.Global,"55555")]
    public int MyInt
    {
      get { return this._myInt; }
      set { this._myInt = value; }
    }
    [Setting(SettingScope.User,"myStringDefaultValue")]
    public string MyString
    {
      get { return this._myString; }
      set { this._myString = value; }
    }
    [Setting(SettingScope.User, "anotherStringDefaultValue")]
    public string AnotherString
    {
      get { return this._anotherString; }
      set { this._anotherString = value; }
    }
    [Setting(SettingScope.User, "")]
    public List<int> AList
    {
      get { return this._alist; }
      set { this._alist = value; }
    }
  }
}

------------------------------------------------------------------------------------

- Only public properties will be parsed
- Those properties must have a get and set method

- You need to specify a setting scope and default value , using Setting attribute:

	[Setting(SettingScope.User,"myStringDefaultValue")]
	Possible SettingScope values are:
		* SettingScope.User, if you want a per user storage/retrieval for this property
		* SettingScope.global if the property is a Global one and does't allow per user storage

When your custom settings class is done , you can use it this way :

// first use the ServiceScope manager to get the SettingsManager running instance :
ISettingsManager mgr = ServiceScope.Get<ISettingsManager>();

// Instanciate a settings object (based on your own class)
MySampleSettingsClass mySettings = new MySampleSettingsClass();

// Load existing or default values
string fileName = "full path xml filename, can be an existing xml file, used by others processes";

// retrieve stored data
mgr.Load(mySettings, fileName);
...
Your code
// get a setting/property  value using your object's public properties
int myInt = mySetting.MyInt;
// modify it
mySetting.MyInt = 2007;

...

//When needed , save the modified mySettings object
mgr.Save(mySettings, fileName);


 