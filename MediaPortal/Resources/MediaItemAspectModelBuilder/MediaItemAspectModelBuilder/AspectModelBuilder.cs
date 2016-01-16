#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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
using System.Reflection;
using System.Text;
using MediaPortal.Common.MediaManagement;

namespace MediaItemAspectModelBuilder
{
  public class AspectModelBuilder
  {
    #region Fields

    protected static Dictionary<string, string> _typeMap = new Dictionary<string, string>
      {
        { "String", "string" },
        { "Int32", "int" },
        { "Int64", "long" },
        { "Double", "double" },
        { "Float", "float" },
        { "Single", "float" },
        { "Boolean", "bool" },
      };
    protected static ICollection<string> _valueTypes = new HashSet<string>
      {
         "Int32", "int" , "Int64", "long",  "Double", "double" , "Float", "float" , "Single", "float" , "Boolean", "bool", "DateTime"
      };
    protected static ICollection<string> _reservedPropertyNames = new HashSet<string>
      {
         "Width", "Height"
      };
    protected IList<string> _copyright = new List<string>();
    protected IList<string> _usings = new List<string>();
    protected IList<string> _consts = new List<string>();
    protected IList<string> _fields = new List<string>();
    protected IList<string> _properties = new List<string>();
    protected IList<string> _propertyCreation = new List<string>();
    protected IList<string> _constructors = new List<string>();
    protected IList<string> _members = new List<string>();
    protected bool _createAsControl;
    protected bool _exposeNullable;

    #endregion

    #region Consts

    const string FIELD_TEMPLATE = "protected AbstractProperty {0};";
    const string BINDING_PROPERTY_TEMPLATE = "public AbstractProperty {0}\r\n{{\r\n  get{{ return {1}; }}\r\n}}";
    const string VALUE_PROPERTY_TEMPLATE = "public {2} {0}\r\n{{\r\n  get {{ return ({2}) {1}.GetValue(); }}\r\n  set {{ {1}.SetValue(value); }}\r\n}}";
    const string PROPERTY_CREATION_TEMPLATE = "{0} = new WProperty(typeof({1}));";
    const string PROPERTY_CREATION_TEMPLATE_CONTROL = "{0} = new SProperty(typeof({1}));"; // Strong references allowed for Controls, but not for models

    #endregion

    /// <summary>
    /// Creates a class that exposes <see cref="MediaItemAspectMetadata.AttributeSpecification"/> as properties that can be used from xaml.
    /// This helper can create different class styles, depending on <paramref name="createAsControl"/>:
    /// <para>
    /// <c>false</c>: Create a standalone class using WProperty as property type (for models)
    /// <c>true</c>:  Create a class that derives from Control using SProperty as property type. This class can be used as custom control.
    /// </para>
    /// </summary>
    /// <param name="aspectType">MediaAspect type.</param>
    /// <param name="classNamespace">Namespace of generated class.</param>
    /// <param name="createAsControl">Create a control (see remarks on class).</param>
    /// <param name="exposeNullable">Internal value types are exposed as C# Nullable.</param>
    /// <returns>Source code of created class.</returns>
    public string BuildCodeTemplate(Type aspectType, string classNamespace, bool createAsControl, bool exposeNullable)
    {
      MediaItemAspectMetadata metadata = GetMetadata(aspectType);
      _createAsControl = createAsControl;
      _exposeNullable = exposeNullable;
      string baseClass = _createAsControl ? ": Control" : "";
      string aspectName = aspectType.Name;
      IDictionary<string, MediaItemAspectMetadata.AttributeSpecification> attributeSpecifications = metadata.AttributeSpecifications;

      foreach (KeyValuePair<string, MediaItemAspectMetadata.AttributeSpecification> attributeSpecification in attributeSpecifications)
      {
        MediaItemAspectMetadata.AttributeSpecification spec = attributeSpecification.Value;
        string attrName = attributeSpecification.Key;
        Type attrType = spec.AttributeType;

        CreateProperty(attrName, attrType, spec.IsCollectionAttribute);
      }

      #region Copyright

      // Copyright
      _copyright.Add(@"/*
    Copyright (C) 2007-2015 Team MediaPortal
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
*/");
      #endregion

      #region Headers

      // Usings
      _usings.Add("using System;");
      _usings.Add("using System.Collections.Generic;");
      _usings.Add("using MediaPortal.Common.General;");
      _usings.Add("using MediaPortal.Common.MediaManagement;");
      _usings.Add("using MediaPortal.Common.MediaManagement.DefaultItemAspects;");
      if (_createAsControl)
        _usings.Add("using MediaPortal.UI.SkinEngine.Controls.Visuals;");

      // Constants
      _consts.Add("public static readonly ICollection<string> EMPTY_STRING_COLLECTION = new List<string>().AsReadOnly();");

      // Common properties
      CreateProperty("MediaItem", typeof(MediaItem));
      _propertyCreation.Add("_mediaItemProperty.Attach(MediaItemChanged);");

      #endregion

      // Construct source file
      StringBuilder result = new StringBuilder();

      AppendRegion(result, "Copyright (C) 2007-2015 Team MediaPortal", _copyright, false);

      AppendRegion(result, null, _usings, false);

      result.AppendLine();
      result.AppendLine();
      result.AppendFormat("namespace {0}\r\n{{\r\n", classNamespace); // Begin of namespace

      result.AppendLine("/// <summary>");
      result.AppendFormat("/// {0}Wrapper wraps the contents of <see cref=\"{0}\"/> into properties that can be bound from xaml controls.\r\n", aspectName);
      result.AppendLine("/// Note: this code was automatically created by the MediaItemAspectModelBuilder helper tool under Resources folder.");
      result.AppendLine("/// </summary>"); // Begin of class
      result.AppendFormat("public class {0}Wrapper{1}\r\n{{\r\n", aspectName, baseClass); // Begin of class

      AppendRegion(result, "Constants", _consts, false);
      AppendRegion(result, "Fields", _fields, false);
      AppendRegion(result, "Properties", _properties);

      List<string> ctors = new List<string> { string.Format("public {0}Wrapper()\r\n{{\r\n  {1}\r\n}}", aspectName, string.Join("\r\n  ", _propertyCreation.ToArray())) };
      AppendRegion(result, "Constructor", ctors);

      _members.Add(@"private void MediaItemChanged(AbstractProperty property, object oldvalue)
{
  Init(MediaItem);
}");

      CreateMembers(aspectType, _members);
      AppendRegion(result, "Members", _members);

      result.AppendLine("}"); // End of class
      result.AppendLine("\r\n}"); // End of namespace

      return result.ToString();
    }

    #region Members

    private void CreateMembers(Type aspectType, IList<string> members)
    {
      string methodStub = @"public void Init(MediaItem mediaItem)
{{
  MediaItemAspect aspect;
  if (mediaItem == null ||!mediaItem.Aspects.TryGetValue({1}.ASPECT_ID, out aspect))
  {{
     SetEmpty();
     return;
  }}

  {0}
}}";
      string emptyStub = @"public void SetEmpty()
{{
  {0}
}}
";
      List<string> initCommands = new List<string>();
      List<string> emptyCommands = new List<string>();
      foreach (FieldInfo fieldInfo in aspectType.GetFields())
      {
        if (!fieldInfo.Name.StartsWith("ATTR_"))
          continue;

        MediaItemAspectMetadata.AttributeSpecification spec = (MediaItemAspectMetadata.AttributeSpecification) fieldInfo.GetValue(null);

        string attrName = CreateSafePropertyName(spec.AttributeName);
        string typeName = BuildTypeName(spec.AttributeType, spec.IsCollectionAttribute, _exposeNullable);
        if (_valueTypes.Contains(typeName) && !_exposeNullable)
        {
          string varName = FirstLower(spec.AttributeName);
          initCommands.Add(string.Format("{0}? {1} = ({0}?) aspect[{2}.{3}];", typeName, varName, aspectType.Name, fieldInfo.Name));
          initCommands.Add(string.Format("{0} = {1}.HasValue? {1}.Value : default({2});", attrName, varName, typeName));
        }
        else
        {
          string defaultValue = typeName == "IEnumerable<string>" ? " ?? EMPTY_STRING_COLLECTION" : "";
          initCommands.Add(string.Format("{0} = ({1}) aspect[{2}.{3}]{4};", attrName, typeName, aspectType.Name, fieldInfo.Name, defaultValue));
        }

        emptyCommands.Add(string.Format("{0} = {1};", attrName, GetEmptyValue(spec)));
      }

      members.Add(string.Format(methodStub, string.Join("\r\n", initCommands.ToArray()), aspectType.Name));

      members.Add(string.Format(emptyStub, string.Join("\r\n", emptyCommands.ToArray())));
    }

    private string GetEmptyValue(MediaItemAspectMetadata.AttributeSpecification spec)
    {
      if (spec.AttributeType == typeof(string) && spec.IsCollectionAttribute)
        return "EMPTY_STRING_COLLECTION";
      if (spec.IsCollectionAttribute)
        return "new List<" + spec.AttributeType.Name + ">()";
      if (!_exposeNullable && _valueTypes.Contains(spec.AttributeType.Name))
        return "default(" + spec.AttributeType.Name + ")";
      return "null";
    }

    private void CreateProperty(string attrName, Type attrType, bool isCollection = false)
    {
      attrName = CreateSafePropertyName(attrName);
      string publicValuePropertyName = attrName;
      string publicPropertyName = publicValuePropertyName + "Property";
      string propertyName = "_" + FirstLower(publicPropertyName);
      string typeName = BuildTypeName(attrType, isCollection, _exposeNullable);
      _fields.Add(string.Format(FIELD_TEMPLATE, propertyName));
      _properties.Add(string.Format(BINDING_PROPERTY_TEMPLATE, publicPropertyName, propertyName));
      _properties.Add(string.Format(VALUE_PROPERTY_TEMPLATE, publicValuePropertyName, propertyName, typeName));
      if (_createAsControl)
        _propertyCreation.Add(string.Format(PROPERTY_CREATION_TEMPLATE_CONTROL, propertyName, typeName));
      else
        _propertyCreation.Add(string.Format(PROPERTY_CREATION_TEMPLATE, propertyName, typeName));
    }

    private string CreateSafePropertyName(string attrName)
    {
      if (_createAsControl && _reservedPropertyNames.Contains(attrName))
        attrName = "Aspect" + attrName;
      return attrName;
    }

    private static string BuildTypeName(Type attrType, bool isCollection, bool exposeNullable)
    {
      string typeName;
      if (!_typeMap.TryGetValue(attrType.Name, out typeName))
        typeName = attrType.Name;

      typeName = isCollection ? string.Format("IEnumerable<{0}>", typeName) : _valueTypes.Contains(typeName) && exposeNullable ? typeName + "?" : typeName;
      return typeName;
    }

    private void AppendRegion(StringBuilder result, string region, IEnumerable<string> list, bool useAdditionalLinebreak = true)
    {
      if (!string.IsNullOrWhiteSpace(region)) result.AppendFormat("#region {0}\r\n\r\n", region);
      result.Append(string.Join("\r\n" + (useAdditionalLinebreak ? "\r\n" : ""), list.ToArray()));
      if (!string.IsNullOrWhiteSpace(region)) result.AppendFormat("\r\n\r\n#endregion\r\n\r\n");
    }

    private static string FirstLower(string value)
    {
      if (string.IsNullOrWhiteSpace(value))
        return string.Empty;

      return value[0].ToString().ToLowerInvariant() + value.Substring(1);
    }

    public MediaItemAspectMetadata GetMetadata(Type type)
    {
      FieldInfo field = type.GetField("Metadata");
      return (MediaItemAspectMetadata) field.GetValue(null);
    }

    #endregion
  }
}
