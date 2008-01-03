using System;
using System.Diagnostics;
using System.ComponentModel;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using MyXaml.Core;
using SkinEngine.Controls.Animations;
using SkinEngine.Controls.Brushes;
using SkinEngine.Controls.Panels;
using SkinEngine.Controls.Transforms;
using SkinEngine.Controls.Visuals;
using SkinEngine.Controls.Visuals.Triggers;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
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
      string fullFileName = String.Format(@"skin\{0}\{1}", SkinContext.SkinName, skinFile);
      using (Parser parser = new Parser())
      {
        parser.InstantiatePropertyDeclaration += new Parser.InstantiatePropertyDeclarationDlgt(parser_InstantiatePropertyDeclaration);
        parser.InstantiateFromQName += new Parser.InstantiateClassDlgt(parser_InstantiateFromQName);
        parser.PropertyDeclarationTest += new Parser.PropertyDeclarationTestDlgt(parser_PropertyDeclarationTest);
        parser.CustomTypeConvertor += new Parser.CustomTypeConverterDlgt(parser_CustomTypeConvertor);
        parser.OnGetResource += new Parser.GetResourceDlgt(parser_OnGetResource);
        parser.AddToCollection += new Parser.AddToCollectionDlgt(parser_AddToCollection);
        parser.OnSetContent += new Parser.SetContentDlg(parser_OnSetContent);
        return parser.Instantiate(fullFileName, "*");
      }
    }


    public object Load(string skinFile, string tagName)
    {
      string fullFileName = String.Format(@"skin\{0}\{1}", SkinContext.SkinName, skinFile);
      using (Parser parser = new Parser())
      {
        parser.InstantiatePropertyDeclaration += new Parser.InstantiatePropertyDeclarationDlgt(parser_InstantiatePropertyDeclaration);
        parser.InstantiateFromQName += new Parser.InstantiateClassDlgt(parser_InstantiateFromQName);
        parser.PropertyDeclarationTest += new Parser.PropertyDeclarationTestDlgt(parser_PropertyDeclarationTest);
        parser.CustomTypeConvertor += new Parser.CustomTypeConverterDlgt(parser_CustomTypeConvertor);
        parser.OnGetResource += new Parser.GetResourceDlgt(parser_OnGetResource);
        parser.AddToCollection += new Parser.AddToCollectionDlgt(parser_AddToCollection);
        parser.OnSetContent += new Parser.SetContentDlg(parser_OnSetContent);
        return (UIElement)parser.Instantiate(fullFileName, tagName);
      }
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

    object parser_OnGetResource(object parser, object obj, string resourceName)
    {
      Trace.WriteLine(String.Format("Get resource:{0}", resourceName));
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
        object result = _lastElement.FindResource(resourceName);
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
      return null;
    }

    void parser_AddToCollection(object parser, AddToCollectionEventArgs e)
    {
      if (e.Container as ResourceDictionary != null)
      {
        if ((e.Instance as ResourceDictionary) != null)
        {
          ResourceDictionary dictNew = (ResourceDictionary)e.Instance;
          XamlLoader loader = new XamlLoader();
          object o = loader.Load(dictNew.Source);

          ResourceDictionary dict = (ResourceDictionary)e.Container;
          dict.Merge((ResourceDictionary)o);
          e.Result = true;
          return;
        }
        else
        {

          string key = e.Node.Attributes["Key"].Value;
          ResourceDictionary dict = (ResourceDictionary)e.Container;
          dict[key] = e.Instance;
          e.Result = true;
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
          e.Result = new GridLength();
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
          double percent = double.Parse(text);
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
      else if (name == "ScrollViewer")
        return true;
      else if (name == "Resources")
        return true;
      else if (name == "ResourceDictionary")
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
    protected float GetFloat(string floatString)
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

  }
}
