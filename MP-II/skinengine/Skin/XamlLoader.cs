using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;

using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MyXaml.Core;
using SlimDX;
using SkinEngine.Controls.Bindings;
using SkinEngine.Controls.Brushes;
using SkinEngine.Controls.Panels;
using SkinEngine.Controls.Transforms;
using SkinEngine.Controls.Visuals;
using SkinEngine.TypeConverters;

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
    /// Registration for all elements the loader can create from a XAML file.
    /// </summary>
    protected static IDictionary<string, Type> ObjectClassRegistrations = new Dictionary<string, Type>();
    static XamlLoader()
    {                            
      // Panels
      ObjectClassRegistrations.Add("DockPanel", typeof(SkinEngine.Controls.Panels.DockPanel));
      ObjectClassRegistrations.Add("StackPanel", typeof(SkinEngine.Controls.Panels.StackPanel));
      ObjectClassRegistrations.Add("VirtualizingStackPanel", typeof(SkinEngine.Controls.Panels.VirtualizingStackPanel));
      ObjectClassRegistrations.Add("Canvas", typeof(SkinEngine.Controls.Panels.Canvas));
      ObjectClassRegistrations.Add("Grid", typeof(SkinEngine.Controls.Panels.Grid));
      ObjectClassRegistrations.Add("RowDefinition", typeof(SkinEngine.Controls.Panels.RowDefinition));
      ObjectClassRegistrations.Add("ColumnDefinition", typeof(SkinEngine.Controls.Panels.ColumnDefinition));
      ObjectClassRegistrations.Add("GridLength", typeof(SkinEngine.Controls.Panels.GridLength));
      ObjectClassRegistrations.Add("WrapPanel", typeof(SkinEngine.Controls.Panels.WrapPanel));

      // Visuals
      ObjectClassRegistrations.Add("Border", typeof(SkinEngine.Controls.Visuals.Border));
      ObjectClassRegistrations.Add("Image", typeof(SkinEngine.Controls.Visuals.Image));
      ObjectClassRegistrations.Add("Button", typeof(SkinEngine.Controls.Visuals.Button));
      ObjectClassRegistrations.Add("CheckBox", typeof(SkinEngine.Controls.Visuals.CheckBox));
      ObjectClassRegistrations.Add("Label", typeof(SkinEngine.Controls.Visuals.Label));
      ObjectClassRegistrations.Add("Rectangle", typeof(SkinEngine.Controls.Visuals.Rectangle));
      ObjectClassRegistrations.Add("Ellipse", typeof(SkinEngine.Controls.Visuals.Ellipse));
      ObjectClassRegistrations.Add("Line", typeof(SkinEngine.Controls.Visuals.Line));
      ObjectClassRegistrations.Add("Polygon", typeof(SkinEngine.Controls.Visuals.Polygon));
      ObjectClassRegistrations.Add("Path", typeof(SkinEngine.Controls.Visuals.Path));
      ObjectClassRegistrations.Add("ListView", typeof(SkinEngine.Controls.Visuals.ListView));
      ObjectClassRegistrations.Add("ContentPresenter", typeof(SkinEngine.Controls.Visuals.ContentPresenter));
      ObjectClassRegistrations.Add("ScrollContentPresenter", typeof(SkinEngine.Controls.Visuals.ScrollContentPresenter));
      ObjectClassRegistrations.Add("ProgressBar", typeof(SkinEngine.Controls.Visuals.ProgressBar));
      ObjectClassRegistrations.Add("KeyBinding", typeof(SkinEngine.Controls.Visuals.KeyBinding));
      ObjectClassRegistrations.Add("TreeView", typeof(SkinEngine.Controls.Visuals.TreeView));
      ObjectClassRegistrations.Add("TreeViewItem", typeof(SkinEngine.Controls.Visuals.TreeViewItem));
      ObjectClassRegistrations.Add("ItemsPresenter", typeof(SkinEngine.Controls.Visuals.ItemsPresenter));
      ObjectClassRegistrations.Add("DataTemplate", typeof(SkinEngine.Controls.Visuals.DataTemplate));
      ObjectClassRegistrations.Add("StyleSelector", typeof(SkinEngine.Controls.Visuals.StyleSelector));
      ObjectClassRegistrations.Add("DataTemplateSelector", typeof(SkinEngine.Controls.Visuals.DataTemplateSelector));
      ObjectClassRegistrations.Add("ScrollViewer", typeof(SkinEngine.Controls.Visuals.ScrollViewer));
      ObjectClassRegistrations.Add("Resources", typeof(SkinEngine.Controls.Visuals.ResourceDictionary));
      ObjectClassRegistrations.Add("ResourceDictionary", typeof(SkinEngine.Controls.Visuals.ResourceDictionary));
      
      // Brushes
      ObjectClassRegistrations.Add("SolidColorBrush", typeof(SkinEngine.Controls.Brushes.SolidColorBrush));
      ObjectClassRegistrations.Add("LinearGradientBrush", typeof(SkinEngine.Controls.Brushes.LinearGradientBrush));
      ObjectClassRegistrations.Add("RadialGradientBrush", typeof(SkinEngine.Controls.Brushes.RadialGradientBrush));
      ObjectClassRegistrations.Add("ImageBrush", typeof(SkinEngine.Controls.Brushes.ImageBrush));
      ObjectClassRegistrations.Add("VisualBrush", typeof(SkinEngine.Controls.Brushes.VisualBrush));
      ObjectClassRegistrations.Add("VideoBrush", typeof(SkinEngine.Controls.Brushes.VideoBrush));
      ObjectClassRegistrations.Add("GradientStop", typeof(SkinEngine.Controls.Brushes.GradientStop));

      // Animations
      ObjectClassRegistrations.Add("ColorAnimation", typeof(SkinEngine.Controls.Animations.ColorAnimation));
      ObjectClassRegistrations.Add("DoubleAnimation", typeof(SkinEngine.Controls.Animations.DoubleAnimation));
      ObjectClassRegistrations.Add("PointAnimation", typeof(SkinEngine.Controls.Animations.PointAnimation));
      ObjectClassRegistrations.Add("Storyboard", typeof(SkinEngine.Controls.Animations.Storyboard));
      ObjectClassRegistrations.Add("ColorAnimationUsingKeyFrames", typeof(SkinEngine.Controls.Animations.ColorAnimationUsingKeyFrames));
      ObjectClassRegistrations.Add("DoubleAnimationUsingKeyFrames", typeof(SkinEngine.Controls.Animations.DoubleAnimationUsingKeyFrames));
      ObjectClassRegistrations.Add("PointAnimationUsingKeyFrames", typeof(SkinEngine.Controls.Animations.PointAnimationUsingKeyFrames));
      ObjectClassRegistrations.Add("SplineColorKeyFrame", typeof(SkinEngine.Controls.Animations.SplineColorKeyFrame));
      ObjectClassRegistrations.Add("SplineDoubleKeyFrame", typeof(SkinEngine.Controls.Animations.SplineDoubleKeyFrame));
      ObjectClassRegistrations.Add("SplinePointKeyFrame", typeof(SkinEngine.Controls.Animations.SplinePointKeyFrame));

      // Triggers
      ObjectClassRegistrations.Add("EventTrigger", typeof(SkinEngine.Controls.Visuals.Triggers.EventTrigger));
      ObjectClassRegistrations.Add("Trigger", typeof(SkinEngine.Controls.Visuals.Triggers.Trigger));
      ObjectClassRegistrations.Add("BeginStoryboard", typeof(SkinEngine.Controls.Visuals.Triggers.BeginStoryboard));
      ObjectClassRegistrations.Add("StopStoryboard", typeof(SkinEngine.Controls.Visuals.Triggers.StopStoryboard));

      // Transforms
      ObjectClassRegistrations.Add("TransformGroup", typeof(SkinEngine.Controls.Transforms.TransformGroup));
      ObjectClassRegistrations.Add("ScaleTransform", typeof(SkinEngine.Controls.Transforms.ScaleTransform));
      ObjectClassRegistrations.Add("SkewTransform", typeof(SkinEngine.Controls.Transforms.SkewTransform));
      ObjectClassRegistrations.Add("RotateTransform", typeof(SkinEngine.Controls.Transforms.RotateTransform));
      ObjectClassRegistrations.Add("TranslateTransform", typeof(SkinEngine.Controls.Transforms.TranslateTransform));

      // Styles
      ObjectClassRegistrations.Add("Style", typeof(SkinEngine.Controls.Visuals.Styles.Style));
      ObjectClassRegistrations.Add("Setter", typeof(SkinEngine.Controls.Visuals.Styles.Setter));
      ObjectClassRegistrations.Add("ControlTemplate", typeof(SkinEngine.Controls.Visuals.Styles.ControlTemplate));
      ObjectClassRegistrations.Add("ItemsPanelTemplate", typeof(SkinEngine.Controls.Visuals.ItemsPanelTemplate));
      ObjectClassRegistrations.Add("CommandGroup", typeof(SkinEngine.Controls.Bindings.CommandGroup));
      ObjectClassRegistrations.Add("InvokeCommand", typeof(SkinEngine.Controls.Bindings.InvokeCommand));
      ObjectClassRegistrations.Add("Include", typeof(SkinEngine.Skin.Include));
    }

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

    public object ConvertType(Type propertyType, object propertyValue)
    {
      CustomTypeEventArgs e = new CustomTypeEventArgs();
      e.PropertyType = propertyType;
      e.Value = propertyValue;
      e.Result = propertyValue;
      parser_CustomTypeConvertor(null, e);
      return e.Result;

    }

    /// <summary>
    /// Handles the CustomTypeConvertor event of the parser control.
    /// </summary>
    /// <param name="parser">The source of the event.</param>
    /// <param name="e">The <see cref="MyXaml.Core.CustomTypeEventArgs"/> instance containing the event data.</param>
    void parser_CustomTypeConvertor(object parser, CustomTypeEventArgs e)
    {
      if (e.PropertyType == typeof(Transform))
      {
        string v = e.Value.ToString();
        string[] parts = v.Split(new char[] { ',' });
        if (parts.Length == 6)
        {
          float[] f = new float[parts.Length];
          for (int i = 0; i < parts.Length; ++i)
          {
            f[i] = BasicTypeConverter.Convert2Float(parts[i]);
          }
          System.Drawing.Drawing2D.Matrix matrix2d = new System.Drawing.Drawing2D.Matrix(f[0], f[1], f[2], f[3], f[4], f[5]);
          Static2dMatrix matrix = new Static2dMatrix();
          matrix.Set2DMatrix(matrix2d);
          e.Result = matrix;
        }
      }
      if (e.PropertyType == typeof(VisibilityEnum))
      {
        string v = e.Value.ToString();
        if (v == "Collapsed") e.Result = VisibilityEnum.Collapsed;
        if (v == "Hidden") e.Result = VisibilityEnum.Hidden;
        if (v == "Visible") e.Result = VisibilityEnum.Visible;
      }
      if (e.PropertyType == typeof(HorizontalAlignmentEnum))
      {
        string v = e.Value.ToString();
        if (v == "Left") e.Result = HorizontalAlignmentEnum.Left;
        if (v == "Right") e.Result = HorizontalAlignmentEnum.Right;
        if (v == "Center") e.Result = HorizontalAlignmentEnum.Center;
        if (v == "Stretch") e.Result = HorizontalAlignmentEnum.Stretch;
      }
      if (e.PropertyType == typeof(VerticalAlignmentEnum))
      {
        string v = e.Value.ToString();
        if (v == "Bottom") e.Result = VerticalAlignmentEnum.Bottom;
        if (v == "Top") e.Result = VerticalAlignmentEnum.Top;
        if (v == "Center") e.Result = VerticalAlignmentEnum.Center;
        if (v == "Stretch") e.Result = VerticalAlignmentEnum.Stretch;
      }
      if (e.PropertyType == typeof(Vector2))
      {
        e.Result = Convert2Vector2(e.Value.ToString());
      }
      else if (e.PropertyType == typeof(Vector3))
      {
        e.Result = Convert2Vector3(e.Value.ToString());
      }
      else if (e.PropertyType == typeof(Vector4))
      {
        e.Result = Convert2Vector4(e.Value.ToString());
      }
      else if (e.PropertyType == typeof(Brush))
      {
        SolidColorBrush b = new SolidColorBrush();
        b.Color = (System.Drawing.Color)TypeDescriptor.GetConverter(typeof(System.Drawing.Color)).ConvertFromString(e.Value.ToString());
        e.Result = b;
      }
      else if (e.PropertyType == typeof(PointCollection))
      {
        PointCollection coll = new PointCollection();
        string text = e.Value.ToString();
        string[] parts = text.Split(new char[] { ',', ' ' });
        for (int i = 0; i < parts.Length; i += 2)
        {
          System.Drawing.Point p = new System.Drawing.Point(Int32.Parse(parts[i]), Int32.Parse(parts[i + 1]));
          coll.Add(p);
        }
        e.Result = coll;
      }
      else if (e.PropertyType == typeof(GridLength))
      {
        string text = e.Value.ToString();
        if (text == "Auto")
        {
          e.Result = new GridLength(GridUnitType.Star, 1.0);
        }
        else if (text.IndexOf('*') < 0)
        {
          double v = double.Parse(text);
          e.Result = new GridLength(GridUnitType.Pixel, v);
        }
        else
        {
          int pos = text.IndexOf('*');
          text = text.Substring(0, pos);
          double percent = BasicTypeConverter.Convert2Double(text);
          e.Result = new GridLength(GridUnitType.Star, percent);
        }
      }
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
      e.Result = GetObject(e.ChildQualifiedName);
    }


    /// <summary>
    /// Handles the InstantiateFromQName event of the parser control.
    /// </summary>
    /// <param name="parser">The source of the event.</param>
    /// <param name="e">The <see cref="MyXaml.Core.InstantiateClassEventArgs"/> instance containing the event data.</param>
    void parser_InstantiateFromQName(object parser, InstantiateClassEventArgs e)
    {
      e.Result = GetObject(e.AssemblyQualifiedName);
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
      return ObjectClassRegistrations.ContainsKey(name);
    }

    /// <summary>
    /// Gets a new object which classname=name.
    /// </summary>
    /// <param name="name">The class name.</param>
    /// <returns>object</returns>
    object GetObject(string name)
    {
      Type t = ObjectClassRegistrations[name];
      object result = Activator.CreateInstance(t);
      if (result is UIElement)
        _lastElement = result as UIElement;
      if (result is ResourceDictionary)
        _lastDictionary = result as ResourceDictionary;    
      return result;
    }

    /// <summary>
    /// Converts a string to a <see cref="Vector2"/>.
    /// </summary>
    /// <param name="coordsString">The coordinates in "0.2,0.4" format. This method
    /// will fill as many coordinates in the result vector as specified in the
    /// comma separated string. So the string "3.5,7.2" will result in a vector (3.5, 7.2),
    /// the string "5.6" will result in a vector (5.6, 0),
    /// an empty or a <code>null</code> string will result in a vector (0, 0).</param>
    /// <returns>New <see cref="Vector2"/> instance with the specified coordinates,
    /// never <code>null</code>.</returns>
    protected static Vector2 Convert2Vector2(string coordsString)
    {
      if (coordsString == null)
      {
        return new Vector2(0, 0);
      }
      Vector2 vec = new Vector2();
      string[] coords = coordsString.Split(new char[] { ',' });
      if (coords.Length > 0)
      {
        vec.X = BasicTypeConverter.Convert2Float(coords[0]);
      }
      if (coords.Length > 1)
      {
        vec.Y = BasicTypeConverter.Convert2Float(coords[1]);
      }
      return vec;
    }

    /// <summary>
    /// Converts a string to a <see cref="Vector3"/>.
    /// </summary>
    /// <param name="coordsString">The coordinates in "0.2,0.4,0.1" format. This method
    /// will fill as many coordinates in the result vector as specified in the
    /// comma separated string. So the string "3.5,7.2,5.2" will result in a vector (3.5, 7.2, 5.2),
    /// the string "5.6" will result in a vector (5.6, 0, 0),
    /// an empty or a <code>null</code> string will result in a vector (0, 0, 0).</param>
    /// <returns>New <see cref="Vector3"/> instance with the specified coordinates,
    /// never <code>null</code>.</returns>
    protected static Vector3 Convert2Vector3(string coordsString)
    {
      if (coordsString == null)
      {
        return new Vector3(0, 0, 0);
      }
      Vector3 vec = new Vector3();
      string[] coords = coordsString.Split(new char[] { ',' });
      if (coords.Length > 0)
      {
        vec.X = BasicTypeConverter.Convert2Float(coords[0]);
      }
      if (coords.Length > 1)
      {
        vec.Y = BasicTypeConverter.Convert2Float(coords[1]);
      }
      if (coords.Length > 2)
      {
        vec.Z = BasicTypeConverter.Convert2Float(coords[2]);
      }
      return vec;
    }

    /// <summary>
    /// Converts a string to a <see cref="Vector4"/>.
    /// </summary>
    /// <param name="coordsString">The coordinates in "0.2,0.4,0.1,0.6" format. This method
    /// will fill as many coordinates in the result vector as specified in the
    /// comma separated string. So the string "3.5,7.2,5.2,2.8" will result in a
    /// vector (3.5, 7.2, 5.2, 2.8),
    /// the string "5.6" will result in a vector (5.6, 0, 0, 0),
    /// an empty or a <code>null</code> string will result in a vector (0, 0, 0, 0).</param>
    /// <returns>New <see cref="Vector4"/> instance with the specified coordinates,
    /// never <code>null</code>.</returns>
    protected static Vector4 Convert2Vector4(string coordsString)
    {
      if (coordsString == null)
      {
        return new Vector4(0, 0, 0, 0);
      }
      Vector4 vec = new Vector4();
      string[] coords = coordsString.Split(new char[] { ',' });
      if (coords.Length > 0)
      {
        vec.X = BasicTypeConverter.Convert2Float(coords[0]);
      }
      if (coords.Length > 1)
      {
        vec.Y = BasicTypeConverter.Convert2Float(coords[1]);
      }
      if (coords.Length > 2)
      {
        vec.W = BasicTypeConverter.Convert2Float(coords[2]);
      }
      if (coords.Length > 3)
      {
        vec.Z = BasicTypeConverter.Convert2Float(coords[3]);
      }
      return vec;
    }
  }
}
