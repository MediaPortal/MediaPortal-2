using System;
using System.Collections.Generic;
using System.Text;
using MyXaml.Core;
using SkinEngine.Controls.Animations;
using SkinEngine.Controls.Brushes;
using SkinEngine.Controls.Panels;
using SkinEngine.Controls.Transforms;
using SkinEngine.Controls.Visuals;
namespace SkinEngine.Skin
{
  public class XamlLoader
  {

    public void Load(string skinFile)
    {
      string fullFileName = String.Format(@"skin\{0}\{1}", SkinContext.SkinName, skinFile);
      using (Parser parser = new Parser())
      {
        parser.InstantiatePropertyDeclaration += new Parser.InstantiatePropertyDeclarationDlgt(parser_InstantiatePropertyDeclaration);
        parser.InstantiateFromQName += new Parser.InstantiateClassDlgt(parser_InstantiateFromQName);
        parser.PropertyDeclarationTest += new Parser.PropertyDeclarationTestDlgt(parser_PropertyDeclarationTest);
        object o= parser.Instantiate(fullFileName, "*");
        int x = 123;
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
  }
}
