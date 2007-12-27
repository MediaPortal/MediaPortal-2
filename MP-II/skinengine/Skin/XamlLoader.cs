using System;
using System.Collections.Generic;
using System.Text;
using MyXaml.Core;
using SkinEngine.Controls.Animations;
using SkinEngine.Controls.Brushes;
using SkinEngine.Controls.Panels;
using SkinEngine.Controls.Transforms;
using SkinEngine.Controls.Visuals;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
namespace SkinEngine.Skin
{
  public class XamlLoader
  {

    public UIElement Load(string skinFile)
    {
      string fullFileName = String.Format(@"skin\{0}\{1}", SkinContext.SkinName, skinFile);
      using (Parser parser = new Parser())
      {
        parser.InstantiatePropertyDeclaration += new Parser.InstantiatePropertyDeclarationDlgt(parser_InstantiatePropertyDeclaration);
        parser.InstantiateFromQName += new Parser.InstantiateClassDlgt(parser_InstantiateFromQName);
        parser.PropertyDeclarationTest += new Parser.PropertyDeclarationTestDlgt(parser_PropertyDeclarationTest);
        parser.CustomTypeConvertor += new Parser.CustomTypeConverterDlgt(parser_CustomTypeConvertor);
        return (UIElement)parser.Instantiate(fullFileName, "*");
      }
    }

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
    }

    void parser_PropertyDeclarationTest(object parser, PropertyDeclarationTestEventArgs e)
    {
      e.Result = IsKnownObject(e.ChildQualifiedName);
    }

    void parser_InstantiatePropertyDeclaration(object parser, InstantiatePropertyDeclarationEventArgs e)
    {
      e.Result = GetObject(e.ChildQualifiedName);
    }


    void parser_InstantiateFromQName(object parser, InstantiateClassEventArgs e)
    {
      e.Result = GetObject(e.AssemblyQualifiedName);
    }

    bool IsKnownObject(string name)
    {
      //panels
      if (name == "DockPanel")
        return true;
      else if (name == "StackPanel")
        return true;
      else if (name == "Canvas")
        return true;

      //visuals
      else if (name == "Border")
        return true;
      else if (name == "Image")
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

      return false;
    }
    object GetObject(string name)
    {
      //panels
      if (name == "DockPanel")
        return new DockPanel();
      else if (name == "StackPanel")
        return new StackPanel();
      else if (name == "Canvas")
        return new Canvas();

      //visuals
      else if (name == "Border")
        return new Border();
      else if (name == "Image")
        return new Image();


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

      return null;
    }

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
