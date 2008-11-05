#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.IO;
using System.Collections.Generic;
using System.Xml;
using MediaPortal.Media.MediaManagement.Views;

namespace Components.Services.MediaManager.Views
{
  public class ViewLoader
  {
    /// <summary>
    /// Loads the specified view definition
    /// </summary>
    /// <param name="file">Name of the file.</param>
    /// <returns></returns>
    public MediaContainer Load(FileInfo file)
    {
      if (!file.Exists) return null;
      XmlDocument doc = new XmlDocument();
      using (FileStream fs = file.OpenRead())
        doc.Load(fs);

      XmlNode definitionNode = doc.SelectSingleNode("/ViewDefinition");
      if (definitionNode == null) return null;
      string name = GetName(definitionNode, "Name");
      if (name == null) return null;
      XmlNodeList listDbs = doc.SelectNodes("/ViewDefinition/Databases/Database");
      List<string> databases = new List<string>();
      foreach (XmlNode node in listDbs)
      {
        string dbName = GetName(node, "Name");
        if (dbName == null) continue;
        databases.Add(dbName);
      }
      if (databases.Count == 0) return null;
      MediaContainer container = new MediaContainer(name, "/");
      XmlNodeList list = doc.SelectNodes("/ViewDefinition/Views/View");
      foreach (XmlNode node in list)
      {
        View view = LoadView(node);
        view.Databases = databases;
        container.Items.Add(new ViewContainer(view, container, null));
      }
      return container;
    }

    /// <summary>
    /// Gets the name.
    /// </summary>
    /// <param name="node">The node.</param>
    /// <param name="attributeName">Name of the attribute.</param>
    /// <returns></returns>
    string GetName(XmlNode node, string attributeName)
    {
      XmlNode nodeItem = node.Attributes.GetNamedItem(attributeName);
      if (nodeItem == null) return null;
      if (nodeItem.Value == null) return null;
      return nodeItem.Value;
    }

    Operator GetOperator(XmlNode node, string attributeName)
    {
      string op = GetName(node, attributeName);
      if (op == null) return Operator.None;
      op = op.ToLower();
      if (op == "and") return Operator.And;
      if (op == "or") return Operator.Or;
      if (op == "==") return Operator.Same;
      if (op == "!=") return Operator.NotEquals;
      if (op == "greater") return Operator.GreaterThen;
      if (op == "less") return Operator.LessThen;
      if (op == "greaterorsame") return Operator.GreaterOrSameThen;
      if (op == "lessorsame") return Operator.LessOrSameThen;
      if (op == "distinct") return Operator.Distinct;
      if (op == "like") return Operator.Like;
      return Operator.None;
    }

    /// <summary>
    /// Loads the view definition from a Xml node
    /// </summary>
    /// <param name="node">The node.</param>
    /// <returns></returns>
    View LoadView(XmlNode node)
    {
      string name = GetName(node, "Name");
      string type = GetName(node, "type");
      if (name == null) return null;
      View view = new View(name);
      view.Type = type;
      
      XmlNodeList list = node.SelectNodes("SubView");
      foreach (XmlNode nodeSub in list)
      {
        View subview = LoadSubView(nodeSub);
        if (subview != null)
        {
          subview.Databases = view.Databases;
          view.SubViews.Add(subview);
        }
      }
      for (int i=0; i < view.SubViews.Count;++i)
      {
        if (!String.IsNullOrEmpty(view.SubViews[i].MappingTable))
        {
          view.MappingTable = view.SubViews[i].MappingTable;
          break;
        }
      }
      return view;
    }

    /// <summary>
    /// Loads the sub view from an xml node
    /// </summary>
    /// <param name="node">The node.</param>
    /// <returns></returns>
    View LoadSubView(XmlNode node)
    {
      string key = GetName(node, "key");
      string compvalue = GetName(node, "value");
      string mapTable = GetName(node, "map");
      Operator op = GetOperator(node, "operator");
      View v = new View("");
      v.MappingTable = mapTable;
      if (key != null && compvalue != null)
      {
        v.Query = new Query(key, op, compvalue);
        return v;
      }
      if (key != null)
      {
        v.Query = new Query(key, op);
        return v;
      }
      XmlNode query = node.SelectSingleNode("Query");
      if (query != null)
      {
        v.Query = LoadQuery(query);
      }
      return v;
    }
    /// <summary>
    /// Loads a query from an xml node
    /// </summary>
    /// <param name="node">The node.</param>
    /// <returns></returns>
    Query LoadQuery(XmlNode node)
    {
      string key = GetName(node, "key");
      string compvalue = GetName(node, "value");
      Operator op = GetOperator(node, "operator");
      if (key != null && compvalue != null)
      {
        return new Query(key, op, compvalue);
      }
      if (key != null)
      {
        return new Query(key, op, compvalue);
      }

      XmlNode expression = node.SelectSingleNode("Expression");
      if (expression != null)
      {
        Expression ex = LoadExpression(expression);
        if (ex != null)
          return new Query("", ex);
      }
      return null;
    }

    /// <summary>
    /// Loads an expression from a xml node.
    /// </summary>
    /// <param name="node">The node.</param>
    /// <returns></returns>
    Expression LoadExpression(XmlNode node)
    {
      List<Query> queries = new List<Query>();
      Operator op = GetOperator(node, "operator");
      XmlNodeList list = node.SelectNodes("Query");
      foreach (XmlNode nodeQ in list)
      {
        Query q = LoadQuery(nodeQ);
        if (q != null)
        {
          queries.Add(q);
        }
      }
      if (queries.Count == 1)
        return new Expression(queries[0]);
      if (queries.Count == 2)
        return new Expression(queries[0], op, queries[1]);
      return null;
    }
  }
}
