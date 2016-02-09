using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Plugins.MediaServer.ANTLR;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Plugins.Transcoding.Aspects;

namespace MediaPortal.Plugins.MediaServer.Parser
{
  public enum LogOp
  {
    AND,
    OR,
  }

  public enum Property
  {
    CLASS,
    GENRE,
    ARTIST,
    ALBUM,
    ORIGINAL_TRACK_NUMBER,
    TITLE,
    DATE,
    CREATOR,
    RES,
    RES_SIZE,
    ID,
    REFID,
    PARENTID,
  }

  public enum PropertyClass
  {
    AUDIO_ITEM,

    MUSIC_ALBUM,
    MUSIC_ARTIST,

    VIDEO_ITEM,

    IMAGE_ITEM,

    PLAYLIST_CONTAINER,
  }

  public enum Op
  {
    // From relOp
    EQUALS,
    NOT_EQUAL,
    LESS_THAN,
    LESS_THAN_EQUALS,
    GREATER_THAN,
    GREATER_THAN_EQUALS,

    // From stringOp
    CONTAINS,
    DOES_NOT_CONTAIN,
    DERIVED_FROM,
    STARTS_WITH,

    // From existsOp
    EXISTS,
    DOES_NOT_EXIST,
  }

  public class SearchExp
  {
    public LogOp? logOp;

    public Property? property;
    public Op? op;
    public PropertyClass? propertyClass;
    public string val;

    public SearchExp parent;
    public IList<SearchExp> children = new List<SearchExp>();
  }

  public class SearchException : Exception
  {
    public SearchException(string msg) : base(msg) { }

    public SearchException(string msg, Exception e) : base(msg, e) { }
  }

  class PropertyClassFilter : IFilter
  {
    public PropertyClass propertyClass;

    public PropertyClassFilter(PropertyClass propertyClass)
    {
      this.propertyClass = propertyClass;
    }
  }

  public class SearchParser
  {
    private class SearchErrorListener : IAntlrErrorListener<IToken>
    {
      public void SyntaxError(IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
      {
        throw new SearchException("Failed to parse at line " + line + " due to " + msg, e);
      }
    }

    private class SearchParseListener : UPnPBaseListener
    {
      private SearchExp node;
      private string val = null;

      private static IDictionary<string, Property> PROPERTIES = new Dictionary<string, Property>();
      private static IDictionary<string, PropertyClass> PROPERTY_CLASSES = new Dictionary<string, PropertyClass>();
      private static IDictionary<string, Op> OPS = new Dictionary<string, Op>();
      private static IDictionary<string, LogOp> LOG_OPS = new Dictionary<string, LogOp>();

      static SearchParseListener()
      {
        PROPERTIES["upnp:class"] = Property.CLASS;
        PROPERTIES["upnp:genre"] = Property.GENRE;
        PROPERTIES["upnp:artist"] = Property.ARTIST;
        PROPERTIES["upnp:album"] = Property.ALBUM;
        PROPERTIES["upnp:originalTrackNumber"] = Property.ORIGINAL_TRACK_NUMBER;
        PROPERTIES["dc:title"] = Property.TITLE;
        PROPERTIES["dc:date"] = Property.DATE;
        PROPERTIES["dc:creator"] = Property.CREATOR;
        PROPERTIES["res"] = Property.RES;
        PROPERTIES["res@size"] = Property.RES_SIZE;
        PROPERTIES["@id"] = Property.ID;
        PROPERTIES["@refID"] = Property.REFID;
        PROPERTIES["@parentId"] = Property.PARENTID;

        PROPERTY_CLASSES["object.item.audioItem"] = PropertyClass.AUDIO_ITEM;
        PROPERTY_CLASSES["object.container.album.musicAlbum"] = PropertyClass.MUSIC_ALBUM;
        PROPERTY_CLASSES["object.container.person.musicArtist"] = PropertyClass.MUSIC_ARTIST;
        PROPERTY_CLASSES["object.item.videoItem"] = PropertyClass.VIDEO_ITEM;
        PROPERTY_CLASSES["object.item.imageItem"] = PropertyClass.IMAGE_ITEM;
        PROPERTY_CLASSES["object.container.playlistContainer"] = PropertyClass.PLAYLIST_CONTAINER;

        OPS["="] = Op.EQUALS;
        OPS["!="] = Op.NOT_EQUAL;
        OPS["<"] = Op.LESS_THAN;
        OPS["<="] = Op.LESS_THAN_EQUALS;
        OPS[">"] = Op.GREATER_THAN;
        OPS[">="] = Op.GREATER_THAN_EQUALS;
        OPS["contains"] = Op.CONTAINS;
        OPS["doesNotContain"] = Op.DOES_NOT_CONTAIN;
        OPS["derivedfrom"] = Op.DERIVED_FROM;
        OPS["derivedFrom"] = Op.DERIVED_FROM;
        OPS["startsWith"] = Op.STARTS_WITH;
        OPS["exists"] = Op.EXISTS;

        LOG_OPS["and"] = LogOp.AND;
        LOG_OPS["or"] = LogOp.OR;
      }

      public SearchParseListener(SearchExp root)
      {
        node = root;
      }

      private static Property ParseProperty(string text)
      {
        Property value;
        if (!PROPERTIES.TryGetValue(text, out value))
        {
          throw new SearchException("Invalid property " + text);
        }
        //Console.WriteLine("ParseProperty " + text + " -> " + value);
        return value;
      }

      private static PropertyClass ParsePropertyClass(string text)
      {
        PropertyClass value;
        if (!PROPERTY_CLASSES.TryGetValue(text, out value))
        {
          throw new SearchException("Invalid property class " + text + " of " + PROPERTY_CLASSES.Count);
        }
        //Console.WriteLine("ParsePropertyClass " + text + " -> " + value);
        return value;
      }

      private static Op ParseOp(string text)
      {
        Op value;
        if (!OPS.TryGetValue(text, out value))
        {
          throw new SearchException("Invalid op " + text);
        }
        //Console.WriteLine("ParseOp " + text + " -> " + value);
        return value;
      }

      private static LogOp ParseLogOp(string text)
      {
        LogOp value;
        if (!LOG_OPS.TryGetValue(text, out value))
        {
          throw new SearchException("Invalid logical op " + text);
        }
        //Console.WriteLine("ParseLogOp " + text + " -> " + value);
        return value;
      }

      public override void EnterInnerExp(UPnPParser.InnerExpContext context)
      {
        SearchExp searchExp = new SearchExp();
        searchExp.parent = node;

        node.children.Add(searchExp);

        node = searchExp;
      }

      public override void ExitInnerExp(UPnPParser.InnerExpContext context)
      {
        node = node.parent;
      }

      public override void ExitLogOp(UPnPParser.LogOpContext context)
      {
        node.logOp = ParseLogOp(context.GetText());
      }

      public override void EnterRelExp(UPnPParser.RelExpContext context)
      {
        SearchExp searchExp = new SearchExp();
        searchExp.parent = node;

        node.children.Add(searchExp);

        node = searchExp;
      }

      public override void ExitRelExp(UPnPParser.RelExpContext context)
      {
        if (node.property == Property.CLASS)
          node.propertyClass = ParsePropertyClass(val);
        else
          node.val = val;

        node = node.parent;
      }

      public override void ExitRelOp(UPnPParser.RelOpContext context)
      {
        node.op = ParseOp(context.GetText());
      }

      public override void ExitStringOp(UPnPParser.StringOpContext context)
      {
        node.op = ParseOp(context.GetText());
      }

      public override void ExitExistsOp(UPnPParser.ExistsOpContext context)
      {
        node.op = ParseOp(context.GetText());
      }

      public override void ExitBoolVal(UPnPParser.BoolValContext context)
      {
        if ("false" == context.GetText() && node.op == Op.EXISTS)
          node.op = Op.DOES_NOT_EXIST;
      }

      public override void ExitProperty(UPnPParser.PropertyContext context)
      {
        node.property = ParseProperty(context.GetText());
      }

      public override void ExitQuotedVal(UPnPParser.QuotedValContext context)
      {
        val = context.GetText().Substring(1, context.GetText().Length - 2).Replace("\\\"", "\"");
        //Console.WriteLine("Quote val " + context.GetText() + " -> " + val);
      }
    }

    public static void ShowSearchExp(string title, SearchExp node)
    {
      Console.WriteLine(title + ":");
      ShowSearchExp(node, "");
    }

    private static void ShowSearchExp(SearchExp node, string indent)
    {
      string sb = "";
      if (node.property != null)
      {
        sb += node.property + " " + node.op + "=";
        if (node.propertyClass != null)
          sb += node.propertyClass;
        else
          sb += node.val;

        Console.WriteLine(indent + indent.Length + ":" + sb);
      }

      bool first = true;
      foreach (SearchExp child in node.children)
      {
        if (first)
          first = false;
        else
          Console.WriteLine(indent + indent.Length + ":" + node.logOp);
        ShowSearchExp(child, indent + " ");
      }
    }

    public static SearchExp Parse(string text)
    {
      UPnPLexer lexer = new UPnPLexer(new AntlrInputStream(text));
      CommonTokenStream tokens = new CommonTokenStream(lexer);
      UPnPParser parser = new UPnPParser(tokens);
      parser.AddErrorListener(new SearchErrorListener());

      SearchExp root = new SearchExp();
      parser.AddParseListener(new SearchParseListener(root));

      parser.searchCrit();
      //ShowSearchExp(text, root);

      return root;
    }

    public static IFilter Convert(SearchExp exp, ICollection<Guid> types)
    {
      if (exp.property == Property.CLASS)
      {
        // Container property classes cause title searches to change attribute (see later)
        if (exp.op == Op.DERIVED_FROM && exp.propertyClass == PropertyClass.AUDIO_ITEM)
        {
          types.Add(AudioAspect.ASPECT_ID);
          return null;
        }
        else if (exp.op == Op.EQUALS && exp.propertyClass == PropertyClass.MUSIC_ALBUM)
        {
          types.Add(AudioAspect.ASPECT_ID);
          types.Add(TranscodeItemAudioAspect.ASPECT_ID);
          return new PropertyClassFilter(exp.propertyClass.Value);
        }
        else if (exp.op == Op.EQUALS && exp.propertyClass == PropertyClass.MUSIC_ARTIST)
        {
          types.Add(AudioAspect.ASPECT_ID);
          types.Add(TranscodeItemAudioAspect.ASPECT_ID);
          return new PropertyClassFilter(exp.propertyClass.Value);
        }
        else if (exp.op == Op.DERIVED_FROM && exp.propertyClass == PropertyClass.VIDEO_ITEM)
        {
          types.Add(VideoAspect.ASPECT_ID);
          types.Add(TranscodeItemVideoAspect.ASPECT_ID);
          return null;
        }
        else if (exp.op == Op.DERIVED_FROM && exp.propertyClass == PropertyClass.PLAYLIST_CONTAINER)
        {
          types.Add(VideoAspect.ASPECT_ID);
          types.Add(TranscodeItemVideoAspect.ASPECT_ID);
          return null;
        }
        else if (exp.op == Op.DERIVED_FROM && exp.propertyClass == PropertyClass.IMAGE_ITEM)
        {
          types.Add(ImageAspect.ASPECT_ID);
          types.Add(TranscodeItemImageAspect.ASPECT_ID);
          return null;
        }
        else
        {
          throw new SearchException("Unable to convert property " + exp.op + " " + exp.propertyClass);
        }
      }
      else if (exp.property == Property.TITLE)
      {
        if (exp.op == Op.CONTAINS)
        {
          return new LikeFilter(MediaAspect.ATTR_TITLE, "%" + exp.val + "%", null);
        }
      }
      else if (exp.property == Property.CREATOR)
      {
        return new LikeFilter(AudioAspect.ATTR_ARTISTS, "%" + exp.val + "%", null);
      }
      else if (exp.property == Property.ARTIST)
      {
        return new LikeFilter(AudioAspect.ATTR_ARTISTS, "%" + exp.val + "%", null);
      }

      IList<IFilter> childFilters = exp.children.Select(childExp => Convert(childExp, types)).Where(filter => filter != null).ToList();

      // Now do the check for container property classes
      int index = 0;
      while (index < childFilters.Count)
      {
        PropertyClassFilter pcf = childFilters[index] as PropertyClassFilter;
        if (pcf != null)
        {
          if (pcf.propertyClass == PropertyClass.MUSIC_ALBUM)
          {
            ChangeAttribute(childFilters, MediaAspect.ATTR_TITLE, AudioAspect.ATTR_ALBUM);
          }
          else if (pcf.propertyClass == PropertyClass.MUSIC_ARTIST)
          {
            ChangeAttribute(childFilters, MediaAspect.ATTR_TITLE, AudioAspect.ATTR_ARTISTS);
          }
          childFilters.RemoveAt(index);
        }
        else
        {
          index++;
        }
      }

      if (childFilters.Count == 0)
      {
        return null;
      }
      else if (childFilters.Count == 1)
      {
        return childFilters[0];
      }

      return new BooleanCombinationFilter(exp.logOp == LogOp.AND ? BooleanOperator.And : BooleanOperator.Or, childFilters);
    }

    private static void ChangeAttribute(IList<IFilter> filters, MediaItemAspectMetadata.AttributeSpecification from, MediaItemAspectMetadata.AttributeSpecification to)
    {
      foreach (IFilter filter in filters)
      {
        AbstractAttributeFilter af = filter as AbstractAttributeFilter;
        if (af != null && af.AttributeType == from)
        {
          af.AttributeType = to;
        }

        BooleanCombinationFilter bcf = filter as BooleanCombinationFilter;
        if (bcf != null)
        {
          ChangeAttribute(bcf.Operands.ToList(), from, to);
        }
      }
    }
  }
}
