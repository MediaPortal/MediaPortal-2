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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using MediaPortal.Common.Logging;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Extensions.MetadataExtractors.StubMetadataExtractors.Settings;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Globalization;
using MediaPortal.Extensions.MetadataExtractors.StubMetadataExtractors.Utilities;

namespace MediaPortal.Extensions.MetadataExtractors.StubMetadataExtractors.StubReaders
{
  /// <summary>
  /// Base class for all stub-file readers
  /// </summary>
  /// <remarks>
  /// We have a separate reader for the different stub-files of all possible MediaItem types (in particular movies and series).
  /// This abstract base class contains common functionality that can be used for all types of stub-files.
  /// </remarks>
  public abstract class StubReaderBase<TStub> where TStub : new()
  {
    #region Delegates

    /// <summary>
    /// Delegate used to synchronously read a <see cref="XElement"/> from a stub-file
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read</param>
    /// <returns><c>true</c> if metadata could be read successfully from <paramref name="element"/>; else <c>false</c></returns>
    protected delegate bool TryReadElementDelegate(XElement element);

    /// <summary>
    /// Delegate used to asynchronously read a <see cref="XElement"/> from a stub-file
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read</param>
    /// <param name="stubDirectoryFsra"><see cref="IFileSystemResourceAccessor"/> to the parent directory of the stub-file</param>
    /// <returns><c>true</c> if metadata could be read successfully from <paramref name="element"/>; else <c>false</c></returns>
    protected delegate Task<bool> TryReadElementAsyncDelegate(XElement element, IFileSystemResourceAccessor stubDirectoryFsra);

    #endregion

    #region Protected fields

    /// <summary>
    /// After a successful call to <see cref="TryReadStubFileAsync"/> the content of the stub-file is in this byte array
    /// </summary>
    protected byte[] _stubBytes;

    /// <summary>
    /// After a call to <see cref="TryReadMetadataAsync"/> all parsed stub objects are contained in this list
    /// </summary>
    protected List<TStub> _stubs = new List<TStub>();

    /// <summary>
    /// Stub object used to temporarily store all readily parsed information from the stub-file
    /// If any information was parsed, this object is added to <see cref="_stubs"/>
    /// </summary>
    protected TStub _currentStub;

    /// <summary>
    /// Debug logger
    /// </summary>
    /// <remarks>
    /// NoLogger or FileLogger depending on the respective <see cref="StubMetadataExtractorSettingsBase"/>
    /// </remarks>
    protected ILogger _debugLogger;

    /// <summary>
    /// Unique number of the MediaItem for which this StubReader was instantiated
    /// </summary>
    protected long _miNumber;

    /// <summary>
    /// If true, no long lasting operations such as parsing pictures are performed
    /// </summary>
    protected bool _importOnly;

    /// <summary>
    /// Dictionary used to find the appropriate <see cref="TryReadElementDelegate"/> or <see cref="TryReadElementAsyncDelegate"/> by element name
    /// </summary>
    protected readonly Dictionary<XName, Delegate> _supportedElements = new Dictionary<XName, Delegate>();

    /// <summary>
    /// Settings of the StubMetadataExtractor
    /// </summary>
    /// <remarks>
    /// This abstract base class can only access the properties of the settings base class <see cref="StubMetadataExtractorSettingsBase"/>.
    /// Properties defined in a settings class derived from <see cref="StubMetadataExtractorSettingsBase"/> can only be accessed by the
    /// respective derived reader class.
    /// </remarks>
    protected StubMetadataExtractorSettingsBase _settings;

    #endregion

    #region Ctor

    /// <summary>
    /// Initializes protected and private fields
    /// </summary>
    /// <param name="debugLogger">Debug logger to log to</param>
    /// <param name="miNumber">Unique number of the MediaItem for which the stub-file is parsed</param>
    /// <param name="importOnly">If <c>true</c>, no long lasting operations such as parsing pictures are performed</param>
    /// <param name="httpClient"><see cref="HttpClient"/> used to download from http URLs contained in stub-files</param>
    /// <param name="settings">Settings of the StubMetadataExtractor</param>
    protected StubReaderBase(ILogger debugLogger, long miNumber, bool importOnly, StubMetadataExtractorSettingsBase settings)
    {
      _debugLogger = debugLogger;
      _miNumber = miNumber;
      _importOnly = importOnly;
      _settings = settings;
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Tries to read an stub-file into a <see cref="XDocument"/>
    /// </summary>
    /// <param name="stubFsra"><see cref="IFileSystemResourceAccessor"/> pointing to the stub-file</param>
    /// <returns><c>true</c> if any usable metadata was found; else <c>false</c></returns>
    public virtual async Task<bool> TryReadMetadataAsync(IFileSystemResourceAccessor stubFsra)
    {
      var stubFileWrittenToDebugLog = false;

      // Make sure the stub-file was read into _stubBytes as byte array
      if (_stubBytes == null && !await TryReadStubFileAsync(stubFsra).ConfigureAwait(false))
        return false;

      if (_settings.EnableDebugLogging && _settings.WriteRawStubFileIntoDebugLog)
        // ReSharper disable once AssignNullToNotNullAttribute
        // TryReadStubFileAsync makes sure that _stubBytes is not null
        using (var stubMemoryStream = new MemoryStream(_stubBytes))
        using (var stubReader = new StreamReader(stubMemoryStream, true))
        {
          var stubString = stubReader.ReadToEnd();
          _debugLogger.Debug("[#{0}]: Stub-file (Encoding: {1}):{2}{3}", _miNumber, stubReader.CurrentEncoding, Environment.NewLine, stubString);
          stubFileWrittenToDebugLog = true;
        }
      
      try
      {
        // ReSharper disable once AssignNullToNotNullAttribute
        // TryReadStubFileAsync makes sure that _stubBytes is not null
        using (var memoryStubStream = new MemoryStream(_stubBytes))
        using (var xmlReader = new XmlStubReader(memoryStubStream))
        {
          var stubDocument = XDocument.Load(xmlReader);
          return await TryReadStubDocumentAsync(stubDocument, stubFsra).ConfigureAwait(false);
        }
      }
      catch (Exception e)
      {
        // ReSharper disable once AssignNullToNotNullAttribute
        // TryReadStubFileAsync makes sure that _stubBytes is not null
        using (var stubMemoryStream = new MemoryStream(_stubBytes))
        using (var stubReader = new StreamReader(stubMemoryStream, true))
        {
          try
          {
            if (!stubFileWrittenToDebugLog)
            {
              var stubString = stubReader.ReadToEnd();
              _debugLogger.Warn("[#{0}]: Cannot parse stub-file with XMLReader (Encoding: {1}):{2}{3}", e, _miNumber, stubReader.CurrentEncoding, Environment.NewLine, stubString);
            }
          }
          catch (Exception ex)
          {
            _debugLogger.Error("[#{0}]: Cannot extract metadata; neither XMLReader can parse nor StreamReader can read the bytes read from the stub-file", ex, _miNumber);
          }
        }
        return false;
      }
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Calls the appropriate <see cref="TryReadElementDelegate"/> or <see cref="TryReadElementAsyncDelegate"/>for each element of root
    /// </summary>
    /// <param name="stubDocument"><see cref="XDocument"/> containing the respective stub-file</param>
    /// <param name="stubFsra"><see cref="IFileSystemResourceAccessor"/> to the stub-file</param>
    /// <returns><c>true</c> if any usable metadata was found; else <c>false</c></returns>
    private async Task<bool> TryReadStubDocumentAsync(XDocument stubDocument, IFileSystemResourceAccessor stubFsra)
    {
      // Checks the structure of the stub document
      if (!IsValidStubDocument(stubDocument))
        return false;

      _stubs.Clear();
      var result = false;
      
      // Create an IFileSystemResourceAccessor to the parent directory of the stub-file 
      var stubDirectoryResourcePath = ResourcePathHelper.Combine(stubFsra.CanonicalLocalResourcePath, "../");
      IResourceAccessor stubDirectoryRa;
      IFileSystemResourceAccessor stubDirectoryFsra = null;
      if (stubDirectoryResourcePath.TryCreateLocalResourceAccessor(out stubDirectoryRa))
      {
        stubDirectoryFsra = stubDirectoryRa as IFileSystemResourceAccessor;
        if (stubDirectoryFsra == null)
          stubDirectoryRa.Dispose();
      }

      using (stubDirectoryFsra)
      {
        // IsValidStubDocument returns false if stubRootDocument is null
        // ReSharper disable once PossibleNullReferenceException
        foreach (var itemRoot in stubDocument.Root.Elements().Where(CanReadItemRootElementTree))
        {
          _currentStub = new TStub();
          var metadataFound = false;
          foreach (var element in itemRoot.Elements())
          {
            Delegate readDelegate;
            if (_supportedElements.TryGetValue(element.Name, out readDelegate))
            {
              try
              {
                if ((readDelegate is TryReadElementDelegate && (readDelegate as TryReadElementDelegate).Invoke(element)) ||
                    (readDelegate is TryReadElementAsyncDelegate && await (readDelegate as TryReadElementAsyncDelegate).Invoke(element, stubDirectoryFsra).ConfigureAwait(false)))
                  metadataFound = true;
              }
              catch (Exception e)
              {
                _debugLogger.Error("[#{0}]: Exception while reading element {1}", e, _miNumber, element);
              }
            }
            else
              _debugLogger.Warn("[#{0}]: Unknown element {1}", _miNumber, element);
          }
          if (metadataFound)
          {
            _stubs.Add(_currentStub);
            result = true;
          }
        }
        _currentStub = default(TStub);
      }
      return result;
    }

    /// <summary>
    /// Checks if the stubDocument has a root element with the name "root" and at least one child element
    /// </summary>
    /// <param name="stubDocument">Document to check</param>
    /// <returns><c>true</c> if ´<paramref name="stubDocument"/> represents a valid stub-document; otherwise <c>false</c></returns>
    /// <remarks>For the structure of <paramref name="stubDocument"/> see the documentation of <see cref="XmlStubReader"/></remarks>
    private bool IsValidStubDocument(XDocument stubDocument)
    {
      if (stubDocument.Root == null)
      {
        _debugLogger.Warn("[#{0}]: Cannot extract metadata; no root element found", _miNumber);
        return false;
      }
      if (stubDocument.Root.Name.ToString() != "root")
      {
        _debugLogger.Error("[#{0}]: Cannot extract metadata; root element name is not 'root'; potential bug in XmlStubReader", _miNumber);
        return false;
      }
      if (!stubDocument.Root.HasElements)
      {
        _debugLogger.Warn("[#{0}]: Cannot extract metadata; no item-root elements found", _miNumber);
        return false;
      }
      if (stubDocument.Root.Elements().Count() > 1)
      {
        _debugLogger.Info("[#{0}]: {1} item root elements found in the stub-file", _miNumber, stubDocument.Root.Elements().Count());
        var firstItemRootElementName = stubDocument.Root.Elements().First().Name.ToString();
        foreach (var element in stubDocument.Root.Elements().Where(element => element.Name.ToString() != firstItemRootElementName))
          _debugLogger.Warn("[#{0}]: First item root element name is {1}, but there is another item root element with the name {2}", _miNumber, firstItemRootElementName, element.Name.ToString());
      }
      return true;
    }

    #endregion

    #region Protected methods

    /// <summary>
    /// Tries to read an stub-file into a byte array (<see cref="_stubBytes"/>)
    /// </summary>
    /// <param name="stubFsra">FileSystemResourceAccessor pointing to the stub-file</param>
    /// <returns><c>true</c>, if the file was successfully read; otherwise <c>false</c></returns>
    protected async Task<bool> TryReadStubFileAsync(IFileSystemResourceAccessor stubFsra)
    {
      try
      {
        using (var stubStream = await stubFsra.OpenReadAsync().ConfigureAwait(false))
        {
          // For xml-files it is recommended to read them as byte array. Reason is that reading as byte array does
          // not yet consider any encoding. After that, it is recommended to use the XmlReader (instead of a StreamReader)
          // because the XmlReader first considers the "Byte Order Mark" ("BOM"). If such is not present, UTF-8 is used.
          // If the XML declaration contains an encoding attribute (which is optional), the XmlReader (contrary to the
          // StreamReader) automatically switches to the enconding specified by the XML declaration.
          _stubBytes = new byte[stubStream.Length];
          await stubStream.ReadAsync(_stubBytes, 0, (int)stubStream.Length).ConfigureAwait(false);
        }
      }
      catch (Exception e)
      {
        _debugLogger.Error("[#{0}]: Cannot extract metadata; cannot read stub-file", e, _miNumber);
        return false;
      }
      return true;
    }

    /// <summary>
    /// Writes the <see cref="_stubs"/> object including its metadata into the debug log in Json form
    /// </summary>
    protected void LogStubObjects()
    {
      _debugLogger.Debug("[#{0}]: {1}s: {2}{3}", _miNumber, _stubs.GetType().GetGenericArguments()[0].Name, Environment.NewLine, JsonConvert.SerializeObject(_stubs, Formatting.Indented, new JsonSerializerSettings { Converters = { new JsonByteArrayConverter(), new StringEnumConverter() } }));
    }

    /// <summary>
    /// Tries to read a simple string from <paramref name="element"/>.Value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns>
    /// <c>null</c> if <paramref name="element"/> is null, <paramref name="element"/> has child elements,
    /// <paramref name="element"/>.Value.Trim() is null, <paramref name="element"/>.Value.Trim() is an empty string or
    /// _seetings.IgnoreStrings is not null and contains a value that (ignoring casing) exactly matches <paramref name="element"/>.Value.Trim();
    /// otherwise <paramref name="element"/>.Value.Trim()
    /// </returns>
    protected string ParseSimpleString(XElement element)
    {
      if (element == null)
        return null;
      if (element.HasElements)
      {
        _debugLogger.Warn("[#{0}]: The following element was supposed to contain a simple value, but it contains child elements: {1}", _miNumber, element);
        return null;
      }
      var result = element.Value.Trim().Trim(new char[] { '|' });
      if (_settings.IgnoreStrings != null && _settings.IgnoreStrings.Contains(result, StringComparer.OrdinalIgnoreCase))
        return null;
      return String.IsNullOrEmpty(result) ? null : result;
    }

    /// <summary>
    /// Tries to read a simple int? from <paramref name="element"/>.Value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns>
    /// <c>null</c> if <see cref="ParseSimpleString"/> returns <c>null</c> for <paramref name="element"/>
    /// or <see cref="ParseSimpleString"/> for <paramref name="element"/> does not contain a valid <see cref="int"/> value;
    /// otherwise (int?)<paramref name="element"/>
    /// </returns>
    protected int? ParseSimpleInt(XElement element)
    {
      var intString = ParseSimpleString(element);
      if (intString == null)
        return null;
      int? result = null;
      try
      {
        result = (int?)element;
      }
      catch (Exception)
      {
        _debugLogger.Warn("[#{0}]: The following element was supposed to contain an int value, but it does not: {1}", _miNumber, element);
      }
      return result;
    }

    /// <summary>
    /// Tries to read a simple long? from <paramref name="element"/>.Value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns>
    /// <c>null</c> if <see cref="ParseSimpleString"/> returns <c>null</c> for <paramref name="element"/>
    /// or <see cref="ParseSimpleString"/> for <paramref name="element"/> does not contain a valid <see cref="long"/> value;
    /// otherwise (long?)<paramref name="element"/>
    /// </returns>
    protected long? ParseSimpleLong(XElement element)
    {
      var longString = ParseSimpleString(element);
      if (longString == null)
        return null;
      long? result = null;
      try
      {
        result = (long?)element;
      }
      catch (Exception)
      {
        _debugLogger.Warn("[#{0}]: The following element was supposed to contain an long value, but it does not: {1}", _miNumber, element);
      }
      return result;
    }

    /// <summary>
    /// Tries to read a simple decimal? from <paramref name="element"/>.Value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns>
    /// <c>null</c> if <see cref="ParseSimpleString"/> returns <c>null</c> for <paramref name="element"/>
    /// or <see cref="ParseSimpleString"/> for <paramref name="element"/> does not contain a valid <see cref="decimal"/> value;
    /// otherwise (decimal?)<paramref name="element"/>.
    /// If a fraction or ratio is found it will try to convert those to a decimal value.
    /// </returns>
    protected decimal? ParseSimpleDecimal(XElement element)
    {
      var decimalString = ParseSimpleString(element);
      if (decimalString == null)
        return null;

      //Decimal defined as fraction
      if (decimalString.Contains("/"))
      {
        string[] numbers = decimalString.Split('/');
        return decimal.Parse(numbers[0]) / decimal.Parse(numbers[1]);
      }

      //Decimal defined as ratio
      if (decimalString.Contains(":"))
      {
        string[] numbers = decimalString.Split(':');
        return decimal.Parse(numbers[0]) / decimal.Parse(numbers[1]);
      }

      decimal val;
      //Decimal defined as neutral localized string
      if (decimal.TryParse(decimalString, NumberStyles.Float, CultureInfo.InvariantCulture, out val))
      {
        return val;
      }

      //Decimal defined as localized string
      if (decimal.TryParse(decimalString, NumberStyles.Float, CultureInfo.CurrentCulture, out val))
      {
        return val;
      }

      decimal? result = null;
      try
      {
        result = (decimal?)element;
      }
      catch (Exception)
      {
        _debugLogger.Warn("[#{0}]: The following element was supposed to contain a decimal value, but it does not: {1}", _miNumber, element);
      }
      return result;
    }

    /// <summary>
    /// Tries to read a simple DateTime? from <paramref name="element"/>.Value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns>
    /// <c>null</c> if <see cref="ParseSimpleString"/> returns <c>null</c> for <paramref name="element"/>
    /// or <see cref="ParseSimpleString"/> for <paramref name="element"/> does not contain a valid <see cref="DateTime"/> value;
    /// otherwise <paramref name="element"/>.Value converted to a <see cref="DateTime"/>
    /// </returns>
    /// <remarks>
    /// <paramref name="element"/>.Value can be any string that can be parsed by <see cref="DateTime"/>.TryParse or
    /// a simple four digit year. In the latter case the value will be 1 January of that year.
    /// </remarks>
    protected DateTime? ParseSimpleDateTime(XElement element)
    {
      var dateTimeString = ParseSimpleString(element);
      if (dateTimeString == null)
        return null;

      DateTime dateTime;
      if (DateTime.TryParse(dateTimeString, out dateTime))
        return dateTime;

      int year;
      if (Int32.TryParse(dateTimeString, out year))
        if (year >= 1000 && year <= 9999)
          return new DateTime(year, 1, 1);

      // We do not log 0-values; Kodi puts 0 in the year-element of every [episodefilename].nfo
      // resulting in lots of warnings otherwise
      if (year != 0)
        _debugLogger.Warn("[#{0}]: The following element was supposed to contain a DateTime value, but it does not: {1}", _miNumber, element);
      return null;
    }

    /// <summary>
    /// Tries to parse a string which consists of one or more sub-strings separated by a particular separator character
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <param name="values">HashSet of strings to which new entries should be added</param>
    /// <returns>
    /// A HashSet with the new and old string values or <c>null</c> if there are neither old nor new values
    /// If _settings.IgnoreStrings is not null, new values equaling (ignoring casing) one of the
    /// values in _settings.IgnoreStrings are filtered and not added to the result
    /// </returns>
    protected HashSet<string> ParseCharacterSeparatedStrings(XElement element, HashSet<String> values)
    {
      var elementContent = ParseSimpleString(element);
      if (elementContent == null)
        return values;
      List<String> separatedStrings;
      if (_settings.SeparatorCharacters != null && _settings.SeparatorCharacters.Count != 0)
        separatedStrings = elementContent.Split(_settings.SeparatorCharacters.ToArray()).Select(str => str.Trim()).Where(str => !String.IsNullOrEmpty(str)).ToList();
      else
        separatedStrings = new List<string> { elementContent };
      if (_settings.IgnoreStrings != null)
        separatedStrings = separatedStrings.Where(str => !_settings.IgnoreStrings.Contains(str, StringComparer.OrdinalIgnoreCase)).ToList();
      if (!separatedStrings.Any())
        return values;
      if (values == null)
        values = new HashSet<String>(separatedStrings, StringComparer.OrdinalIgnoreCase);
      else
        foreach (var simpleString in separatedStrings)
          values.Add(simpleString);
      return values;
    }

    /// <summary>
    /// Tries to read an attribute with a given <paramref name="attributeName"/> as string from <paramref name="element"/>
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <param name="attributeName">Name of the attribute</param>
    /// <returns>
    /// <c>null</c> if <paramref name="element"/> is null, an attribute with the given
    /// <param name="attributeName"></param> does not exist or contains an empty string;
    /// otherwise the (trimmed) value of the given attribute as string
    /// </returns>
    protected string ParseStringAttribute(XElement element, string attributeName)
    {
      if (element == null)
        return null;
      var attribute = element.Attribute(attributeName);
      if (attribute == null)
        return null;
      var result = attribute.Value.Trim();
      return String.IsNullOrEmpty(result) ? null : result;
    }

    /// <summary>
    /// Tries to read an attribute with a given <paramref name="attributeName"/> as int? from <paramref name="element"/>
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <param name="attributeName">Name of the attribute</param>
    /// <returns>
    /// <c>null</c> if <paramref name="element"/> is null, an attribute with the given
    /// <param name="attributeName"></param> does not exist or does not contain a valid int value;
    /// otherwise the value of the given attribute as int?
    /// </returns>
    protected int? ParseIntAttribute(XElement element, string attributeName)
    {
      if (element == null)
        return null;
      var attribute = element.Attribute(attributeName);
      if (attribute == null)
        return null;
      int? result = null;
      try
      {
        result = (int?)attribute;
      }
      catch (Exception)
      {
        _debugLogger.Warn("[#{0}]: The attribute '{1}' in the following element was supposed to contain an int value, but it does not: {2}", _miNumber, attributeName, element);
      }
      return result;
    }

    #endregion

    #region Abstract methods

    /// <summary>
    /// Checks whether <see cref="itemRootElement"/>.Name has a value that the derived StubReader can understand
    /// </summary>
    /// <param name="itemRootElement"><see cref="XElement"/> to check</param>
    /// <returns><c>true</c> if the derived StubReader can understand the <paramref name="itemRootElement"/>; otherwise <c>false</c></returns>
    protected abstract bool CanReadItemRootElementTree(XElement itemRootElement);

    #endregion
  }
}
