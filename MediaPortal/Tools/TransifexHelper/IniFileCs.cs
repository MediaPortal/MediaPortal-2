//  Programmer: Ludvik Jerabek
//        Date: 08\23\2010
//     Purpose: Allow INI manipulation in .NET

using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Diagnostics;
using System;

namespace TransifexHelper
{
  /// <summary>
  /// IniFile class used to read and write ini files by loading the file into memory
  /// </summary>
  public class IniFile
  {
    /// <summary>
    /// List of IniSection objects keeps track of all the sections in the INI file
    /// </summary>
    private readonly Dictionary<string, IniSection> _sections;

    /// <summary>
    /// Public constructor
    /// </summary>
    public IniFile()
    {
      _sections = new Dictionary<string, IniSection>(StringComparer.InvariantCultureIgnoreCase);
    }

    /// <summary>
    /// Loads the Reads the data in the ini file into the IniFile object
    /// </summary>
    /// <param name="fileName"></param>
    public void Load(string fileName)
    {
      Load(fileName, false);
    }

    /// <summary>
    /// Loads the Reads the data in the ini file into the IniFile object
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="merge"></param>
    public void Load(string fileName, bool merge)
    {
      if (!merge)
      {
        RemoveAllSections();
      }

      //  Clear the object... 
      IniSection tempsection = null;
      StreamReader oReader = new StreamReader(fileName);
      Regex regexcomment = new Regex("^([\\s]*#.*)", (RegexOptions.Singleline | RegexOptions.IgnoreCase));
      // ^[\\s]*\\[[\\s]*([^\\[\\s].*[^\\s\\]])[\\s]*\\][\\s]*$
      Regex regexsection = new Regex("^[\\s]*\\[[\\s]*([^\\[\\s].*[^\\s\\]])[\\s]*\\][\\s]*$",
                                     (RegexOptions.Singleline | RegexOptions.IgnoreCase));
      //Regex regexsection = new Regex("\\[[\\s]*([^\\[\\s].*[^\\s\\]])[\\s]*\\]", (RegexOptions.Singleline | RegexOptions.IgnoreCase));
      Regex regexkey = new Regex("^\\s*([^=\\s]*)[^=]*=(.*)", (RegexOptions.Singleline | RegexOptions.IgnoreCase));

      while (!oReader.EndOfStream)
      {
        string line = oReader.ReadLine();
        if (line != string.Empty)
        {
          Match m = null;
          if (regexcomment.Match(line).Success)
          {
            m = regexcomment.Match(line);
            Trace.WriteLine(string.Format("Skipping Comment: {0}", m.Groups[0].Value));
          }
          else if (regexsection.Match(line).Success)
          {
            m = regexsection.Match(line);
            Trace.WriteLine(string.Format("Adding section [{0}]", m.Groups[1].Value));
            tempsection = AddSection(m.Groups[1].Value);
          }
          else if (regexkey.Match(line).Success && tempsection != null)
          {
            m = regexkey.Match(line);
            Trace.WriteLine(string.Format("Adding Key [{0}]=[{1}]", m.Groups[1].Value, m.Groups[2].Value));
            tempsection.AddKey(m.Groups[1].Value).Value = m.Groups[2].Value;
          }
          else if (tempsection != null)
          {
            //  Handle Key without value
            Trace.WriteLine(string.Format("Adding Key [{0}]", line));
            tempsection.AddKey(line);
          }
          else
          {
            //  This should not occur unless the tempsection is not created yet...
            Trace.WriteLine(string.Format("Skipping unknown type of data: {0}", line));
          }
        }
      }
      oReader.Close();
    }

    /// <summary>
    /// Used to save the data back to the file or your choice
    /// </summary>
    /// <param name="fileName"></param>
    public void Save(string fileName)
    {
      StreamWriter oWriter = new StreamWriter(fileName, false);

      foreach (IniSection s in Sections)
      {
        Trace.WriteLine(string.Format("Writing Section: [{0}]", s.Name));
        oWriter.WriteLine(string.Format("[{0}]", s.Name));

        foreach (IniSection.IniKey k in s.Keys)
        {
          if (k.Value != string.Empty)
          {
            Trace.WriteLine(string.Format("Writing Key: {0}={1}", k.Name, k.Value));
            oWriter.WriteLine(string.Format("{0}={1}", k.Name, k.Value));
          }
          else
          {
            Trace.WriteLine(string.Format("Writing Key: {0}", k.Name));
            oWriter.WriteLine(string.Format("{0}", k.Name));
          }
        }
      }

      oWriter.Close();
    }

    /// <summary>
    /// Gets all the sections names
    /// </summary>
    public List<IniSection> Sections
    {
      get { return new List<IniSection>(_sections.Values.OrderBy(s => s.Name)); }
    }

    /// <summary>
    /// Adds a section to the IniFile object, returns a IniSection object to the new or existing object
    /// </summary>
    /// <param name="section"></param>
    /// <returns></returns>
    public IniSection AddSection(string section)
    {
      // Trim spaces
      section = section.Trim();

      if (_sections.ContainsKey(section)) return _sections[section];
    
      IniSection s = new IniSection(this, section);
      _sections[section] = s;
      return s;
    }

    /// <summary>
    /// Removes a section by its name section, returns trus on success
    /// </summary>
    /// <param name="section"></param>
    /// <returns></returns>
    public bool RemoveSection(string section)
    {
      // Trim spaces
      section = section.Trim();

      return RemoveSection(GetSection(section));
    }

    /// <summary>
    /// Removes section by object, returns trus on success
    /// </summary>
    /// <param name="section"></param>
    /// <returns></returns>
    public bool RemoveSection(IniSection section)
    {
      if (section == null) return false;

      try
      {
        _sections.Remove(section.Name);
        return true;
      }
      catch (Exception ex)
      {
        Trace.WriteLine(ex.Message);
        return false;
      }
    }

    /// <summary>
    /// Removes all existing sections, returns trus on success
    /// </summary>
    /// <returns></returns>
    public bool RemoveAllSections()
    {
      _sections.Clear();

      return (_sections.Count == 0);
    }

    /// <summary>
    /// Returns an IniSection to the section by name, NULL if it was not found
    /// </summary>
    /// <param name="section"></param>
    /// <returns></returns>
    public IniSection GetSection(string section)
    {
      // Trim spaces
      section = section.Trim();
      if (!_sections.ContainsKey(section)) return null;

      return _sections[section];
    }

    /// <summary>
    /// Returns a KeyValue in a certain section
    /// </summary>
    /// <param name="section"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    public string GetKeyValue(string section, string key)
    {
      IniSection s = GetSection(section);
      if (s == null) return string.Empty;

      IniSection.IniKey k = s.GetKey(key);
      if (k == null) return string.Empty;

      return k.Value;
    }

    /// <summary>
    /// Sets a KeyValuePair in a certain section
    /// </summary>
    /// <param name="section"></param>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public bool SetKeyValue(string section, string key, string value)
    {
      IniSection s = AddSection(section);
      if (s == null) return false;

      IniSection.IniKey k = s.AddKey(key);
      if (k == null) return false;

      k.Value = value;
      return true;
    }

    /// <summary>
    /// Renames an existing section
    /// </summary>
    /// <param name="section"></param>
    /// <param name="newSection"></param>
    /// <returns>returns true on success, false if the section didn't exist or there was another section with the same sNewSection</returns>
    public bool RenameSection(string section, string newSection)
    {
      //  Note string trims are done in lower calls.
      IniSection s = GetSection(section);
      if (s == null) return false;

      return s.SetName(newSection);
    }

    /// <summary>
    /// Renames an existing key
    /// </summary>
    /// <param name="section"></param>
    /// <param name="key"></param>
    /// <param name="newKey"></param>
    /// <returns>returns true on success, false if the key didn't exist or there was another section with the same sNewKey</returns>
    public bool RenameKey(string section, string key, string newKey)
    {
      //  Note string trims are done in lower calls.
      IniSection s = GetSection(section);
      if (s == null) return false;

      IniSection.IniKey k = s.GetKey(key);
      if (k == null) return false;

      return k.SetName(newKey);
    }

    /// <summary>
    /// IniSection class
    /// </summary>
    public class IniSection
    {
      /// <summary>
      /// IniFile IniFile object instance
      /// </summary>
      private readonly IniFile _iniFile;
      /// <summary>
      /// List of IniKeys in the section
      /// </summary>
      private readonly Dictionary<string, IniKey> _keys;

      /// <summary>
      /// Constuctor so objects are internally managed
      /// </summary>
      /// <param name="parent"></param>
      /// <param name="section"></param>
      protected internal IniSection(IniFile parent, string section)
      {
        _iniFile = parent;
        Name = section;
        _keys = new Dictionary<string, IniKey>(StringComparer.InvariantCultureIgnoreCase);
      }

      /// <summary>
      /// Returns and hashtable of keys associated with the section
      /// </summary>
      public List<IniKey> Keys
      {
        get { return new List<IniKey>(_keys.Values.OrderBy(k => k.Name)); }
      }

      /// <summary>
      /// Gets the section name
      /// </summary>
      public string Name { get; private set; }

      /// <summary>
      /// Adds a key to the IniSection object, returns a IniKey object to the new or existing object
      /// </summary>
      /// <param name="key"></param>
      /// <returns></returns>
      public IniKey AddKey(string key)
      {
        // Trim spaces
        key = key.Trim();

        if (key.Length == 0) return null;

        if (_keys.ContainsKey(key)) return _keys[key];

        IniKey k = new IniKey(this, key);
        _keys[key] = k;
        return k;
      }

      /// <summary>
      /// Removes a single key by string
      /// </summary>
      /// <param name="key"></param>
      /// <returns>True if succeeded false if an error occured</returns>
      public bool RemoveKey(string key)
      {
        return RemoveKey(GetKey(key));
      }

      /// <summary>
      /// Removes a single key by IniKey object
      /// </summary>
      /// <param name="key">The key to remove.</param>
      /// <returns>True if succeeded false if an error occured</returns>
      public bool RemoveKey(IniKey key)
      {
        if (key == null) return false;

        try
        {
          _keys.Remove(key.Name);
          return true;
        }
        catch (Exception ex)
        {
          Trace.WriteLine(ex.Message);
          return false;
        }
      }

      /// <summary>
      /// Removes all the keys in the section
      /// </summary>
      /// <returns>True if succeeded false if an error occured</returns>
      public bool RemoveAllKeys()
      {
        _keys.Clear();

        return (_keys.Count == 0);
      }

      /// <summary>
      /// Returns a IniKey object to the key by name, NULL if it was not found
      /// </summary>
      /// <param name="key"></param>
      /// <returns>Returns a IniKey object to the key by name, NULL if it was not found</returns>
      public IniKey GetKey(string key)
      {
        // Trim spaces
        key = key.Trim();

        return _keys.ContainsKey(key) ? _keys[key] : null;
      }

      /// <summary>
      /// Sets the section name, 
      /// </summary>
      /// <param name="section"></param>
      /// <returns>returns true on success, fails if the section name section already exists</returns>
      public bool SetName(string section)
      {
        // Trim spaces
        section = section.Trim();
        if (section.Length == 0) return false;

        // Get existing section if it even exists...
        IniSection s = _iniFile.GetSection(section);
        if (s != this && s != null) return false;

        try
        {
          // Remove the current section
          _iniFile._sections.Remove(Name);
          // Set the new section name to this object
          _iniFile._sections[section] = this;
          // Set the new section name
          Name = section;
          return true;
        }
        catch (Exception ex)
        {
          Trace.WriteLine(ex.Message);
          return false;
        }
      }

      /// <summary>
      /// IniKey class
      /// </summary>
      public class IniKey
      {
        /// <summary>
        /// Pointer to the parent CIniSection
        /// </summary>
        private readonly IniSection _section;

        /// <summary>
        /// Constuctor so objects are internally managed
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="key"></param>
        protected internal IniKey(IniSection parent, string key)
        {
          _section = parent;
          Name = key;
        }

        /// <summary>
        /// Gets the Name of the Key
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets/sets the value of the key
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Sets the key name
        /// </summary>
        /// <param name="key"></param>
        /// <returns>Returns true on success, fails if the section name key already exists</returns>
        public bool SetName(string key)
        {
          key = key.Trim();
          if (key.Length == 0) return false;

          IniKey k = _section.GetKey(key);
          if (k != this && k != null) return false;

          try
          {
            // Remove the current key
            _section._keys.Remove(Name);
            // Set the new key name to this object
            _section._keys[key] = this;
            // Set the new key name
            Name = key;
            return true;
          }
          catch (Exception ex)
          {
            Trace.WriteLine(ex.Message);
            return false;
          }
        }
      }
    }
  }
}