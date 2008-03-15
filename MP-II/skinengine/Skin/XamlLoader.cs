using System;
using System.Diagnostics;
using System.Reflection;

using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MyXaml.Core;
using SkinEngine.Controls.Bindings;
using SkinEngine.Controls.Visuals;
using SkinEngine.ElementRegistrations;

namespace SkinEngine.Skin
{                              
  /// <summary>
  /// This is the loader class for each XAML file.
  /// </summary>              
  public class XamlLoader
  {
    UIElement _lastElement;
    ResourceDictionary _lastDictionary;

    /// <summary>
    /// Loads the specified skin file using MyXaml
    /// and returns the root UIElement
    /// </summary>
    /// <param name="skinFile">The skin file.</param>
    /// <returns></returns>
    public object Load(string skinFile)
    {
      DateTime dt = DateTime.Now;
      string fullFileName = String.Format(@"skin\{0}\{1}", SkinContext.SkinName, skinFile);
      if (System.IO.File.Exists(skinFile))
      {
        fullFileName = skinFile;
      }
      using (Parser parser = new Parser())
      {
        parser.InstantiatePropertyDeclaration += new Parser.InstantiatePropertyDeclarationDlgt(parser_InstantiatePropertyDeclaration);
        parser.InstantiateFromQName += new Parser.InstantiateClassDlgt(parser_InstantiateFromQName);
        parser.PropertyDeclarationTest += new Parser.PropertyDeclarationTestDlgt(parser_PropertyDeclarationTest);
        parser.CustomTypeConvertor += new Parser.CustomTypeConverterDlgt(parser_CustomTypeConvertor);
        parser.OnGetResource += new Parser.GetResourceDlgt(parser_OnGetResource);
        parser.AddToCollection += new Parser.AddToCollectionDlgt(parser_AddToCollection);
        parser.OnSetContent += new Parser.SetContentDlg(parser_OnSetContent);
        parser.OnGetBinding += new Parser.GetBindingDlgt(parser_OnGetBinding);
        parser.OnImportNameSpace += new Parser.ImportNamespaceDlgt(parser_OnImportNameSpace);
        parser.OnGetTemplateBinding += new Parser.GetBindingDlgt(parser_OnGetTemplateBinding);
        object obj = parser.Instantiate(fullFileName, "*");
        TimeSpan ts = DateTime.Now - dt;
        ServiceScope.Get<ILogger>().Info("Xaml loaded {0} msec:{1}", skinFile, ts.TotalMilliseconds);
        return obj;
      }
    }


    public object Load(string skinFile, string tagName)
    {
      Trace.WriteLine("---load:" + skinFile);
      string fullFileName = String.Format(@"skin\{0}\{1}", SkinContext.SkinName, skinFile);
      if (System.IO.File.Exists(skinFile))
      {
        fullFileName = skinFile;
      }
      using (Parser parser = new Parser())
      {
        parser.InstantiatePropertyDeclaration += new Parser.InstantiatePropertyDeclarationDlgt(parser_InstantiatePropertyDeclaration);
        parser.InstantiateFromQName += new Parser.InstantiateClassDlgt(parser_InstantiateFromQName);
        parser.PropertyDeclarationTest += new Parser.PropertyDeclarationTestDlgt(parser_PropertyDeclarationTest);
        parser.CustomTypeConvertor += new Parser.CustomTypeConverterDlgt(parser_CustomTypeConvertor);
        parser.OnGetResource += new Parser.GetResourceDlgt(parser_OnGetResource);
        parser.AddToCollection += new Parser.AddToCollectionDlgt(parser_AddToCollection);
        parser.OnSetContent += new Parser.SetContentDlg(parser_OnSetContent);
        parser.OnGetBinding += new Parser.GetBindingDlgt(parser_OnGetBinding);
        parser.OnGetTemplateBinding += new Parser.GetBindingDlgt(parser_OnGetTemplateBinding);
        parser.OnImportNameSpace += new Parser.ImportNamespaceDlgt(parser_OnImportNameSpace);
        UIElement obj = (UIElement)parser.Instantiate(fullFileName, tagName);
        Trace.WriteLine("---------");
        return obj;
      }
    }

    void parser_OnImportNameSpace(object parser, object obj, string nameSpace)
    {
      //clr-namespace:Model;assembly=mymovies
      string[] parts = nameSpace.Split(new char[] { ';' });
      if (parts.Length != 2)
      {
        ServiceScope.Get<ILogger>().Info("XamlParser: invalid namespace declaration: {0}", nameSpace);
        return;
      }
      string className = parts[0].Substring(parts[0].IndexOf(":") + 1);
      string assemblyName = parts[1].Substring(parts[1].IndexOf("=") + 1);
      if (!SkinEngine.ModelManager.Instance.Contains(assemblyName, className))
      {
        SkinEngine.ModelManager.Instance.Load(assemblyName, className);
      }
      Model model = SkinEngine.ModelManager.Instance.GetModel(assemblyName, className);
      if (model == null)
      {
        ServiceScope.Get<ILogger>().Info("XamlParser: unknown model: {0}.{1}", assemblyName, className);
        return;
      }
      PropertyInfo info = obj.GetType().GetProperty("Context");
      if (info == null)
      {
        ServiceScope.Get<ILogger>().Info("XamlParser: object {0} does not have a Context property", obj);
        return;
      }
      MethodInfo methodInfo = info.GetSetMethod();
      if (methodInfo == null)
      {
        ServiceScope.Get<ILogger>().Info("XamlParser: object {0} does not have a Context set property", obj);
        return;
      }
      methodInfo.Invoke(obj, new object[] { model.Instance });
    }

    void parser_OnSetContent(object parser, object obj, object content)
    {
      if (obj is UIElement)
      {
        if (content is FrameworkElement)
        {
          UIElement element = (UIElement)obj;
          ContentPresenter contentPresenter = VisualTreeHelper.Instance.FindElementType(element, typeof(ContentPresenter)) as ContentPresenter;
          if (contentPresenter != null)
          {
            contentPresenter.Content = (FrameworkElement)content;
          }
        }
        else if (content is String)
        {
          UIElement element = (UIElement)obj;
          ContentPresenter contentPresenter = VisualTreeHelper.Instance.FindElementType(element, typeof(ContentPresenter)) as ContentPresenter;
          if (contentPresenter != null)
          {
            Label l = new Label();
            l.Text = (string)content;
            l.Font = "font12";
            contentPresenter.Content = l;
          }
        }
      }
    }

    object parser_OnGetTemplateBinding(object parser, object obj, string bindingExpression, PropertyInfo info)
    {
      if (obj is IBindingCollection)
      {
        Visual element = (Visual)obj; ;
        TemplateBinding b = new TemplateBinding();
        b.Expression = bindingExpression;
        b.PropertyInfo = info;

        IBindingCollection collection = (IBindingCollection)obj;
        collection.Add(b);
      }
      else
      {
        ServiceScope.Get<ILogger>().Info("XamlParser: class {0} does not implement IBindingCollection", obj);
      }
      return null;
    }

    object parser_OnGetBinding(object parser, object obj, string bindingExpression, PropertyInfo info)
    {
      if (obj is IBindingCollection)
      {
        Binding b = new Binding();
        b.Expression = bindingExpression;
        b.PropertyInfo = info;

        IBindingCollection collection = (IBindingCollection)obj;
        collection.Add(b);
      }
      else
      {
        ServiceScope.Get<ILogger>().Info("XamlParser: class {0} does not implement IBindingCollection", obj);
      }
      return null;
    }

    object parser_OnGetResource(object parser, object obj, string resourceName)
    {
      //Trace.WriteLine(String.Format("Get resource:{0}", resourceName));
      if (obj as UIElement != null)
      {
        UIElement elm = (UIElement)obj;
        object result = elm.FindResource(resourceName);
        ICloneable clone = result as ICloneable;
        if (clone != null)
        {
          return clone.Clone();
        }
        if (result != null)
        {
          Trace.WriteLine(String.Format("xaml loader type: {0} is not clonable", result));
          return result;
        }
      }
      if (_lastElement != null)
      {
        object result;
        UIElement element = _lastElement;
        do
        {
          result = element.FindResource(resourceName);
          if (result != null) break;
          element = element.VisualParent;
        } while (element != null);

        ICloneable clone = result as ICloneable;
        if (clone != null)
        {
          return clone.Clone();
        }
        if (result != null)
        {
          Trace.WriteLine(String.Format("xaml loader type: {0} is not clonable", result));
          return result;
        }
      }
      if (_lastDictionary != null)
      {
        if (_lastDictionary.Contains(resourceName))
        {
          object result = _lastDictionary[resourceName];
          ICloneable clone = result as ICloneable;
          if (clone != null)
          {
            return clone.Clone();
          }
          if (result != null)
          {
            // Trace.WriteLine(String.Format("xaml loader type:{0} is not clonable", result));
            return result;
          }
        }
      }
      ServiceScope.Get<ILogger>().Error("Resource: {0} not found", resourceName);
      return null;
    }

    void parser_AddToCollection(object parser, AddToCollectionEventArgs e)
    {
      if (e.Container as ResourceDictionary != null)
      {
        if ((e.Instance as ResourceDictionary) != null)
        {
          ResourceDictionary dictNew = (ResourceDictionary)e.Instance;
          object o = ResourceDictionaryCache.Instance.Get(dictNew.Source);


          ResourceDictionary dict = (ResourceDictionary)e.Container;
          dict.Merge((ResourceDictionary)o);
          e.Result = true;

          if (_lastDictionary == null)
            _lastDictionary = dict;
          return;
        }
        else
        {
          string key = e.Node.Attributes["Key"].Value;
          ResourceDictionary dict = (ResourceDictionary)e.Container;
          dict[key] = e.Instance;
          e.Result = true;
          if (_lastDictionary == null)
            _lastDictionary = dict;
        }
      }
    }

    /// <summary>
    /// Handles the CustomTypeConvertor event of the parser control.
    /// </summary>
    /// <param name="parser">The source of the event.</param>
    /// <param name="e">The <see cref="MyXaml.Core.CustomTypeEventArgs"/> instance containing the event data.</param>
    void parser_CustomTypeConvertor(object parser, CustomTypeEventArgs e)
    {
      e.Result = XamlTypeConverter.ConvertType(e.PropertyType, e.Value);
    }

    /// <summary>
    /// Handles the PropertyDeclarationTest event of the parser control.
    /// </summary>
    /// <param name="parser">The source of the event.</param>
    /// <param name="e">The <see cref="MyXaml.Core.PropertyDeclarationTestEventArgs"/> instance containing the event data.</param>
    void parser_PropertyDeclarationTest(object parser, PropertyDeclarationTestEventArgs e)
    {
      e.Result = IsKnownObject(e.ChildQualifiedName);
    }

    /// <summary>
    /// Handles the InstantiatePropertyDeclaration event of the parser control.
    /// </summary>
    /// <param name="parser">The source of the event.</param>
    /// <param name="e">The <see cref="MyXaml.Core.InstantiatePropertyDeclarationEventArgs"/> instance containing the event data.</param>
    void parser_InstantiatePropertyDeclaration(object parser, InstantiatePropertyDeclarationEventArgs e)
    {
      e.Result = CreateObject(e.ChildQualifiedName);
    }


    /// <summary>
    /// Handles the InstantiateFromQName event of the parser control.
    /// </summary>
    /// <param name="parser">The source of the event.</param>
    /// <param name="e">The <see cref="MyXaml.Core.InstantiateClassEventArgs"/> instance containing the event data.</param>
    void parser_InstantiateFromQName(object parser, InstantiateClassEventArgs e)
    {
      e.Result = CreateObject(e.AssemblyQualifiedName);
    }

    /// <summary>
    /// Determines whether name is a known class name or not
    /// </summary>
    /// <param name="name">The name.</param>
    /// <returns>
    /// 	<c>true</c> if name is known classname; otherwise, <c>false</c>.
    /// </returns>
    bool IsKnownObject(string name)
    {                              
      return XamlElements.ObjectClassRegistrations.ContainsKey(name);
    }

    /// <summary>
    /// Gets a new object which classname=name.
    /// </summary>
    /// <param name="name">The class name.</param>
    /// <returns>object</returns>
    object CreateObject(string className)
    {
      Type t = XamlElements.ObjectClassRegistrations[className];
      object result = Activator.CreateInstance(t);
      if (result is UIElement)
        _lastElement = result as UIElement;
      if (result is ResourceDictionary)
        _lastDictionary = result as ResourceDictionary;    
      return result;
    }
  }
}
