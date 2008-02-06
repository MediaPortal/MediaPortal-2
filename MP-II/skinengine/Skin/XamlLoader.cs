using System;
using System.Reflection;
using System.Diagnostics;
using System.ComponentModel;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using MyXaml.Core;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using SkinEngine.Controls.Animations;
using SkinEngine.Controls.Brushes;
using SkinEngine.Controls.Panels;
using SkinEngine.Controls.Transforms;
using SkinEngine.Controls.Visuals;
using SkinEngine.Controls.Visuals.Triggers;
using SkinEngine.Controls.Bindings;

using SlimDX;
using SlimDX.Direct3D;
using SlimDX.Direct3D9;
namespace SkinEngine.Skin
{
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
        ServiceScope.Get<ILogger>().Info("XamlParser: invalid namespace declaration:{0}", nameSpace);
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
        ServiceScope.Get<ILogger>().Info("XamlParser: unknown model :{0}.{1}", assemblyName, className);
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
          Trace.WriteLine(String.Format("xaml loader type:{0} is not clonable", result));
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
          Trace.WriteLine(String.Format("xaml loader type:{0} is not clonable", result));
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
            Trace.WriteLine(String.Format("xaml loader type:{0} is not clonable", result));
            return result;
          }
        }
      }
      ServiceScope.Get<ILogger>().Error("Resource:{0} not found", resourceName);
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
            f[i] = GetFloat(parts[i]);
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
        e.Result = GetVector2(e.Value.ToString());
      }
      else if (e.PropertyType == typeof(Vector3))
      {
        e.Result = GetVector3(e.Value.ToString());
      }
      else if (e.PropertyType == typeof(Vector4))
      {
        e.Result = GetVector4(e.Value.ToString());
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
          double percent = GetDouble(text);
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
      //panels
      if (name == "DockPanel")
        return true;
      else if (name == "StackPanel")
        return true;
      else if (name == "VirtualizingStackPanel")
        return true;
      else if (name == "Canvas")
        return true;
      else if (name == "Grid")
        return true;
      else if (name == "RowDefinition")
        return true;
      else if (name == "ColumnDefinition")
        return true;
      else if (name == "GridLength")
        return true;
      else if (name == "WrapPanel")
        return true;

      //visuals
      else if (name == "Border")
        return true;
      else if (name == "Image")
        return true;
      else if (name == "Button")
        return true;
      else if (name == "Label")
        return true;
      else if (name == "Rectangle")
        return true;
      else if (name == "Ellipse")
        return true;
      else if (name == "Line")
        return true;
      else if (name == "Polygon")
        return true;
      else if (name == "Path")
        return true;
      else if (name == "CheckBox")
        return true;
      else if (name == "ListView")
        return true;
      else if (name == "DataTemplate")
        return true;
      else if (name == "StyleSelector")
        return true;
      else if (name == "DataTemplateSelector")
        return true;
      else if (name == "ContentPresenter")
        return true;
      else if (name == "ScrollContentPresenter")
        return true;
      else if (name == "ScrollViewer")
        return true;
      else if (name == "Resources")
        return true;
      else if (name == "ResourceDictionary")
        return true;
      else if (name == "ProgressBar")
        return true;
      else if (name == "KeyBinding")
        return true;
      else if (name == "TreeView")
        return true;
      else if (name == "TreeViewItem")
        return true;
      else if (name == "ItemsPresenter")
        return true;


      //brushes
      else if (name == "SolidColorBrush")
        return true;
      else if (name == "LinearGradientBrush")
        return true;
      else if (name == "RadialGradientBrush")
        return true;
      else if (name == "ImageBrush")
        return true;
      else if (name == "VisualBrush")
        return true;
      else if (name == "VideoBrush")
        return true;
      else if (name == "GradientStop")
        return true;

      //animations
      else if (name == "ColorAnimation")
        return true;
      else if (name == "DoubleAnimation")
        return true;
      else if (name == "PointAnimation")
        return true;
      else if (name == "Storyboard")
        return true;
      else if (name == "ColorAnimationUsingKeyFrames")
        return true;
      else if (name == "DoubleAnimationUsingKeyFrames")
        return true;
      else if (name == "PointAnimationUsingKeyFrames")
        return true;
      else if (name == "SplineColorKeyFrame")
        return true;
      else if (name == "SplineDoubleKeyFrame")
        return true;
      else if (name == "SplinePointKeyFrame")
        return true;

      //triggers
      else if (name == "EventTrigger")
        return true;
      else if (name == "Trigger")
        return true;
      else if (name == "BeginStoryboard")
        return true;
      else if (name == "StopStoryboard")
        return true;

      //Transforms
      else if (name == "TransformGroup")
        return true;
      else if (name == "ScaleTransform")
        return true;
      else if (name == "SkewTransform")
        return true;
      else if (name == "RotateTransform")
        return true;
      else if (name == "TranslateTransform")
        return true;

      //Styles
      else if (name == "Style")
        return true;
      else if (name == "Setter")
        return true;
      else if (name == "ControlTemplate")
        return true;
      else if (name == "ItemsPanelTemplate")
        return true;

      else if (name == "CommandGroup")
        return true;
      else if (name == "InvokeCommand")
        return true;
      return false;
    }
    /// <summary>
    /// Gets a new object which classname=name.
    /// </summary>
    /// <param name="name">The class name.</param>
    /// <returns>object</returns>
    object GetObject(string name)
    {
      //panels
      if (name == "DockPanel")
      {
        _lastElement = new DockPanel();
        return _lastElement;
      }
      else if (name == "StackPanel")
      {
        _lastElement = new StackPanel();
        return _lastElement;
      }
      else if (name == "VirtualizingStackPanel")
      {
        _lastElement = new VirtualizingStackPanel();
        return _lastElement;
      }
      else if (name == "Canvas")
      {
        _lastElement = new Canvas();
        return _lastElement;
      }
      else if (name == "Grid")
      {
        _lastElement = new Grid();
        return _lastElement;
      }
      else if (name == "RowDefinition")
        return new RowDefinition();
      else if (name == "ColumnDefinition")
        return new ColumnDefinition();
      else if (name == "GridLength")
        return new GridLength();
      else if (name == "WrapPanel")
      {
        _lastElement = new WrapPanel();
        return _lastElement;
      }


      //visuals
      else if (name == "Border")
      {
        _lastElement = new Border();
        return _lastElement;
      }
      else if (name == "Image")
      {
        _lastElement = new Image();
        return _lastElement;
      }
      else if (name == "Button")
      {
        _lastElement = new Button();
        return _lastElement;
      }
      else if (name == "CheckBox")
      {
        _lastElement = new CheckBox();
        return _lastElement;
      }
      else if (name == "Label")
      {
        _lastElement = new Label();
        return _lastElement;
      }
      else if (name == "Rectangle")
      {
        _lastElement = new Rectangle();
        return _lastElement;
      }
      else if (name == "Ellipse")
      {
        _lastElement = new Ellipse();
        return _lastElement;
      }
      else if (name == "Line")
      {
        _lastElement = new SkinEngine.Controls.Visuals.Line();
        return _lastElement;
      }
      else if (name == "Polygon")
      {
        _lastElement = new SkinEngine.Controls.Visuals.Polygon();
        return _lastElement;
      }
      else if (name == "Path")
      {
        _lastElement = new SkinEngine.Controls.Visuals.Path();
        return _lastElement;
      }
      else if (name == "ListView")
      {
        _lastElement = new SkinEngine.Controls.Visuals.ListView();
        return _lastElement;
      }
      else if (name == "ContentPresenter")
      {
        _lastElement = new SkinEngine.Controls.Visuals.ContentPresenter();
        return _lastElement;
      }
      else if (name == "ScrollContentPresenter")
      {
        _lastElement = new SkinEngine.Controls.Visuals.ScrollContentPresenter();
        return _lastElement;
      }
      else if (name == "ProgressBar")
      {
        _lastElement = new SkinEngine.Controls.Visuals.ProgressBar();
        return _lastElement;
      }
      else if (name == "KeyBinding")
      {
        return new SkinEngine.Controls.Visuals.KeyBinding();
      }
      else if (name == "TreeView")
      {
        _lastElement = new SkinEngine.Controls.Visuals.TreeView();
        return _lastElement;
      }
      else if (name == "TreeViewItem")
      {
        _lastElement = new SkinEngine.Controls.Visuals.TreeViewItem();
        return _lastElement;
      }
      else if (name == "ItemsPresenter")
      {
        _lastElement = new SkinEngine.Controls.Visuals.ItemsPresenter();
        return _lastElement;
      }
      else if (name == "DataTemplate")
        return new SkinEngine.Controls.Visuals.DataTemplate();
      else if (name == "StyleSelector")
        return new SkinEngine.Controls.Visuals.StyleSelector();
      else if (name == "DataTemplateSelector")
        return new SkinEngine.Controls.Visuals.DataTemplateSelector();
      else if (name == "ScrollViewer")
        return new SkinEngine.Controls.Visuals.ScrollViewer();
      else if (name == "Resources")
        return new ResourceDictionary();
      else if (name == "ResourceDictionary")
      {
        _lastDictionary = new ResourceDictionary();
        return _lastDictionary;
      }

      //brushes
      else if (name == "SolidColorBrush")
        return new SolidColorBrush();
      else if (name == "LinearGradientBrush")
        return new LinearGradientBrush();
      else if (name == "RadialGradientBrush")
        return new RadialGradientBrush();
      else if (name == "ImageBrush")
        return new ImageBrush();
      else if (name == "VisualBrush")
        return new VisualBrush();
      else if (name == "VideoBrush")
        return new VideoBrush();
      else if (name == "GradientStop")
        return new GradientStop();

      //animations
      else if (name == "ColorAnimation")
        return new ColorAnimation();
      else if (name == "DoubleAnimation")
        return new DoubleAnimation();
      else if (name == "PointAnimation")
        return new PointAnimation();
      else if (name == "Storyboard")
        return new Storyboard();
      else if (name == "ColorAnimationUsingKeyFrames")
        return new ColorAnimationUsingKeyFrames();
      else if (name == "DoubleAnimationUsingKeyFrames")
        return new DoubleAnimationUsingKeyFrames();
      else if (name == "PointAnimationUsingKeyFrames")
        return new PointAnimationUsingKeyFrames();
      else if (name == "SplineColorKeyFrame")
        return new SplineColorKeyFrame();
      else if (name == "SplineDoubleKeyFrame")
        return new SplineDoubleKeyFrame();
      else if (name == "SplinePointKeyFrame")
        return new SplinePointKeyFrame();

      //triggers
      else if (name == "EventTrigger")
        return new EventTrigger();
      else if (name == "Trigger")
        return new Trigger();
      else if (name == "BeginStoryboard")
        return new BeginStoryboard();
      else if (name == "StopStoryboard")
        return new StopStoryboard();

      //Transforms
      else if (name == "TransformGroup")
        return new TransformGroup();
      else if (name == "ScaleTransform")
        return new ScaleTransform();
      else if (name == "SkewTransform")
        return new SkewTransform();
      else if (name == "RotateTransform")
        return new RotateTransform();
      else if (name == "TranslateTransform")
        return new TranslateTransform();

      //Styles
      else if (name == "Style")
        return new SkinEngine.Controls.Visuals.Styles.Style();
      else if (name == "Setter")
        return new SkinEngine.Controls.Visuals.Styles.Setter();
      else if (name == "ControlTemplate")
        return new SkinEngine.Controls.Visuals.Styles.ControlTemplate();
      else if (name == "ItemsPanelTemplate")
        return new SkinEngine.Controls.Visuals.ItemsPanelTemplate();
      else if (name == "CommandGroup")
        return new SkinEngine.Controls.Bindings.CommandGroup();
      else if (name == "InvokeCommand")
        return new SkinEngine.Controls.Bindings.InvokeCommand();
      return null;
    }

    /// <summary>
    /// converts a string to a vector2
    /// </summary>
    /// <param name="position">The position in '0.2,0.4' format.</param>
    /// <returns></returns>
    protected Vector2 GetVector2(string position)
    {
      if (position == null)
      {
        return new Vector2(0, 0);
      }
      Vector2 vec = new Vector2();
      string[] coords = position.Split(new char[] { ',' });
      if (coords.Length > 0)
      {
        vec.X = GetFloat(coords[0]);
      }
      if (coords.Length > 1)
      {
        vec.Y = GetFloat(coords[1]);
      }
      return vec;
    }
    /// <summary>
    /// converts a string into a vector3 format
    /// </summary>
    /// <param name="position">The position in '0.2,0.3,0.4' format</param>
    /// <returns></returns>
    protected Vector3 GetVector3(string position)
    {
      if (position == null)
      {
        return new Vector3(0, 0, 0);
      }
      Vector3 vec = new Vector3();
      string[] coords = position.Split(new char[] { ',' });
      if (coords.Length > 0)
      {
        vec.X = GetFloat(coords[0]);
      }
      if (coords.Length > 1)
      {
        vec.Y = GetFloat(coords[1]);
      }
      if (coords.Length > 2)
      {
        vec.Z = GetFloat(coords[2]);
      }
      return vec;
    }
    /// <summary>
    /// converts a string into a vector4 format
    /// </summary>
    /// <param name="position">The position in '0.2,0.3,0.4,0.5' format</param>
    /// <returns></returns>
    protected Vector4 GetVector4(string position)
    {
      if (position == null)
      {
        return new Vector4(0, 0, 0, 0);
      }
      Vector4 vec = new Vector4();
      string[] coords = position.Split(new char[] { ',' });
      if (coords.Length > 0)
      {
        vec.X = GetFloat(coords[0]);
      }
      if (coords.Length > 1)
      {
        vec.Y = GetFloat(coords[1]);
      }
      if (coords.Length > 2)
      {
        vec.W = GetFloat(coords[2]);
      }
      if (coords.Length > 3)
      {
        vec.Z = GetFloat(coords[3]);
      }
      return vec;
    }

    /// <summary>
    /// converts a string into a float.
    /// </summary>
    /// <param name="floatString">The  string.</param>
    /// <returns>float</returns>
    public float GetFloat(string floatString)
    {
      float test = 12.03f;
      string comma = test.ToString();
      bool replaceCommas = (comma.IndexOf(",") >= 0);
      if (replaceCommas)
      {
        floatString = floatString.Replace(".", ",");
      }
      else
      {
        floatString = floatString.Replace(",", ".");
      }
      float f;
      float.TryParse(floatString, out f);
      return f;
    }
    public double GetDouble(string doubleString)
    {
      float test = 12.03f;
      string comma = test.ToString();
      bool replaceCommas = (comma.IndexOf(",") >= 0);
      if (replaceCommas)
      {
        doubleString = doubleString.Replace(".", ",");
      }
      else
      {
        doubleString = doubleString.Replace(",", ".");
      }
      double f;
      double.TryParse(doubleString, out f);
      return f;
    }

  }
}
