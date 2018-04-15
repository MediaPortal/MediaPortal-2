#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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
    public string BuildCodeTemplate(Type aspectType, Type[] childAspectTypes, string classNamespace, string aspectNamespace, bool createAsControl, bool exposeNullable)
    {
      bool multiAspect = false;
      MediaItemAspectMetadata metadata = GetMetadata(aspectType, out multiAspect);
      
      Dictionary<Type, bool> childAspects = new Dictionary<Type, bool>();
      if (childAspectTypes != null)
      {
        foreach (Type t in childAspectTypes)
        {
          bool multi;
          MediaItemAspectMetadata childMetadata = GetMetadata(t, out multi);
          childAspects[t] = multi;
        }
      }

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

        CreateProperty(attrName, attrType, _exposeNullable, spec.IsCollectionAttribute);
      }

      foreach (var childAspect in childAspects)
        CreateChildAspectProperty(childAspect.Key, childAspect.Value);

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
      if (childAspects.Any(kvp => kvp.Value))
        _usings.Add("using System.Linq;");
      _usings.Add("using MediaPortal.Common.General;");
      _usings.Add("using MediaPortal.Common.MediaManagement;");

      _usings.Add(string.Format("using {0};", aspectNamespace));
      if (_createAsControl)
      {
        _usings.Add("using MediaPortal.UI.SkinEngine.Controls.Visuals;");
        if (multiAspect)
          _usings.Add("using MediaPortal.Utilities.DeepCopy;");
      }

      // Constants
      _consts.Add("public static readonly ICollection<string> EMPTY_STRING_COLLECTION = new List<string>().AsReadOnly();");
      CreateChildAspectConsts(childAspects);

      // Common properties
      CreateProperty("MediaItem", typeof(MediaItem), _exposeNullable);
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

      if (multiAspect)
      {
        CreateProperty("AspectIndex", typeof(int), false);
        _propertyCreation.Add("_aspectIndexProperty.Attach(AspectIndexChanged);");
        CreateProperty("AspectCount", typeof(int), false);
      }

      AppendRegion(result, "Constants", _consts, false);
      AppendRegion(result, "Fields", _fields, false);
      AppendRegion(result, "Properties", _properties);

      List<string> ctors = new List<string> { string.Format("public {0}Wrapper()\r\n{{\r\n  {1}\r\n}}", aspectName, string.Join("\r\n  ", _propertyCreation.ToArray())) };
      AppendRegion(result, "Constructor", ctors);

      _members.Add(@"private void MediaItemChanged(AbstractProperty property, object oldvalue)
{
  Init(MediaItem);
}");

      if (multiAspect)
      {
        _members.Add(@"private void AspectIndexChanged(AbstractProperty property, object oldvalue)
{
  Init(MediaItem);
}");
        if (_createAsControl)
        {
          _members.Add(string.Format(@"public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
{{
  Detach();
  base.DeepCopy(source, copyManager);
  var aw = ({0}Wrapper)source;
  AspectIndex = aw.AspectIndex;
  Attach();
}}", aspectName));
          _members.Add(@"private void Attach()
{
  _aspectIndexProperty.Attach(AspectIndexChanged);
}");
          _members.Add(@"private void Detach()
{
  _aspectIndexProperty.Detach(AspectIndexChanged);
}");
        }
      }
      CreateMembers(aspectType, _members, childAspects, multiAspect);
      AppendRegion(result, "Members", _members);

      result.AppendLine("}"); // End of class
      result.AppendLine("\r\n}"); // End of namespace

      return result.ToString();
    }

    #region Members

    private void CreateMembers(Type aspectType, IList<string> members, Dictionary<Type, bool> childAspects, bool multiAspect)
    {
      string methodStub = null;
      string emptyStub = null;
      if (multiAspect == false)
      {
        methodStub = @"public void Init(MediaItem mediaItem)
{{
  SingleMediaItemAspect aspect;
  if (mediaItem == null ||!MediaItemAspect.TryGetAspect(mediaItem.Aspects, {1}.Metadata, out aspect))
  {{
     SetEmpty();
     return;
  }}

  {0}
}}";
        emptyStub = @"public void SetEmpty()
{{
  {0}
}}";
      }
      else
      {
        methodStub = @"public void Init(MediaItem mediaItem)
{{
  IList<MultipleMediaItemAspect> aspects;
  if (mediaItem == null || !MediaItemAspect.TryGetAspects(mediaItem.Aspects, {1}.Metadata, out aspects) ||
      AspectIndex < 0 || AspectIndex >= aspects.Count)
  {{
     SetEmpty();
     return;
  }}

  AspectCount = aspects.Count;
  {0}
}}";
        emptyStub = @"public void SetEmpty()
{{
  AspectCount = 0;
  {0}
}}";
      }
      List<string> initCommands = new List<string>();
      List<string> emptyCommands = new List<string>();

      foreach (FieldInfo fieldInfo in aspectType.GetFields())
      {
        if (!fieldInfo.Name.StartsWith("ATTR_"))
          continue;

        MediaItemAspectMetadata.AttributeSpecification spec = (MediaItemAspectMetadata.AttributeSpecification)fieldInfo.GetValue(null);

        string attrName = CreateSafePropertyName(spec.AttributeName);
        string typeName = BuildTypeName(spec.AttributeType, spec.IsCollectionAttribute, _exposeNullable);
        if (_valueTypes.Contains(typeName) && !_exposeNullable)
        {
          string varName = FirstLower(spec.AttributeName);
          if (multiAspect)
            initCommands.Add(string.Format("{0}? {1} = ({0}?) aspects[AspectIndex][{2}.{3}];", typeName, varName, aspectType.Name, fieldInfo.Name));
          else
            initCommands.Add(string.Format("{0}? {1} = ({0}?) aspect[{2}.{3}];", typeName, varName, aspectType.Name, fieldInfo.Name));
          initCommands.Add(string.Format("{0} = {1}.HasValue? {1}.Value : default({2});", attrName, varName, typeName));
        }
        else
        {
          string defaultValue = typeName == "IEnumerable<string>" ? " ?? EMPTY_STRING_COLLECTION" : "";
          if (multiAspect)
            initCommands.Add(string.Format("{0} = ({1}) aspects[AspectIndex][{2}.{3}]{4};", attrName, typeName, aspectType.Name, fieldInfo.Name, defaultValue));
          else
            initCommands.Add(string.Format("{0} = ({1}) aspect[{2}.{3}]{4};", attrName, typeName, aspectType.Name, fieldInfo.Name, defaultValue));
        }

        emptyCommands.Add(string.Format("{0} = {1};", attrName, GetEmptyValue(spec)));
      }

      List<string> childPopulationMembers = new List<string>();
      foreach (var childAspect in childAspects)
      {
        if (childAspect.Value)
        {
          initCommands.Add(string.Format("Add{0}s(mediaItem);", childAspect.Key.Name));
          emptyCommands.Add(string.Format("{0} = EMPTY_{1}_COLLECTION;", CreateChildPropertyName(childAspect.Key, true), childAspect.Key.Name.ToUpperInvariant()));
          childPopulationMembers.Add(CreateChildPopulationMethod(childAspect.Key));
        }
        else
        {
          string propertyName = CreateChildPropertyName(childAspect.Key, false);
          initCommands.Add(string.Format("{0} = mediaItem.Aspects.ContainsKey({1}.ASPECT_ID) ? new {1}() { MediaItem = mediaItem } : null;",
            propertyName, childAspect.Key.Name));
          emptyCommands.Add(string.Format("{0} = null;", propertyName));
        }
      }

      members.Add(string.Format(methodStub, string.Join("\r\n  ", initCommands.ToArray()), aspectType.Name));
      members.Add(string.Format(emptyStub, string.Join("\r\n  ", emptyCommands.ToArray())));
      foreach (string childPopulationMember in childPopulationMembers)
        members.Add(childPopulationMember);
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

    private void CreateChildAspectConsts(Dictionary<Type, bool> childAspects)
    {
      foreach (var kvp in childAspects.Where(kvp => kvp.Value))
        _consts.Add(string.Format("public static readonly ICollection<{0}Wrapper> EMPTY_{1}_COLLECTION = new List<{0}Wrapper>().AsReadOnly();", kvp.Key.Name, kvp.Key.Name.ToUpperInvariant()));
    }

    private void CreateProperty(string attrName, Type attrType, bool exposeNullable, bool isCollection = false)
    {
      attrName = CreateSafePropertyName(attrName);
      string publicValuePropertyName = attrName;
      string publicPropertyName = publicValuePropertyName + "Property";
      string propertyName = "_" + FirstLower(publicPropertyName);
      string typeName = BuildTypeName(attrType, isCollection, exposeNullable);
      _fields.Add(string.Format(FIELD_TEMPLATE, propertyName));
      _properties.Add(string.Format(BINDING_PROPERTY_TEMPLATE, publicPropertyName, propertyName));
      _properties.Add(string.Format(VALUE_PROPERTY_TEMPLATE, publicValuePropertyName, propertyName, typeName));
      if (_createAsControl)
        _propertyCreation.Add(string.Format(PROPERTY_CREATION_TEMPLATE_CONTROL, propertyName, typeName));
      else
        _propertyCreation.Add(string.Format(PROPERTY_CREATION_TEMPLATE, propertyName, typeName));
    }

    private void CreateChildAspectProperty(Type childType, bool isCollection)
    {
      string publicValuePropertyName = CreateChildPropertyName(childType, isCollection);
      string publicPropertyName = publicValuePropertyName + "Property";
      string propertyName = "_" + FirstLower(publicPropertyName);
      string typeName = isCollection ? string.Format("IEnumerable<{0}Wrapper>", childType.Name) : childType.Name + "Wrapper";
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

    private string CreateChildPropertyName(Type childType, bool isCollection)
    {
      string name = CreateSafePropertyName(childType.Name.Replace("Aspect", ""));
      if (isCollection)
        name += "s";
      return name;
    }

    private string CreateChildPopulationMethod(Type childType)
    {
      string stub = @"protected void Add{0}s(MediaItem mediaItem)
{{
  IList<MultipleMediaItemAspect> multiAspect;
  if (MediaItemAspect.TryGetAspects(mediaItem.Aspects, {0}.Metadata, out multiAspect))
    {1} = multiAspect.Select((a, i) => new {0}Wrapper() {{ AspectIndex = i, MediaItem = mediaItem }}).ToList();
  else
    {1} = EMPTY_{2}_COLLECTION;
}}";
      return string.Format(stub, childType.Name, CreateChildPropertyName(childType, true), childType.Name.ToUpperInvariant());
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

    public MediaItemAspectMetadata GetMetadata(Type type, out bool multiAspect)
    {
      multiAspect = false;
      FieldInfo field = type.GetField("Metadata");
      object metadata = field.GetValue(null);
      if (metadata is MultipleMediaItemAspectMetadata)
      {
        multiAspect = true;
      }
      return (MediaItemAspectMetadata)metadata;
    }

    #endregion
  }
}
