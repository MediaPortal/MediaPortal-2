#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Xml.XPath;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Input;
using MP2_PluginWizard.Model;
using MP2_PluginWizard.Utils;
using MP2_PluginWizard.Settings;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Settings;


namespace MP2_PluginWizard.ViewModel
{
	/// <summary>
	/// Description of PluginDataViewModel.
	/// </summary>
	public class PluginDataViewModel : ViewModelBase
	{
		
    #region const fields
    public const string PLUGIN_META_FILE = "plugin.xml";

    public const int PLUGIN_DESCRIPTOR_VERSION_MAJOR = 1;
    public const int MIN_PLUGIN_DESCRIPTOR_VERSION_MINOR = 0;

    #endregion
    		
		#region Private/protected fields
    private string _pathName;
    private string _descriptorVersion;
    private string _name;
    private Guid _pluginId;
    private string _copyright;
    private string _author;
    private string _description;
    private string _pluginVersion;
    private bool _autoActivate;
    private string _stateTrackerClassName;

    //WelcomePage
	  private bool _mp2PluginsAvailable;

    //BasicPluginDataPage
    private string _newRuntime;
    private RelayCommand _addRuntimeCommand;
    private RelayCommand _removeRuntimeCommand;
    
    //DependsConflictsBuilderPage
    private string _newBuilderName;
    private string _newBuilderClassName;
    private PluginNameId _dependsOnSelectedItem;
    private PluginNameId _conflictsWithSelectedItem;

    private RelayCommand _addDependsOnCommand;
    private RelayCommand _removeDependsOnCommand;

    private RelayCommand _addConflictsWithCommand;
    private RelayCommand _removeConflictsWithCommand;
    
    private RelayCommand _addBuildersCommand;
    private RelayCommand _removeBuildersCommand;
    
    // RegisterPage
		private string _newRegisterLocation;
	  private int _registerSelectedIndex;
		private string _newRegisterItemName;
    private string _newRegisterItemId;
    private bool _newRegisterItemRedundant;
    private int _registerItemSelectedIndex;
    private string _newRegisterAttributeName;
    private string _newRegisterAttributeValue;
    
    private RelayCommand _addRegisterLocationCommand;
    private RelayCommand _removeRegisterLocationCommand;
    
    private RelayCommand _addRegisterItemCommand;
    private RelayCommand _removeRegisterItemCommand;
    
    private RelayCommand _addRegisterAttributeCommand;
    private RelayCommand _removeRegisterAttributeCommand;
        
    // ResultPage
    private string _pluginText;
    
    #endregion

		#region Ctor/Dtor
		public PluginDataViewModel()
		{
			// Init plugin.xml data
			DependsOn = new ObservableCollection<PluginNameId>();
      ConflictsWith = new ObservableCollection<PluginNameId>();
      Builders = new ObservableCollection<PluginBuilder>();
      Registers = new ObservableCollection<PluginRegister>();
      AssemblyFiles = new ObservableCollection<string>();
      	
      // Init WelcomePage
      PluginPaths = new ObservableCollection<string>();
		  IsPluginOnly = true;
      
      // Init BasicPluginDataPage
          
      
      // Init DependsConflictsBuilderPage
      PluginList = new SortableObservableCollection<PluginNameId>();
      BuilderList = new ObservableCollection<string>();
      
      // Init RegisterPage
      RegisterLocations = new ObservableCollection<string>();
     
      // Init ResultPage
     
      
      // Load default values
      DescriptorVersion = "1.0";
      Copyright = "GPL";
      PluginVersion = "1.0";
      PluginId = Guid.NewGuid();
      
      ServiceRegistration.Get<ILogger>().Debug("Loading settings");
      var settingsManager = ServiceRegistration.Get<ISettingsManager>();
			var settings = settingsManager.Load<PluginsSettings>();
			var settingsWizard = settingsManager.Load<WizardSettings>();
      
      //Init Lists
      if ((settingsWizard.PluginPaths != null) && (settingsWizard.PluginPaths.Count > 0))
      	foreach(var path in settingsWizard.PluginPaths)
      		PluginPaths.Add(path);

      if ((settings.Plugins != null) && (settings.Plugins.Count > 0))
      {
        Mp2PluginsAvailable = true;
        foreach (var plugin in settings.Plugins)
          PluginList.Add(new PluginNameId(plugin.Name, plugin.Id));
        PluginList.Sort(x => x.Name, ListSortDirection.Ascending);
      }
      else
        Mp2PluginsAvailable = false;
      
      if (settingsWizard.Builders != null)
      	foreach (var builder in settingsWizard.Builders)
      		BuilderList.Add(builder);
  
      PluginPaths.CollectionChanged += OnPluginPathsChanged;
      BuilderList.CollectionChanged += OnBuilderListChanged;     
		}
		
		#endregion
		
		#region plugin.xml 
		
		#region Public properties
    /// <summary>
    /// Returns the path to the plugin.
    /// </summary>
    public string PathName
    {
      get { return _pathName; }
      set { SetProperty(ref _pathName, value, "PathName"); }
    }
    
    /// <summary>
    /// Returns the Version of the descriptor.
    /// </summary>
    public string DescriptorVersion
    {
      get { return _descriptorVersion; }
      set { SetProperty(ref _descriptorVersion, value, "DescriptorVersion"); }
    }
    
    /// <summary>
    /// Returns the plugin's name.
    /// </summary>
    public string Name 
    {
      get { return _name; }
      set { SetProperty(ref _name, value, "Name"); }
    }

    /// <summary>
    /// Returns the plugin's unique id.
    /// </summary>
    public Guid PluginId
    {
      get { return _pluginId; }
      set { SetProperty(ref _pluginId, value, "PluginId"); }
    }

    /// <summary>
    /// Returns the plugin's copyright statement.
    /// </summary>
    public string Copyright
    {
      get { return _copyright; }
      set { SetProperty(ref _copyright, value, "Copyright"); }
    }

    /// <summary>
    /// Returns the plugin's author.
    /// </summary>
    public string Author
    {
      get { return _author; }
      set 
      { 
      	SetProperty(ref _author, value, "Author"); 
      	
      	if (string.IsNullOrEmpty(value)) return;
      	var settingsManager = ServiceRegistration.Get<ISettingsManager>();
      	var settings = settingsManager.Load<WizardSettings>();
				settings.Author = value;
				settingsManager.Save(settings);
      }
    }

    /// <summary>
    /// Returns a short description of the plugins function.
    /// </summary>
    public string Description
    {
      get { return _description; }
      set { SetProperty(ref _description, value, "Description"); }
    }

    /// <summary>
    /// Returns the plugin's version.
    /// </summary>
    public string PluginVersion
    {
      get { return _pluginVersion; }
      set { SetProperty(ref _pluginVersion, value, "PluginVersion"); }
    }

    /// <summary>
    /// Gets the information if this plugin should be automatically activated when enabled.
    /// </summary>
    public bool AutoActivate
    {
      get { return _autoActivate; }
      set { SetProperty(ref _autoActivate, value, "AutoActivate"); }
    }

    /// <summary>
    /// Returns the class name of the StateTracker.
    /// </summary>
    public string StateTrackerClassName
    {
      get { return _stateTrackerClassName; }
      set { SetProperty(ref _stateTrackerClassName, value, "StateTrackerClassName"); }
    }

    /// <summary>
    /// Returns the Runtime Assembly files of this plugin
    /// </summary>
    public ObservableCollection<string> AssemblyFiles { get; private set; }
        
    /// <summary>
    /// Returns a collection of names and id's of plugins, this plugin depends on.
    /// </summary>
    public ObservableCollection<PluginNameId> DependsOn { get; private set; }

    /// <summary>
    /// Returns a collection of names and Id's of plugins, this plugin stands in conflict with.
    /// </summary>
    public ObservableCollection<PluginNameId> ConflictsWith { get; private set; }

    /// <summary>
    /// Gets all builders defined by this plugin. 
    /// </summary>
    public ObservableCollection<PluginBuilder> Builders { get; private set; }

    /// <summary>
    /// Gets all Registers defined in one plugin
    /// </summary>
    public ObservableCollection<PluginRegister> Registers { get; private set; }
    
    // for Project file creation
    public string AssemblyId { get; set; }
    public string ProjectId { get; set; }
    public string ShortName { get; set; }
    public string ProjectSourcePath { get; set; }
    
    
    #endregion
    

    public void Reset()
    {
      var settings = ServiceRegistration.Get<ISettingsManager>().Load<WizardSettings>();
      // Load default values
      DescriptorVersion = "1.0";
      Copyright = "GPL";
      Description = string.Empty;
   
      PluginVersion = "1.0";
      PluginId = Guid.NewGuid();
      Author = settings.Author;
      StateTrackerClassName = string.Empty;
      AutoActivate = false;

      AssemblyFiles.Clear();
      DependsOn.Clear();
      ConflictsWith.Clear();
      Builders.Clear();
      Registers.Clear();
      SetDefaultName();
    }

    public void SetDefaultName()
    {
      if (!string.IsNullOrEmpty(Name) && !Name.Equals("MetadataExtractor") && !Name.Equals("ResourceProvider")) return;

      Name = "";
      if (IsPluginMeta) Name = "MetadataExtractor";
      else if (IsPluginRes) Name = "ResourceProvider";
    }

    
    #region Load
    /// <summary>
    /// Loads the plugin descriptor file (plugin.xml) from a plugin directory.
    /// </summary>
    /// <param name="pluginDirectoryPath">Root directory path of the plugin to load the metadata.</param>
    /// <returns><c>true</c>, if the plugin descriptor could successfully be loaded, else <c>false</c>.
    /// </returns>
    public bool Load(string pluginDirectoryPath)
    {
      string path = Path.Combine(pluginDirectoryPath, PLUGIN_META_FILE);
      if (!File.Exists(path))
        return false;
      
      try
      {
        using (Stream pluginFileStream = File.OpenRead(path))
        {
          var doc = new XPathDocument(pluginFileStream);
          var nav = doc.CreateNavigator();
          nav.MoveToChild(XPathNodeType.Element);
          if (nav.LocalName != "Plugin")
            throw new ArgumentException("File is no plugin descriptor file (document element must be 'Plugin')");

          var versionOk = false;
          var pluginIdSet = false;
          XPathNavigator attrNav = nav.Clone();
          if (attrNav.MoveToFirstAttribute())
            do
            {
              switch (attrNav.Name)
              {
                case "DescriptorVersion":
                  CheckVersionCompatible(attrNav.Value, PLUGIN_DESCRIPTOR_VERSION_MAJOR, MIN_PLUGIN_DESCRIPTOR_VERSION_MINOR);
                  DescriptorVersion = attrNav.Value; 
                  versionOk = true;
                  break;
                case "Name":
                  Name = attrNav.Value;
                  break;
                case "PluginId":
                  PluginId = new Guid(attrNav.Value);
                  pluginIdSet = true;
                  break;
                case "Author":
                  Author = attrNav.Value;
                  break;
                case "Copyright":
                  Copyright = attrNav.Value;
                  break;
                case "Description":
                  Description = attrNav.Value;
                  break;
                case "PluginVersion":
                  PluginVersion = attrNav.Value;
                  break;
                case "AutoActivate":
                  AutoActivate = Boolean.Parse(attrNav.Value);
                  break;
                default:
                  throw new ArgumentException("'Plugin' element doesn't support an attribute '" + attrNav.Name + "'");
              }
            } while (attrNav.MoveToNextAttribute());
          if (!versionOk)
            throw new ArgumentException("'Version' attribute not found");

          if (!pluginIdSet)
            throw new ArgumentException("'PluginId' attribute not found");

          XPathNavigator childNav = nav.Clone();
          if (childNav.MoveToChild(XPathNodeType.Element))
            do
            {
              switch (childNav.LocalName)
              {
                case "Runtime":
                  ParseRuntimeElement(childNav.Clone(), pluginDirectoryPath);
                  break;
                case "Builder":
                  Builders.Add(ParseBuilderElement(childNav.Clone()));
                  break;
                case "Register":
                    var register = ParseRegisterElement(childNav.Clone());
                    Registers.Add(register);
                  break;
                case "DependsOn":
                  foreach (var guid in ParsePluginIdEnumeration(childNav.Clone()))
                  	DependsOn.Add(new PluginNameId(GetPluginName(guid), guid));
                  break;
                case "ConflictsWith":
                  foreach (var guid in ParsePluginIdEnumeration(childNav.Clone()))
                    ConflictsWith.Add(new PluginNameId(GetPluginName(guid), guid));
                  break;
                default:
                  throw new ArgumentException("'Plugin' element doesn't support a child element '" + childNav.Name + "'");
              }
            } while (childNav.MoveToNext(XPathNodeType.Element));
        }
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("Error parsing plugin descriptor file [" + path + "]", e);
        return false;
      }
      if (Registers.Count > 0)
      {
        // Select the first element to also show RegisterItems and RegisterAttributes right from the beginning
        RegisterSelectedIndex = 0; 
      }
      return true;
    }

    #endregion

    #region Parsing methods

    /// <summary>
    /// Processes the <i>Runtime</i> sub element of the <i>Plugin</i> element.
    /// </summary>
    /// <param name="runtimeNavigator">XPath navigator pointing to the <c>Runtime</c> element.</param>
    /// <param name="pluginDirectory">Root directory path of the plugin whose metadata is to be parsed.</param>
    protected void ParseRuntimeElement(XPathNavigator runtimeNavigator, string pluginDirectory)
    {
      if (runtimeNavigator.HasAttributes)
        throw new ArgumentException("'Runtime' element mustn't contain any attributes");
      XPathNavigator childNav = runtimeNavigator.Clone();
      if (childNav.MoveToChild(XPathNodeType.Element))
        do
        {
          switch (childNav.LocalName)
          {
            case "Assembly":
              string fileName = childNav.GetAttribute("FileName", string.Empty);
              if (string.IsNullOrEmpty(fileName))
                throw new ArgumentException("'Assembly' element needs an attribute 'FileName'");
              AssemblyFiles.Add(fileName);
              break;
            case "PluginStateTracker":
              StateTrackerClassName = childNav.GetAttribute("ClassName", string.Empty);
              if (StateTrackerClassName.Length == 0)
                throw new ArgumentException("'PluginStateTracker' element needs an attribute 'ClassName'");
              break;
            default:
              throw new ArgumentException("'Runtime' element doesn't support a child element '" + childNav.Name + "'");
          }
        } while (childNav.MoveToNext(XPathNodeType.Element));
    }

    /// <summary>
    /// Processes the <i>Builder</i> sub element of the <i>Plugin</i> element.
    /// </summary>
    /// <param name="builderNavigator">XPath navigator pointing to the <c>Builder</c> element.</param>
    /// <returns>Parsed builder - name to classname mapping.</returns>
    protected static PluginBuilder ParseBuilderElement(XPathNavigator builderNavigator)
    {
      string name = null;
      string className = null;
      XPathNavigator attrNav = builderNavigator.Clone();
      if (attrNav.MoveToFirstAttribute())
        do
        {
          switch (attrNav.Name)
          {
            case "Name":
              name = attrNav.Value;
              break;
            case "ClassName":
              className = attrNav.Value;
              break;
            default:
              throw new ArgumentException("'Builder' element doesn't support an attribute '" + attrNav.Name + "'");
          }
        } while (attrNav.MoveToNextAttribute());
      if (name == null)
        throw new ArgumentException("'Builder' element needs an attribute 'Name'");
      if (className == null)
        throw new ArgumentException("'Builder' element needs an attribute 'ClassName'");
      if (builderNavigator.SelectChildren(XPathNodeType.Element).Count > 0)
        throw new ArgumentException("'Builder' element doesn't support child nodes");
      return new PluginBuilder(name, className);
    }

    /// <summary>
    /// Processes the <i>Register</i> sub element of the <i>Plugin</i> element.
    /// </summary>
    /// <param name="registerNavigator">XPath navigator pointing to the <c>Register</c> element.</param>
    /// <returns>Metadata structures of all registered items in the given element.</returns>
    protected static PluginRegister ParseRegisterElement(XPathNavigator registerNavigator)
    {
      string location = null;
      XPathNavigator attrNav = registerNavigator.Clone();
      if (attrNav.MoveToFirstAttribute())
      do
        {
          switch (attrNav.Name)
          {
            case "Location":
              location = attrNav.Value;
              break;
            default:
              throw new ArgumentException("'Register' element doesn't support an attribute '" + attrNav.Name + "'");
          }
        } while (attrNav.MoveToNextAttribute());
      if (location == null)
        throw new ArgumentException("'Register' element needs an attribute 'Location'");
      
      var register = new PluginRegister(location);
      
      XPathNavigator childNav = registerNavigator.Clone();
      if (childNav.MoveToChild(XPathNodeType.Element))
        do
        {
          string id = null;
          var redundant = false;
          var attributes = new List<PluginRegisterAttribute>();
          var builderName = childNav.LocalName;
          attrNav = childNav.Clone();
          if (attrNav.MoveToFirstAttribute())
            do
            {
              switch (attrNav.Name)
              {
                case "Id":
                  id = attrNav.Value;
                  break;
                case "Redundant":
                  redundant = bool.Parse(attrNav.Value);
                  break;
                default:
                  attributes.Add(new PluginRegisterAttribute(attrNav.Name, attrNav.Value));
                  break;
              }
            } while (attrNav.MoveToNextAttribute());
          if (id == null)
            throw new ArgumentException("'Id' attribute has to be given for plugin item '" + childNav.Name + "'");
          register.Items.Add(new PluginRegisterItem(builderName, id, redundant, attributes));
        } while (childNav.MoveToNext(XPathNodeType.Element));
      return register;
    }

    /// <summary>
    /// Processes an element containing a collection of <i>&lt;PluginReference PluginId="..."/&gt;</i> sub elements and
    /// returns an enumeration of the referenced ids.
    /// </summary>
    /// <param name="enumNavigator">XPath navigator pointing to an element containing the &lt;PluginReference PluginId="..."/&gt;
    /// sub elements.</param>
    /// <returns>Enumeration of parsed plugin ids.</returns>
    protected static IEnumerable<Guid> ParsePluginIdEnumeration(XPathNavigator enumNavigator)
    {
      if (enumNavigator.HasAttributes)
        throw new ArgumentException(string.Format("'{0}' element mustn't contain any attributes", enumNavigator.Name));
      XPathNavigator childNav = enumNavigator.Clone();
      if (childNav.MoveToChild(XPathNodeType.Element))
        do
        {
          switch (childNav.LocalName)
          {
            case "PluginReference":
              string id = null;
              XPathNavigator attrNav = childNav.Clone();
              if (attrNav.MoveToFirstAttribute())
                do
                {
                  switch (attrNav.Name)
                  {
                    case "PluginId":
                      id = attrNav.Value;
                      break;
                    default:
                      throw new ArgumentException("'PluginReference' sub element doesn't support an attribute '" + attrNav.Name + "'");
                  }
                } while (attrNav.MoveToNextAttribute());
              if (id == null)
                throw new ArgumentException("'PluginReference' sub element needs an attribute 'PluginId'");
              yield return new Guid(id);
              break;
            default:
              throw new ArgumentException("'" + enumNavigator.Name + "' element doesn't support a child element '" + childNav.Name + "'");
          }
        } while (childNav.MoveToNext(XPathNodeType.Element));
    }


    /// <summary>
    /// Tries to parse the specified version string in the format <c>#.#</c> or <c>#</c>, where # stands
    /// for an int number.
    /// </summary>
    /// <param name="versionStr">The string to parse. This string should be in the format
    /// <c>#.#</c> or <c>#</c>.</param>
    /// <param name="verMajor">Returns the major version number.</param>
    /// <param name="verMinor">Returns the minor version number, if the string contains both. Else, this
    /// parameter will return <c>0</c>.</param>
    /// <returns><c>true</c>, if the version string could correctly be parsed, else <c>false</c>.</returns>
    public static bool TryParseVersionString(string versionStr, out int verMajor, out int verMinor)
    {
      string[] numbers = versionStr.Split(new[] { '.' });
      verMinor = 0;
      verMajor = 0;
      if (numbers.Length < 1 || numbers.Length > 2)
        return false;
      if (!Int32.TryParse(numbers[0], out verMajor))
        return false;
      if (numbers.Length > 1)
        if (!Int32.TryParse(numbers[0], out verMinor))
          return false;
      return true;
    }

    /// <summary>
    /// Helper method to check the given version string to be compatible to the specified version number.
    /// A compatible version has the same major version number (<paramref name="expectedMajor"/>) and an
    /// equal or greater minor version number (<paramref name="minExpectedMinor"/>).
    /// </summary>
    /// <param name="versionStr">Version string to check against the expected values.</param>
    /// <param name="expectedMajor">Expected major version. The major version number of the given
    /// <paramref name="versionStr"/> must match exactly with the <paramref name="expectedMajor"/>
    /// version.</param>
    /// <param name="minExpectedMinor">Expected minor version. The minor version number of the given
    /// <paramref name="versionStr"/> must match this parameter or be greater than it.</param>
    public static void CheckVersionCompatible(string versionStr, int expectedMajor, int minExpectedMinor)
    {
      int verHigh;
      int verLow;
      if (!TryParseVersionString(versionStr, out verHigh, out verLow))
        throw new ArgumentException("Illegal version number '" + versionStr + "', expected format: '#.#'");
      if (verHigh == expectedMajor && verLow >= minExpectedMinor)
        return;
      throw new ArgumentException("Version number '" + versionStr +
          "' is not compatible with expected version number '" + expectedMajor + "." + minExpectedMinor + "'");
    }



    #endregion

    #region Save
    public bool Save()
    {
    	SavePluginFile(PluginPathName);
    	
    	if (!IsPluginOnly) {
    		AssemblyId = Guid.NewGuid().ToString().ToUpper();
    	  ProjectId = Guid.NewGuid().ToString().ToUpper();
    	  ShortName = ProjectSourcePath = string.Empty;
    		    		
        var applicationPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        if (applicationPath != null)
        {
          if (IsPluginMeta) 
          {
          	ProjectSourcePath = Path.Combine(applicationPath, @"Defaults\MetadataExtractor");
          	ShortName = Name.Replace("MetadataExtractor", "");
          }
          else if (IsPluginRes) 
          { 
          	ProjectSourcePath = Path.Combine(applicationPath, @"Defaults\ResourceProviderWithOutProxy");
          	ShortName = Name.Replace("ResourceProvider", "");
          }
          else if (IsPluginGui)
          {
          	ProjectSourcePath = Path.Combine(applicationPath, @"Defaults\GuiPlugin");
          }
          
          SaveProjectData();
        }
      }
    	return true;
    }
    
    
    /// <summary>
    /// Saves the plugin file (plugin.xml) to the pluginDirectoryPath.
    /// </summary>
    /// <param name="pluginDirectoryPath">Root directory path of the plugin to save the metadata to.</param>
    /// <returns><c>true</c>, if the plugin file could successfully be saved, else <c>false</c>.</returns>
    /// <remarks>No XMLdocument class was used to have the possibillity to place comments in the XML file</remarks>
    public bool SavePluginFile(string pluginDirectoryPath)
    {
    	StreamWriter writer;
    	try
    	{
    		var pluginFile = Path.Combine(pluginDirectoryPath, PLUGIN_META_FILE);
      	if (File.Exists(pluginFile))
      	{
        	var bakFile = pluginFile + ".bak";
        	if (File.Exists(bakFile))
        	  File.Delete(bakFile);
        	File.Move(pluginFile, bakFile);
      	}
      	writer = new StreamWriter(pluginFile);
    	}
    	catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("Error saving plugin file [" + pluginDirectoryPath + "]", e);
        return false;
      }
      return Save(writer);
    }
    
    /// <summary>
    /// Saves the plugin file (plugin.xml) to the stream.
    /// </summary>
    /// <param name="writer">stream writer to save the data to</param>
    /// <returns><c>true</c>, if the plugin file could successfully be written, else <c>false</c>.</returns>
    public bool Save(TextWriter writer)
    {
      try
      {
        // == Plugin ==
        writer.WriteLine("<Plugin");
        writer.WriteLine("    DescriptorVersion=\"{0}\"", DescriptorVersion);
				writer.WriteLine("    Name=\"{0}\"", Name);
				writer.WriteLine("    PluginId=\"{{{0}}}\"", PluginId.ToString().ToUpper());
        writer.WriteLine("    Author=\"{0}\"", Author);
        writer.WriteLine("    Copyright=\"{0}\"", Copyright);
        writer.WriteLine("    Description=\"{0}\"", Description);
        writer.WriteLine("    PluginVersion=\"{0}\"", PluginVersion);
        if (AutoActivate)
          writer.WriteLine("    AutoActivate=\"true\"");        	
        writer.WriteLine("    >");
        writer.WriteLine("");
       
				// == Runtime ==
				if ((AssemblyFiles.Count > 0) || (!string.IsNullOrEmpty(StateTrackerClassName)))
				{
					writer.WriteLine("<Runtime>");
      		foreach (var filePath in AssemblyFiles)
      		{
        		writer.WriteLine("  <Assembly FileName=\"{0}\"/>", filePath);
          }
      		if (!string.IsNullOrEmpty(StateTrackerClassName))
      		{
      			writer.WriteLine("  <PluginStateTracker ClassName=\"{0}\"/>", StateTrackerClassName);
      		}
      		writer.WriteLine("</Runtime>");
      		writer.WriteLine("");
				}
				
        // == DependsOn ==
      	if (DependsOn.Count > 0)
      	{
      		writer.WriteLine("<DependsOn>");
      		foreach (var nameId in DependsOn)
      		{
      			writer.WriteLine("  <PluginReference PluginId=\"{{{0}}}\"/>  <!-- {1} -->", 
      			                 nameId.Id.ToString().ToUpper(), nameId.Name);
          }
      		writer.WriteLine("</DependsOn>");
      		writer.WriteLine("");
      	}
        
      	// == ConflictsWith	==
      	if (ConflictsWith.Count > 0)
      	{
      		writer.WriteLine("<ConflictsWith>");
      		foreach (var nameId in ConflictsWith)
      		{
      			writer.WriteLine("  <PluginReference PluginId=\"{{{0}}}\"/>  <!-- {1} -->", 
      			                 nameId.Id.ToString().ToUpper(), nameId.Name);
          }
      		writer.WriteLine("</ConflictsWith>");
      		writer.WriteLine("");
      	}
        
      	// == Builder ==
      	if (Builders.Count > 0)
      	{
      		foreach(var entry in Builders)
      		{
        		writer.WriteLine("<Builder Name=\"{0}\" ClassName=\"{1}\"/>", entry.Name, entry.ClassName);
          }
      		writer.WriteLine("");
      	}
      	
      	// == Register ==
      	if (Registers.Count > 0)
      	{
      		foreach (var data in Registers)
      		{
      			writer.WriteLine("<Register Location=\"{0}\">", data.Location);
      			foreach (var item in data.Items)
      			{
      				writer.Write("  <{0} Id=\"{1}\"", item.BuilderName, item.Id);
      				if (item.IsRedundant)
      				{
      					writer.Write(" Redundant=\"true\"");
      				}
      				foreach (var attr in item.Attributes)
      				{
      					writer.WriteLine("");
      					writer.Write("      {0}=\"{1}\"", attr.Name, attr.Value);
      				}
      				writer.WriteLine("/>");
      			}
      			writer.WriteLine("</Register>");
      			writer.WriteLine("");
      		}
      	}
      	
        writer.WriteLine("</Plugin>");
        writer.Flush();
        writer.Close();
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("Error saving plugin file", e);
        return false;
      }
      return true;
    }
    
    public bool SaveProjectData()
    {
    	if (!Directory.Exists(ProjectSourcePath) || !Directory.Exists(PluginPathName)) return false;
         	
      try
      {
        var stack = new Stack<string>();
        stack.Push(ProjectSourcePath);

        while (stack.Count > 0)
        {
          // Pop a directory
          var dir = stack.Pop();

          var files = Directory.GetFiles(dir);
          foreach (var file in files)
            ProcessAndCopyFile(file);

          var directories = Directory.GetDirectories(dir);
          foreach (var directory in directories)
            stack.Push(directory);
        }
      }
      catch (Exception ex)
      {
        return false;
      }
      return true;
    }
    
    private void ProcessAndCopyFile(string filePath)
    {
      var fileContents = File.ReadAllText(filePath);

      fileContents = fileContents.Replace("xxPluginName", Name);
      fileContents = fileContents.Replace("xxShortName", ShortName);
      fileContents = fileContents.Replace("xxPluginDescription", Description);
      fileContents = fileContents.Replace("xxPluginId", PluginId.ToString().ToUpper());
      fileContents = fileContents.Replace("xxCurrentYear", DateTime.Now.Year.ToString());
      fileContents = fileContents.Replace("xxAssemblyId", AssemblyId);
      fileContents = fileContents.Replace("xxProjectId", ProjectId);
      
      var destFilePath = filePath.Replace(ProjectSourcePath, PluginPathName);
      var destPath = Path.GetDirectoryName(destFilePath);
      destFilePath = destFilePath.Replace("xxPluginName", Name);
      destFilePath = destFilePath.Replace("xxShortName", ShortName);
      
      if (destPath != null && !Directory.Exists(destPath))
        Directory.CreateDirectory(destPath);

      if (File.Exists(destFilePath))
        File.Delete(destFilePath);
      File.WriteAllText(destFilePath, fileContents);
    }
    
    
  
    #endregion
    
    public string GetPluginName(Guid guid)
    {
    	foreach (var plugin in PluginList)
    	{
    		if (plugin.Id.Equals(guid))
    			return plugin.Name; 
    	}
    	return "[no data]";
    }

    
    
    #endregion
    
		#region WelcomePage
		
		#region Private/protected fields
    private string _pluginPathName;
    private bool _isPluginOnly;
    private bool _isPluginMeta;
    private bool _isPluginRes;
    private bool _isPluginGui;
   
    #endregion
       
    #region Public properties

    public SortableObservableCollection<PluginNameId> PluginList { get; private set; }

	  public bool Mp2PluginsAvailable
	  {
	    get { return _mp2PluginsAvailable; }
      set
      {
        _mp2PluginsAvailable = value;
        OnPropertyChanged("Mp2PluginsAvailable");
      }
	  }

    public string PluginPathName
    {
      get { return _pluginPathName; }
      set { SetProperty(ref _pluginPathName, value, "PluginPathName"); }
    }

    public string NewPluginPathName
    {
      set
      {
        if (PluginPathName != null)
          return;
        
        if (string.IsNullOrEmpty(value)) return;
        
        PluginPaths.Add(value);
        PluginPathName = value;
      }
    }

    public ObservableCollection<string> PluginPaths { get; private set; }
    
	  public bool IsPluginOnly
	  {
	    get { return _isPluginOnly; }
      set { SetProperty(ref _isPluginOnly, value, "IsPluginOnly"); SetDefaultName(); }
	  }

	  public bool IsPluginMeta
	  {
	    get { return _isPluginMeta; }
      set { SetProperty(ref _isPluginMeta, value, "IsPluginMeta"); SetDefaultName(); }
	  }

	  public bool IsPluginRes
	  {
	    get { return _isPluginRes; }
      set { SetProperty(ref _isPluginRes, value, "IsPluginRes"); SetDefaultName(); }
	  }

	  public bool IsPluginGui
	  {
	    get { return _isPluginGui; }
      set { SetProperty(ref _isPluginGui, value, "IsPluginGui"); SetDefaultName(); }
	  }

	  #endregion
  
    #region Private Methods
    private void OnPluginPathsChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
    	// user can only add therefore we only store on Add
    	if (e.Action != NotifyCollectionChangedAction.Add) return;
    	if (e.NewItems == null) return;
    	
    	ServiceRegistration.Get<ILogger>().Debug("Updating PluginPaths");
      var settingsManager = ServiceRegistration.Get<ISettingsManager>();
    	var settings = settingsManager.Load<WizardSettings>();
    	if (settings.PluginPaths == null)
    		settings.PluginPaths = new List<string>();
    	foreach (var item in e.NewItems)
    	{
    		var path = (string)item;
    		if (!string.IsNullOrEmpty(path))
    			settings.PluginPaths.Add(path);
    	}
    	settingsManager.Save(settings);
    }
    
    #endregion
    
    #region Public Methods
    public void AddNewPluginPathName(string newPathName)
    {
      // always insert/move the Mp2PathName to the top position in the list
      if (PluginPaths.Contains(newPathName))
        PluginPaths.Remove(newPathName);
      PluginPaths.Insert(0, newPathName);

      // keep only the last 10 PathNames in the list
      while (PluginPaths.Count > 10)
        PluginPaths.RemoveAt(PluginPaths.Count - 1);

      PluginPathName = newPathName;
    }
    
    #endregion
        
		#endregion
			
		#region BasicPluginDataPage
		#region Public properties
		public string NewRuntime
		{
			get { return _newRuntime; }
			set { SetProperty(ref _newRuntime, value, "NewRuntime"); }
		}
		#endregion
		
		#endregion
		
		#region DependsConflictsBuilderPage  
		#region Public properties
    
    public PluginNameId DependsOnSelectedItem 
    {
    	get { return _dependsOnSelectedItem; }
    	set { SetProperty(ref _dependsOnSelectedItem, value, "DependsOnSelectedItem"); }
    }  
        
    public PluginNameId ConflictsWithSelectedItem 
    {
    	get { return _conflictsWithSelectedItem; }
    	set { SetProperty(ref _conflictsWithSelectedItem, value, "ConflictsWithSelectedItem"); }
    }  
    
    public string NewBuilderName 
    {
    	get { return _newBuilderName; }
    	set { SetProperty(ref _newBuilderName, value, "NewBuilderName"); }
    }  
    
    public string NewBuilderClassName 
    {
    	get { return _newBuilderClassName; }
    	set { SetProperty(ref _newBuilderClassName, value, "NewBuilderClassName"); }
    }  
    
    
    public ObservableCollection<string> BuilderList { get; private set; }
    
    #endregion
    
    #region private methods
    private void OnBuilderListChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
    	// user can only add therefore we only store on Add
    	if (e.Action != NotifyCollectionChangedAction.Add) return;
    	if (e.NewItems == null) return;
    	
    	ServiceRegistration.Get<ILogger>().Debug("Updating BuilderList");
      var settingsManager = ServiceRegistration.Get<ISettingsManager>();
    	var settings = settingsManager.Load<WizardSettings>();
    	if (settings.Builders == null)
    		settings.Builders = new List<string>();
    	foreach (var item in e.NewItems)
    	{
    		var path = (string)item;
    		if (!string.IsNullOrEmpty(path))
    			settings.Builders.Add(path);
    	}
    	settingsManager.Save(settings);
    }
    #endregion
    
		#endregion
			
		#region RegisterPage
		
		#region Public properties
		
    #region Register Location section
    public ObservableCollection<string> RegisterLocations { get; private set; }
      
		public string NewRegisterLocation
		{
			get { return _newRegisterLocation; }
			set { SetProperty(ref _newRegisterLocation, value, "NewRegisterLocation"); }
		}

    public int RegisterSelectedIndex
    {
      get { return _registerSelectedIndex; }
      set
      {
        SetProperty(ref _registerSelectedIndex, value, "RegisterSelectedIndex");
        OnPropertyChanged("RegisterItems");
        if (RegisterItems.Count > 0)
          RegisterItemSelectedIndex = 0;
      }
    }    

    #endregion
    
    #region Register Items
     public ObservableCollection<PluginRegisterItem> DefaultRegisterItems = new ObservableCollection<PluginRegisterItem>();
    public ObservableCollection<PluginRegisterItem> RegisterItems 
    {
      get
      {
        if ((Registers.Count < 1) || RegisterSelectedIndex < 0) return DefaultRegisterItems;
        return Registers[RegisterSelectedIndex].Items;
      }
    }
    
    public string NewRegisterItemName
		{
			get { return _newRegisterItemName; }
			set { SetProperty(ref _newRegisterItemName, value, "NewRegisterItemName"); }
		}

    public string NewRegisterItemId
    {
      get { return _newRegisterItemId; }
      set { SetProperty(ref _newRegisterItemId, value, "NewRegisterItemId"); }
    }

    public bool NewRegisterItemRedundant
    {
      get { return _newRegisterItemRedundant; }
      set { SetProperty(ref _newRegisterItemRedundant, value, "NewRegisterItemRedundant"); }
    }

    public int RegisterItemSelectedIndex
    {
      get { return _registerItemSelectedIndex; }
      set
      {
        SetProperty(ref _registerItemSelectedIndex, value, "RegisterItemSelectedIndex");
        OnPropertyChanged("RegisterAttributes");
      }
    }    
    #endregion
    
    #region Attributes section
    public ObservableCollection<PluginRegisterAttribute> DefaultRegisterAttributes = new ObservableCollection<PluginRegisterAttribute>();

	  public ObservableCollection<PluginRegisterAttribute> RegisterAttributes
	  {
      get
      {
        if ((RegisterItems.Count < 1) || RegisterItemSelectedIndex < 0) return DefaultRegisterAttributes;
        return RegisterItems[RegisterItemSelectedIndex].Attributes;
      }
	  }
   
    public string NewRegisterAttributeName
    {
      get { return _newRegisterAttributeName; }
      set { SetProperty(ref _newRegisterAttributeName, value, "NewRegisterAttributeName"); }
    }
    	
    public string NewRegisterAttributeValue
    {
      get { return _newRegisterAttributeValue; }
      set { SetProperty(ref _newRegisterAttributeValue, value, "NewRegisterAttributeValue"); }
    }	
    #endregion
    
    #endregion
		
		#endregion
		
		#region ResultPage
		
		#region Public properties
    public string PluginText
    {
    	get { return _pluginText; }
    	set { SetProperty(ref _pluginText, value, "PluginText"); }
    }

   
    #endregion
    
    #region Public Methods
    public void UpDatePluginText()
    {
    	var sb = new StringBuilder();
    	var writer = new StringWriter(sb);
    	
    	if (Save(writer))
    	{
    		PluginText = sb.ToString();
    	}
    }


    #endregion
    
		#endregion

    #region Commands
    
   	#region Runtime Commands
       
    /// <summary>
    /// 
    /// </summary>
    public ICommand AddRuntimeCommand
    {
      get 
      {
        return _addRuntimeCommand ?? (_addRuntimeCommand = new RelayCommand(AddRuntime));
      }
    }

    bool CanAddRuntime
    {
    	get { return (!string.IsNullOrEmpty(NewRuntime)); }
    }

    void AddRuntime(object o)
    {
      if (CanAddRuntime)
      {
        AssemblyFiles.Add(NewRuntime);
      }
    }

    /// <summary>
    /// 
    /// </summary>
    public ICommand RemoveRuntimeCommand
    {
      get
      {
        return _removeRuntimeCommand ?? (_removeRuntimeCommand = new RelayCommand(RemoveRuntime));
      }
    }

    void RemoveRuntime(object o)
    {
      var item = (string) o;
      AssemblyFiles.Remove(item);
    }
    
    #endregion
    
    #region DependsOn Commands
    /// <summary>
    /// 
    /// </summary>
    public ICommand AddDependsOnCommand
    {
      get 
      {
        return _addDependsOnCommand ?? (_addDependsOnCommand = new RelayCommand(AddDependsOn, param => CanAddDependsOn));
      }
    }

    bool CanAddDependsOn
    {
      get { return (DependsOnSelectedItem != null); }
    }

    void AddDependsOn(object o)
    {
      if (CanAddDependsOn)
      {
        DependsOn.Add(DependsOnSelectedItem);
      }
    }

    /// <summary>
    /// 
    /// </summary>
    public ICommand RemoveDependsOnCommand
    {
      get
      {
        return _removeDependsOnCommand ?? (_removeDependsOnCommand = new RelayCommand(RemoveDependsOn));
      }
    }

    void RemoveDependsOn(object o)
    {
      var plugin = (PluginNameId) o;
      DependsOn.Remove(plugin);
    }
    
    #endregion
    
    #region ConflictsWith Commands
    /// <summary>
    /// 
    /// </summary>
    public ICommand AddConflictsWithCommand
    {
      get 
      {
        return _addConflictsWithCommand ?? (_addConflictsWithCommand = new RelayCommand(AddConflictsWith, param => CanAddConflictsWith));
      }
    }

    bool CanAddConflictsWith
    {
      get { return (ConflictsWithSelectedItem != null); }
    }

    void AddConflictsWith(object o)
    {
      if (CanAddConflictsWith)
      {
        ConflictsWith.Add(ConflictsWithSelectedItem);
      }
    }

    /// <summary>
    /// 
    /// </summary>
    public ICommand RemoveConflictsWithCommand
    {
      get
      {
        return _removeConflictsWithCommand ?? (_removeConflictsWithCommand = new RelayCommand(RemoveConflictsWith));
      }
    }

    void RemoveConflictsWith(object o)
    {
      var plugin = (PluginNameId) o;
      ConflictsWith.Remove(plugin);
    }
    
    #endregion
    
    #region Builders Commands
    /// <summary>
    /// 
    /// </summary>
    public ICommand AddBuildersCommand
    {
      get 
      {
        return _addBuildersCommand ?? (_addBuildersCommand = new RelayCommand(AddBuilders));
      }
    }

    bool CanAddBuilders
    {
    	get { return (!string.IsNullOrEmpty(NewBuilderName) && !string.IsNullOrEmpty(NewBuilderClassName)); }
    }

    void AddBuilders(object o)
    {
      if (CanAddBuilders)
      {
        Builders.Add(new PluginBuilder(NewBuilderName, NewBuilderClassName));
      }
    }

    /// <summary>
    /// 
    /// </summary>
    public ICommand RemoveBuildersCommand
    {
      get {return _removeBuildersCommand ?? (_removeBuildersCommand = new RelayCommand(RemoveBuilders)); }
    }

    void RemoveBuilders(object o)
    {
      var builder = (PluginBuilder) o;
      Builders.Remove(builder);
    }
    
    #endregion
    
    #region RegisterLocation
    /// <summary>
    /// 
    /// </summary>
    public ICommand AddRegisterLocationCommand
    {
      get 
      {
        return _addRegisterLocationCommand ?? (_addRegisterLocationCommand = new RelayCommand(AddRegisterLocation));
      }
    }

    bool CanAddRegisterLocation
    {
    	get { return (!string.IsNullOrEmpty(NewRegisterLocation)); }
    }

    void AddRegisterLocation(object o)
    {
      if (CanAddRegisterLocation)
        Registers.Add(new PluginRegister(NewRegisterLocation));
    }

    /// <summary>
    /// 
    /// </summary>
    public ICommand RemoveRegisterLocationCommand
    {
      get
      {
        return _removeRegisterLocationCommand ?? (_removeRegisterLocationCommand = new RelayCommand(RemoveRegisterLocation));
      }
    }

    void RemoveRegisterLocation(object o)
    {
      var location = (PluginRegister) o;
      Registers.Remove(location);
    }
    #endregion
    
    #region RegisterItem
    /// <summary>
    /// 
    /// </summary>
    public ICommand AddRegisterItemCommand
    {
      get 
      {
        return _addRegisterItemCommand ?? (_addRegisterItemCommand = new RelayCommand(AddRegisterItem));
      }
    }

    bool CanAddRegisterItem
    {
      get { return (!string.IsNullOrEmpty(NewRegisterItemName)) && (!string.IsNullOrEmpty(NewRegisterItemId)); }
    }

    void AddRegisterItem(object o)
    {
      if (CanAddRegisterItem)
      {
        RegisterItems.Add(new PluginRegisterItem(NewRegisterItemName, NewRegisterItemId, NewRegisterItemRedundant));
        OnPropertyChanged("RegisterItems");
      }
    }

    /// <summary>
    /// 
    /// </summary>
    public ICommand RemoveRegisterItemCommand
    {
      get
      {
        return _removeRegisterItemCommand ?? (_removeRegisterItemCommand = new RelayCommand(RemoveRegisterItem));
      }
    }

    void RemoveRegisterItem(object o)
    {
      var item = (PluginRegisterItem) o;
      RegisterItems.Remove(item);
    }
    #endregion
    
    #region RegisterAttribute
    /// <summary>
    /// 
    /// </summary>
    public ICommand AddRegisterAttributeCommand
    {
      get 
      {
        return _addRegisterAttributeCommand ?? (_addRegisterAttributeCommand = new RelayCommand(AddRegisterAttribute));
      }
    }

    bool CanAddRegisterAttribute
    {
      get { return (!string.IsNullOrEmpty(NewRegisterAttributeName) && !string.IsNullOrEmpty(NewRegisterAttributeValue)); }
    }

    void AddRegisterAttribute(object o)
    {
      if (CanAddRegisterAttribute)
      {
        RegisterAttributes.Add(new PluginRegisterAttribute(NewRegisterAttributeName, NewRegisterAttributeValue));
        OnPropertyChanged("RegisterAttributes");
      }
    }

    /// <summary>
    /// 
    /// </summary>
    public ICommand RemoveRegisterAttributeCommand
    {
      get
      {
        return _removeRegisterAttributeCommand ?? (_removeRegisterAttributeCommand = new RelayCommand(RemoveRegisterAttribute));
      }
    }

    void RemoveRegisterAttribute(object o)
    {
      var attribute = (PluginRegisterAttribute) o;
      RegisterAttributes.Remove(attribute);
    }
    #endregion
    
    #endregion
	
	}
}
