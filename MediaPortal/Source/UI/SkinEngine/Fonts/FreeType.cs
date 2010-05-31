#region License
/*
MIT License
Copyright ?2003-2007 Tao Framework Team
http://www.taoframework.com
All rights reserved.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
#endregion License

#region Version
// based on http://www.koders.com/csharp/fid840ED1F892217853EE1DD8692B953A84E1D5C2AE.aspx
//
// Applicable to Freetype 2.1.9 or later
//
// 2007-Nov-18 - Euan D MacInnes
//         Repaired FT_Library_Version
//
// 2007-Nov-12 - Jendave. 
//         Added FT_NATIVE_LIBRARY and CALLING CONVENTION
//         Added Summary tags for all fields in structs/classes
//
// 2007-Nov-9- Euan D MacInnes. 
//         Converted names to official FreeType names
//         Added help text from FreeType website
//
// 2007-Nov-01 Euan D MacInnes. Amendments are to:
// Init_FreeType, to make the libptr "out"
// New_Face, to make the aface "out"
// NOTE: Some FreeType variables do not start with FT.

// Some structures exist here ***Rec_, that are currently unused.
// These were originally intended as typed pointer references.
// However IntPtr's have been used for now. 
#endregion

using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Security;

namespace Tao.FreeType
{
  #region Class Documentation
  /// <summary>
  ///     FreeType 2 Binding for .NET
  /// </summary>
  /// <remarks>
  ///     <para>
  ///         Binds functions and definitions in 
  ///         freetype6.dll (Windows)
  ///         /usr/lib/libfreetype.so.6 (Linux - specifically Fedora Core freetype install location)
  ///         /Library/Frameworks/Mono.framework/Libraries/libfreetype.6.dylib (MacOSX)
  ///     </para>
  ///     <para>
  ///         The FreeType library includes the base data types and function calls to FreeType 2
  ///         to allow access to TrueType and OpenType fonts across platforms.
  ///     </para>
  ///     <para>
  ///         This is not a rendering utility and will not render fonts to the screen. It is an interface
  ///         to the various font formats, and can provide either outline or bitmapped versions
  ///         of font glyphs.
  ///     </para>    
  /// </remarks>
  #endregion Class Documentation

  [StructLayout(LayoutKind.Sequential)]
  public struct MemoryRec_
  {
    public IntPtr /*void*/ user;
    public IntPtr /* funcptr */ alloc;
    public IntPtr /* funcptr */ free;
    public IntPtr /* funcptr */ realloc;
  }

  /// <summary>
  /// A structure used to describe an input stream.
  /// </summary>
  [StructLayout(LayoutKind.Sequential)]
  public struct FT_StreamRec
  {
    public IntPtr /*byte*/ _base;
    public uint size;
    public uint pos;
    public FT_StreamDesc descriptor;
    public FT_StreamDesc pathname;
    public IntPtr /* funcptr */ close;
    public IntPtr /*MemoryRec_*/ memory;
    public IntPtr /*byte*/ cursor;
    public IntPtr /*byte*/ limit;
  }

  /// <summary>
  /// A union type used to store either a long or a pointer. This is used to store a file descriptor or a ?FILE*? in an input stream
  /// </summary>
  [StructLayout(LayoutKind.Explicit)]
  public struct FT_StreamDesc
  {
    [FieldOffset(0)]
    public int _value;
    [FieldOffset(0)]
    public IntPtr /*void*/ pointer;
  }

  /// <summary>
  /// A simple structure used to store a 2D vector; coordinates are of the FT_Pos type.
  /// </summary>
  [StructLayout(LayoutKind.Sequential)]
  public struct FT_Vector
  {
    public int x;
    public int y;
  }

  /// <summary>
  /// A structure used to hold an outline's bounding box, i.e., the coordinates of its extrema in the horizontal and vertical directions.
  /// </summary>
  [StructLayout(LayoutKind.Sequential)]
  public struct FT_BBox
  {
    public int xMin;
    public int yMin;
    public int xMax;
    public int yMax;
  }

  /// <summary>
  /// A structure used to describe a bitmap or pixmap to the raster. Note that we now manage pixmaps of various depths through the ?pixel_mode? field.
  /// </summary>
  [StructLayout(LayoutKind.Sequential)]
  public struct FT_Bitmap
  {
    public int rows;
    public int width;
    public int pitch;
    public IntPtr /*byte*/ buffer;
    public short num_grays;
    public sbyte pixel_mode;
    public sbyte palette_mode;
    public IntPtr /*void*/ palette;
  }

  /// <summary>
  /// This structure is used to describe an outline to the scan-line converter.
  /// </summary>
  [StructLayout(LayoutKind.Sequential)]
  public struct FT_Outline
  {
    public short n_contours;
    public short n_points;
    public IntPtr /*Vector*/ points;
    public IntPtr /*sbyte*/ tags;
    public IntPtr /*short*/ contours;
    public int flags;
  }

  /// <summary>
  /// A structure to hold various function pointers used during outline decomposition in order to emit segments, conic, and cubic B?ziers, as well as ?move to? and ?close to? operations.
  /// </summary>
  [StructLayout(LayoutKind.Sequential)]
  public struct FT_Outline_Funcs
  {
    public IntPtr /* funcptr */ move_to;
    public IntPtr /* funcptr */ line_to;
    public IntPtr /* funcptr */ conic_to;
    public IntPtr /* funcptr */ cubic_to;
    public int shift;
    public int delta;
  }

  [StructLayout(LayoutKind.Sequential)]
  public struct RasterRec_
  {
  }

  /// <summary>
  /// A structure used to model a single span of gray (or black) pixels when rendering a monochrome or anti-aliased bitmap
  /// </summary>
  [StructLayout(LayoutKind.Sequential)]
  public struct FT_Span
  {
    public short x;
    public ushort len;
    public byte coverage;
  }

  /// <summary>
  /// A structure to hold the arguments used by a raster's render functions
  /// </summary>
  [StructLayout(LayoutKind.Sequential)]
  public struct FT_Raster_Params
  {
    public IntPtr /*Bitmap*/ target;
    public IntPtr /*void*/ source;
    public int flags;
    public IntPtr /* funcptr */ gray_spans;
    public IntPtr /* funcptr */ black_spans;
    public IntPtr /* funcptr */ bit_test;
    public IntPtr /* funcptr */ bit_set;
    public IntPtr /*void*/ user;
    public FT_BBox clip_box;
  }

  /// <summary>
  /// A structure used to describe a given raster class to the library.
  /// </summary>
  [StructLayout(LayoutKind.Sequential)]
  public struct FT_Raster_Funcs
  {
    public FT_Glyph_Format glyph_format;
    public IntPtr /* funcptr */ raster_new;
    public IntPtr /* funcptr */ raster_reset;
    public IntPtr /* funcptr */ raster_set_mode;
    public IntPtr /* funcptr */ raster_render;
    public IntPtr /* funcptr */ raster_done;
  }

  /// <summary>
  /// A simple structure used to store a 2D vector unit vector.
  /// </summary>
  [StructLayout(LayoutKind.Sequential)]
  public struct FT_UnitVector
  {
    public short x;
    public short y;
  }

  /// <summary>
  /// A simple structure used to store a 2x2 matrix. Coefficients are in 16.16 fixed float format. The computation performed is:
  /// x' = x*xx + y*xy                                             
  /// y' = x*yx + y*yy   
  /// </summary>
  [StructLayout(LayoutKind.Sequential)]
  public struct FT_Matrix
  {
    public int xx;
    public int xy;
    public int yx;
    public int yy;
  }

  /// <summary>
  /// Read-only binary data represented as a pointer and a length.
  /// </summary>
  [StructLayout(LayoutKind.Sequential)]
  public struct FT_Data
  {
    public IntPtr /*byte*/ pointer;
    public int length;
  }

  /// <summary>
  /// Client applications often need to associate their own data to a variety of FreeType core objects. For example, a text layout API might want to associate a glyph cache to a given size object.
  /// Most FreeType object contains a ?generic? field, of type FT_Generic, which usage is left to client applications and font servers.
  /// It can be used to store a pointer to client-specific data, as well as the address of a ?finalizer? function, which will be called by FreeType when the object is destroyed (for example, the previous client example would put the address of the glyph cache destructor in the ?finalizer? field).
  /// </summary>
  [StructLayout(LayoutKind.Sequential)]
  public struct FT_Generic
  {
    public IntPtr /*void*/ data;
    public IntPtr /* funcptr */ finalizer;
  }

  /// <summary>
  /// A structure used to hold a single list element.
  /// </summary>
  [StructLayout(LayoutKind.Sequential)]
  public struct FT_ListNodeRec
  {
    public IntPtr /*ListNodeRec*/ prev;
    public IntPtr /*ListNodeRec*/ next;
    public IntPtr /*void*/ data;
  }

  /// <summary>
  /// A structure used to hold a simple double-linked list. These are used in many parts of FreeType.
  /// </summary>
  [StructLayout(LayoutKind.Sequential)]
  public struct FT_ListRec
  {
    public IntPtr /*ListNodeRec*/ head;
    public IntPtr /*ListNodeRec*/ tail;
  }

  /// <summary>
  /// A structure used to model the metrics of a single glyph. The values are expressed in 26.6 fractional pixel format; if the flag FT_LOAD_NO_SCALE has been used while loading the glyph, values are expressed in font units instead.
  /// </summary>
  [StructLayout(LayoutKind.Sequential)]
  public struct FT_Glyph_Metrics
  {
    public int width;
    public int height;
    public int horiBearingX;
    public int horiBearingY;
    public int horiAdvance;
    public int vertBearingX;
    public int vertBearingY;
    public int vertAdvance;
  }

  /// <summary>
  /// This structure models the metrics of a bitmap strike (i.e., a set of glyphs for a given point size and resolution) in a bitmap font. It is used for the ?available_sizes? field of FT_Face.
  /// </summary>
  [StructLayout(LayoutKind.Sequential)]
  public struct FT_Bitmap_Size
  {
    public short height;
    public short width;
    public int size;
    public int x_ppem;
    public int y_ppem;
  }

  [StructLayout(LayoutKind.Sequential)]
  public struct LibraryRec_
  {
  }

  [StructLayout(LayoutKind.Sequential)]
  public struct ModuleRec_
  {
  }

  [StructLayout(LayoutKind.Sequential)]
  public struct DriverRec_
  {
  }

  [StructLayout(LayoutKind.Sequential)]
  public struct RendererRec_
  {
  }

  /// <summary>
  /// FreeType root face class structure. A face object models a typeface in a font file.
  /// </summary>
  [StructLayout(LayoutKind.Sequential)]
  public struct FT_FaceRec
  {
    public int num_faces;
    public int face_index;
    public int face_flags;
    public int style_flags;
    public int num_glyphs;
    public IntPtr /*sbyte*/ family_name;
    public IntPtr /*sbyte*/ style_name;
    public int num_fixed_sizes;
    public IntPtr /*Bitmap_Size*/ available_sizes;
    public int num_charmaps;
    public IntPtr /*IntPtr CharMapRec*/ charmaps;
    public FT_Generic generic;
    public FT_BBox bbox;
    public ushort units_per_EM;
    public short ascender;
    public short descender;
    public short height;
    public short max_advance_width;
    public short max_advance_height;
    public short underline_position;
    public short underline_thickness;
    public IntPtr /*GlyphSlotRec*/ glyph;
    public IntPtr /*SizeRec*/ size;
    public IntPtr /*CharMapRec*/ charmap;
    public IntPtr /*DriverRec_*/ driver;
    public IntPtr /*MemoryRec_*/ memory;
    public IntPtr /*StreamRec*/ stream;
    public FT_ListRec sizes_list;
    public FT_Generic autohint;
    public IntPtr /*void*/ extensions;
    public IntPtr /*Face_InternalRec_*/ _internal;
  }

  /// <summary>
  /// FreeType root size class structure. A size object models a face object at a given size.
  /// </summary>
  [StructLayout(LayoutKind.Sequential)]
  public struct FT_SizeRec
  {
    public IntPtr /*FaceRec*/ face;
    public FT_Generic generic;
    public FT_Size_Metrics metrics;
    public IntPtr /*Size_InternalRec_*/ _internal;
  }

  /// <summary>
  /// FreeType root glyph slot class structure. A glyph slot is a container where individual glyphs can be loaded, be they in outline or bitmap format.
  /// </summary>
  [StructLayout(LayoutKind.Sequential)]
  public struct FT_GlyphSlotRec
  {
    public IntPtr /*LibraryRec_*/ library;

    public IntPtr /*FaceRec*/ face;
    public IntPtr /*GlyphSlotRec*/ next;
    public uint reserved;
    public FT_Generic generic;
    public FT_Glyph_Metrics metrics;
    public int linearHoriAdvance;
    public int linearVertAdvance;
    public FT_Vector advance;
    public FT_Glyph_Format format;
    public FT_Bitmap bitmap;
    public int bitmap_left;
    public int bitmap_top;
    public FT_Outline outline;
    public uint num_subglyphs;
    public IntPtr /*SubGlyphRec_*/ subglyphs;
    public IntPtr /*void*/ control_data;
    public int control_len;
    public int lsb_delta;
    public int rsb_delta;
    public IntPtr /*void*/ other;
    public IntPtr /*Slot_InternalRec_*/ _internal;
  }

  /// <summary>
  /// The base charmap structure.
  /// </summary>
  [StructLayout(LayoutKind.Sequential)]
  public struct FT_CharMapRec
  {
    public IntPtr /*FaceRec*/ face;
    public FT_Encoding encoding;
    public ushort platform_id;
    public ushort encoding_id;
  }

  [StructLayout(LayoutKind.Sequential)]
  public struct Face_InternalRec_
  {
  }

  [StructLayout(LayoutKind.Sequential)]
  public struct Size_InternalRec_
  {
  }

  /// <summary>
  /// The size metrics structure gives the metrics of a size object.
  /// </summary>
  [StructLayout(LayoutKind.Sequential)]
  public struct FT_Size_Metrics
  {
    public ushort x_ppem;
    public ushort y_ppem;
    public int x_scale;
    public int y_scale;
    public int ascender;
    public int descender;
    public int height;
    public int max_advance;
  }

  [StructLayout(LayoutKind.Sequential)]
  public struct SubGlyphRec_
  {
  }

  [StructLayout(LayoutKind.Sequential)]
  public struct Slot_InternalRec_
  {
  }

  /// <summary>
  /// A simple structure used to pass more or less generic parameters to FT_Open_Face.
  /// </summary>
  [StructLayout(LayoutKind.Sequential)]
  public struct FT_Parameter
  {
    public uint tag;
    public IntPtr /*void*/ data;
  }

  /// <summary>
  /// A structure used to indicate how to open a new font file or stream. A pointer to such a structure can be used as a parameter for the functions FT_Open_Face and FT_Attach_Stream.
  /// </summary>
  [StructLayout(LayoutKind.Sequential)]
  public struct FT_Open_Args
  {
    public uint flags;
    public IntPtr /*byte*/ memory_base;
    public int memory_size;
    public IntPtr /*sbyte*/ pathname;
    public IntPtr /*StreamRec*/ stream;
    public IntPtr /*ModuleRec_*/ driver;
    public int num_params;
    public IntPtr /*Parameter*/ _params;
  }

  /// <summary>
  /// The root glyph structure contains a given glyph image plus its advance width in 16.16 fixed float format.
  /// </summary>
  [StructLayout(LayoutKind.Sequential)]
  public struct FT_Glyph
  {
    public IntPtr /*LibraryRec_*/ library;
    private IntPtr /*FT_Glyph_Class**/  clazz;
    public FT_Glyph_Format format;
    public FT_Vector advance;
  }

  /// <summary>
  /// A structure used for bitmap glyph images. This really is a `sub-class' of `FT_Glyph'. 
  /// </summary>
  [StructLayout(LayoutKind.Sequential)]
  public struct FT_BitmapGlyph
  {
    public FT_Glyph root;
    public int left;
    public int top;
    public FT_Bitmap bitmap;
  }
  /// <summary>
  /// An enumeration type used to describe the format of pixels in a given bitmap. Note that additional formats may be added in the future.
  /// </summary>
  public enum FT_Pixel_Mode
  {
    FT_PIXEL_MODE_NONE = 0,
    FT_PIXEL_MODE_MONO,
    FT_PIXEL_MODE_GRAY,
    FT_PIXEL_MODE_GRAY2,
    FT_PIXEL_MODE_GRAY4,
    FT_PIXEL_MODE_LCD,
    FT_PIXEL_MODE_LCD_V,
    FT_PIXEL_MODE_MAX,
  }

  /// <summary>
  /// An enumeration type used to describe the format of a given glyph image. Note that this version of FreeType only supports two image formats, even though future font drivers will be able to register their own format.
  /// </summary>
  public enum FT_Glyph_Format
  {
    FT_GLYPH_FORMAT_NONE = (int)((uint)0 << 24 | (uint)0 << 16 | (uint)0 << 8 | (uint)0),
    FT_GLYPH_FORMAT_COMPOSITE = (int)((uint)'c' << 24 | (uint)'o' << 16 | (uint)'m' << 8 | (uint)'p'),
    FT_GLYPH_FORMAT_BITMAP = (int)((uint)'b' << 24 | (uint)'i' << 16 | (uint)'t' << 8 | (uint)'s'),
    FT_GLYPH_FORMAT_OUTLINE = (int)((uint)'o' << 24 | (uint)'u' << 16 | (uint)'t' << 8 | (uint)'l'),
    FT_GLYPH_FORMAT_PLOTTER = (int)((uint)'p' << 24 | (uint)'l' << 16 | (uint)'o' << 8 | (uint)'t'),
  }

  /// <summary>
  /// An enumeration used to specify character sets supported by charmaps. Used in the FT_Select_Charmap API function.
  /// note:
  /// Despite the name, this enumeration lists specific character repertories (i.e., charsets), and not text encoding methods (e.g., UTF-8, UTF-16, GB2312_EUC, etc.).
  /// Because of 32-bit charcodes defined in Unicode (i.e., surrogates), all character codes must be expressed as FT_Longs.
  /// Other encodings might be defined in the future.
  /// </summary>
  public enum FT_Encoding
  {
    FT_ENCODING_NONE = (int)((uint)0 << 24 | (uint)0 << 16 | (uint)0 << 8 | (uint)0),
    FT_ENCODING_MS_SYMBOL = (int)((uint)'s' << 24 | (uint)'y' << 16 | (uint)'m' << 8 | (uint)'b'),
    FT_ENCODING_UNICODE = (int)((uint)'u' << 24 | (uint)'n' << 16 | (uint)'i' << 8 | (uint)'c'),
    FT_ENCODING_SJIS = (int)((uint)'s' << 24 | (uint)'j' << 16 | (uint)'i' << 8 | (uint)'s'),
    FT_ENCODING_GB2312 = (int)((uint)'g' << 24 | (uint)'b' << 16 | (uint)' ' << 8 | (uint)' '),
    FT_ENCODING_BIG5 = (int)((uint)'b' << 24 | (uint)'i' << 16 | (uint)'g' << 8 | (uint)'5'),
    FT_ENCODING_WANSUNG = (int)((uint)'w' << 24 | (uint)'a' << 16 | (uint)'n' << 8 | (uint)'s'),
    FT_ENCODING_JOHAB = (int)((uint)'j' << 24 | (uint)'o' << 16 | (uint)'h' << 8 | (uint)'a'),
    FT_ENCODING_MS_SJIS = (int)(FT_Encoding.FT_ENCODING_SJIS),
    FT_ENCODING_MS_GB2312 = (int)(FT_Encoding.FT_ENCODING_GB2312),
    FT_ENCODING_MS_BIG5 = (int)(FT_Encoding.FT_ENCODING_BIG5),
    FT_ENCODING_MS_WANSUNG = (int)(FT_Encoding.FT_ENCODING_WANSUNG),
    FT_ENCODING_MS_JOHAB = (int)(FT_Encoding.FT_ENCODING_JOHAB),
    FT_ENCODING_ADOBE_STANDARD = (int)((uint)'A' << 24 | (uint)'D' << 16 | (uint)'O' << 8 | (uint)'B'),
    FT_ENCODING_ADOBE_EXPERT = (int)((uint)'A' << 24 | (uint)'D' << 16 | (uint)'B' << 8 | (uint)'E'),
    FT_ENCODING_ADOBE_CUSTOM = (int)((uint)'A' << 24 | (uint)'D' << 16 | (uint)'B' << 8 | (uint)'C'),
    FT_ENCODING_ADOBE_LATIN_1 = (int)((uint)'l' << 24 | (uint)'a' << 16 | (uint)'t' << 8 | (uint)'1'),
    FT_ENCODING_OLD_LATIN_2 = (int)((uint)'l' << 24 | (uint)'a' << 16 | (uint)'t' << 8 | (uint)'2'),
    FT_ENCODING_APPLE_ROMAN = (int)((uint)'a' << 24 | (uint)'r' << 16 | (uint)'m' << 8 | (uint)'n'),
  }

  /// <summary>
  /// An enumeration type that lists the render modes supported by FreeType 2. Each mode corresponds to a specific type of scanline conversion performed on the outline.
  /// For bitmap fonts the ?bitmap->pixel_mode? field in the FT_GlyphSlotRec structure gives the format of the returned bitmap.
  /// </summary>
  public enum FT_Render_Mode
  {
    FT_RENDER_MODE_NORMAL = 0,
    FT_RENDER_MODE_LIGHT,
    FT_RENDER_MODE_MONO,
    FT_RENDER_MODE_LCD,
    FT_RENDER_MODE_LCD_V,
    FT_RENDER_MODE_MAX,
  }

  /// <summary>
  /// An enumeration used to specify which kerning values to return in FT_Get_Kerning.
  /// </summary>
  public enum FT_Kerning_Mode
  {
    FT_KERNING_DEFAULT = 0,
    FT_KERNING_UNFITTED,
    FT_KERNING_UNSCALED,
  }

  /// <summary>
  /// Main FreeType method class. Call FT_Init_FreeType to initialise
  /// </summary>
  public class FT
  {
    #region Private Constants
    #region string FT_NATIVE_LIBRARY
    /// <summary>
    /// Specifies the FT native library used in the bindings
    /// </summary>
    /// <remarks>
    /// The Windows dll is specified here universally - note that
    /// under Mono the non-windows native library can be mapped using
    /// the ".config" file mechanism.  Kudos to the Mono team for this
    /// simple yet elegant solution.
    /// </remarks>
    private const string FT_NATIVE_LIBRARY = "freetype6.dll";
    #endregion string FT_NATIVE_LIBRARY

    #region CallingConvention CALLING_CONVENTION
    /// <summary>
    ///     Specifies the calling convention used for the binding.
    /// </summary>
    /// <remarks>
    ///     Specifies <see cref="CallingConvention.Cdecl" />
    ///     for the bindings.
    /// </remarks>
    private const CallingConvention CALLING_CONVENTION = CallingConvention.Cdecl;
    #endregion CallingConvention CALLING_CONVENTION
    #endregion Private Constants
    public static Hashtable ErrorStrings;
    static FT()
    {
      ErrorStrings = new Hashtable();
      ErrorStrings[0x00] = "no error";


      ErrorStrings[0x01] = "cannot open resource";

      ErrorStrings[0x02] = "unknown file format";

      ErrorStrings[0x03] = "broken file";

      ErrorStrings[0x04] = "invalid FreeType version";

      ErrorStrings[0x05] = "module version is too low";

      ErrorStrings[0x06] = "invalid argument";

      ErrorStrings[0x07] = "unimplemented feature";

      ErrorStrings[0x08] = "broken table";

      ErrorStrings[0x09] = "broken offset within table";




      ErrorStrings[0x10] = "invalid glyph index";

      ErrorStrings[0x11] = "invalid character code";

      ErrorStrings[0x12] = "unsupported glyph image format";

      ErrorStrings[0x13] = "cannot render this glyph format";

      ErrorStrings[0x14] = "invalid outline";

      ErrorStrings[0x15] = "invalid composite glyph";

      ErrorStrings[0x16] = "too many hints";

      ErrorStrings[0x17] = "invalid pixel size";




      ErrorStrings[0x20] = "invalid object handle";

      ErrorStrings[0x21] = "invalid library handle";

      ErrorStrings[0x22] = "invalid module handle";

      ErrorStrings[0x23] = "invalid face handle";

      ErrorStrings[0x24] = "invalid size handle";

      ErrorStrings[0x25] = "invalid glyph slot handle";

      ErrorStrings[0x26] = "invalid charmap handle";

      ErrorStrings[0x27] = "invalid cache manager handle";

      ErrorStrings[0x28] = "invalid stream handle";




      ErrorStrings[0x30] = "too many modules";

      ErrorStrings[0x31] = "too many extensions";




      ErrorStrings[0x40] = "out of memory";

      ErrorStrings[0x41] = "unlisted object";




      ErrorStrings[0x51] = "cannot open stream";

      ErrorStrings[0x52] = "invalid stream seek";

      ErrorStrings[0x53] = "invalid stream skip";

      ErrorStrings[0x54] = "invalid stream read";

      ErrorStrings[0x55] = "invalid stream operation";

      ErrorStrings[0x56] = "invalid frame operation";

      ErrorStrings[0x57] = "nested frame access";

      ErrorStrings[0x58] = "invalid frame read";




      ErrorStrings[0x60] = "raster uninitialized";

      ErrorStrings[0x61] = "raster corrupted";

      ErrorStrings[0x62] = "raster overflow";

      ErrorStrings[0x63] = "negative height while rastering";




      ErrorStrings[0x70] = "too many registered caches";




      ErrorStrings[0x80] = "invalid opcode";

      ErrorStrings[0x81] = "too few arguments";

      ErrorStrings[0x82] = "stack overflow";

      ErrorStrings[0x83] = "code overflow";

      ErrorStrings[0x84] = "bad argument";

      ErrorStrings[0x85] = "division by zero";

      ErrorStrings[0x86] = "invalid reference";

      ErrorStrings[0x87] = "found debug opcode";

      ErrorStrings[0x88] = "found ENDF opcode in execution stream";

      ErrorStrings[0x89] = "nested DEFS";

      ErrorStrings[0x8A] = "invalid code range";

      ErrorStrings[0x8B] = "execution context too long";

      ErrorStrings[0x8C] = "too many function definitions";

      ErrorStrings[0x8D] = "too many instruction definitions";

      ErrorStrings[0x8E] = "SFNT font table missing";

      ErrorStrings[0x8F] = "horizontal header (hhea) table missing";

      ErrorStrings[0x90] = "locations (loca) table missing";

      ErrorStrings[0x91] = "name table missing";

      ErrorStrings[0x92] = "character map (cmap) table missing";

      ErrorStrings[0x93] = "horizontal metrics (hmtx) table missing";

      ErrorStrings[0x94] = "PostScript (post) table missing";

      ErrorStrings[0x95] = "invalid horizontal metrics";

      ErrorStrings[0x96] = "invalid character map (cmap) format";

      ErrorStrings[0x97] = "invalid ppem value";

      ErrorStrings[0x98] = "invalid vertical metrics";

      ErrorStrings[0x99] = "could not find context";

      ErrorStrings[0x9A] = "invalid PostScript (post) table format";

      ErrorStrings[0x9B] = "invalid PostScript (post) table";




      ErrorStrings[0xA0] = "opcode syntax error";

      ErrorStrings[0xA1] = "argument stack underflow";

      ErrorStrings[0xA2] = "ignore";




      ErrorStrings[0xB0] = "`STARTFONT' field missing";

      ErrorStrings[0xB1] = "`FONT' field missing";

      ErrorStrings[0xB2] = "`SIZE' field missing";

      ErrorStrings[0xB3] = "`CHARS' field missing";

      ErrorStrings[0xB4] = "`STARTCHAR' field missing";

      ErrorStrings[0xB5] = "`ENCODING' field missing";

      ErrorStrings[0xB6] = "`BBX' field missing";
    }

    public const uint ft_open_driver = 0x8;
    public const uint ft_open_memory = 0x1;
    public const uint ft_open_params = 0x10;
    public const uint ft_open_pathname = 0x4;
    public const uint ft_open_stream = 0x2;
    public const uint ft_outline_even_odd_fill = 0x2;
    public const uint ft_outline_high_precision = 0x100;
    public const uint ft_outline_ignore_dropouts = 0x8;
    public const uint ft_outline_none = 0x0;
    public const uint ft_outline_owner = 0x1;
    public const uint ft_outline_reverse_fill = 0x4;
    public const uint ft_outline_single_pass = 0x200;
    public const uint ft_raster_flag_aa = 0x1;
    public const uint ft_raster_flag_clip = 0x4;
    public const uint ft_raster_flag_default = 0x0;
    public const uint ft_raster_flag_direct = 0x2;
    public const int FREETYPE_MAJOR = 2;
    public const int FREETYPE_MINOR = 1;
    public const int FREETYPE_PATCH = 9;
    public const int ALIGNMENT = 8;
    public const int Curve_Tag_Conic = 0;
    public const int Curve_Tag_Cubic = 2;
    public const int Curve_Tag_On = 1;
    public const int Curve_Tag_Touch_X = 8;
    public const int Curve_Tag_Touch_Y = 16;
    public const int CURVE_TAG_CONIC = 0;
    public const int CURVE_TAG_CUBIC = 2;
    public const int CURVE_TAG_ON = 1;
    public const int CURVE_TAG_TOUCH_X = 8;
    public const int CURVE_TAG_TOUCH_Y = 16;
    public const int FT_LOAD_CROP_BITMAP = 0x40;
    public const int FT_LOAD_DEFAULT = 0x0;
    public const int FT_LOAD_FORCE_AUTOHINT = 0x20;
    public const int FT_LOAD_IGNORE_GLOBAL_ADVANCE_WIDTH = 0x200;
    public const int FT_LOAD_IGNORE_TRANSFORM = 0x800;
    public const int FT_LOAD_LINEAR_DESIGN = 0x2000;
    public const int FT_LOAD_MONOCHROME = 0x1000;
    public const int FT_LOAD_NO_BITMAP = 0x8;
    public const int FT_LOAD_NO_HINTING = 0x2;
    public const int FT_LOAD_NO_RECURSE = 0x400;
    public const int FT_LOAD_NO_SCALE = 0x1;
    public const int FT_LOAD_PEDANTIC = 0x80;
    public const int FT_LOAD_RENDER = 0x4;
    public const int FT_LOAD_SBITS_ONLY = 0x4000;
    public const int FT_LOAD_VERTICAL_LAYOUT = 0x10;
    public const int MAX_MODULES = 32;
    public const uint FT_OPEN_DRIVER = 0x8;
    public const uint FT_OPEN_MEMORY = 0x1;
    public const uint FT_OPEN_PARAMS = 0x10;
    public const uint FT_OPEN_PATHNAME = 0x4;
    public const uint FT_OPEN_STREAM = 0x2;
    public const uint FT_OUTLINE_EVEN_ODD_FILL = 0x2;
    public const uint FT_OUTLINE_HIGH_PRECISION = 0x100;
    public const uint FT_OUTLINE_IGNORE_DROPOUTS = 0x8;
    public const uint FT_OUTLINE_NONE = 0x0;
    public const uint FT_OUTLINE_OWNER = 0x1;
    public const uint FT_OUTLINE_REVERSE_FILL = 0x4;
    public const uint FT_OUTLINE_SINGLE_PASS = 0x200;
    public const uint FT_RASTER_FLAG_AA = 0x1;
    public const uint FT_RASTER_FLAG_CLIP = 0x4;
    public const uint FT_RASTER_FLAG_DEFAULT = 0x0;
    public const uint FT_RASTER_FLAG_DIRECT = 0x2;
    public const int HAVE_FCNTL_H = 1;
    public const int HAVE_UNISTD_H = 1;
    public const int T1_MAX_CHARSTRINGS_OPERANDS = 256;
    public const int T1_MAX_DICT_DEPTH = 5;
    public const int T1_MAX_SUBRS_CALLS = 16;
    public const int Mod_Err_Base = 0;
    public const int Mod_Err_Autohint = 0;
    public const int Mod_Err_BDF = 0;
    public const int Mod_Err_Cache = 0;
    public const int Mod_Err_CFF = 0;
    public const int Mod_Err_CID = 0;
    public const int Mod_Err_Gzip = 0;
    public const int Mod_Err_LZW = 0;
    public const int Mod_Err_PCF = 0;
    public const int Mod_Err_PFR = 0;
    public const int Mod_Err_PSaux = 0;
    public const int Mod_Err_PShinter = 0;
    public const int Mod_Err_PSnames = 0;
    public const int Mod_Err_Raster = 0;
    public const int Mod_Err_SFNT = 0;
    public const int Mod_Err_Smooth = 0;
    public const int Mod_Err_TrueType = 0;
    public const int Mod_Err_Type1 = 0;
    public const int Mod_Err_Type42 = 0;
    public const int Mod_Err_Winfonts = 0;
    public const int Mod_Err_Max = 1;
    public const int Err_Ok = 0x00;
    public const int Err_Cannot_Open_Resource = (int)(0x01 + 0);
    public const int Err_Unknown_File_Format = (int)(0x02 + 0);
    public const int Err_Invalid_File_Format = (int)(0x03 + 0);
    public const int Err_Invalid_Version = (int)(0x04 + 0);
    public const int Err_Lower_Module_Version = (int)(0x05 + 0);
    public const int Err_Invalid_Argument = (int)(0x06 + 0);
    public const int Err_Unimplemented_Feature = (int)(0x07 + 0);
    public const int Err_Invalid_Table = (int)(0x08 + 0);
    public const int Err_Invalid_Offset = (int)(0x09 + 0);
    public const int Err_Invalid_Glyph_Index = (int)(0x10 + 0);
    public const int Err_Invalid_Character_Code = (int)(0x11 + 0);
    public const int Err_Invalid_Glyph_Format = (int)(0x12 + 0);
    public const int Err_Cannot_Render_Glyph = (int)(0x13 + 0);
    public const int Err_Invalid_Outline = (int)(0x14 + 0);
    public const int Err_Invalid_Composite = (int)(0x15 + 0);
    public const int Err_Too_Many_Hints = (int)(0x16 + 0);
    public const int Err_Invalid_Pixel_Size = (int)(0x17 + 0);
    public const int Err_Invalid_Handle = (int)(0x20 + 0);
    public const int Err_Invalid_Library_Handle = (int)(0x21 + 0);
    public const int Err_Invalid_Driver_Handle = (int)(0x22 + 0);
    public const int Err_Invalid_Face_Handle = (int)(0x23 + 0);
    public const int Err_Invalid_Size_Handle = (int)(0x24 + 0);
    public const int Err_Invalid_Slot_Handle = (int)(0x25 + 0);
    public const int Err_Invalid_CharMap_Handle = (int)(0x26 + 0);
    public const int Err_Invalid_Cache_Handle = (int)(0x27 + 0);
    public const int Err_Invalid_Stream_Handle = (int)(0x28 + 0);
    public const int Err_Too_Many_Drivers = (int)(0x30 + 0);
    public const int Err_Too_Many_Extensions = (int)(0x31 + 0);
    public const int Err_Out_Of_Memory = (int)(0x40 + 0);
    public const int Err_Unlisted_Object = (int)(0x41 + 0);
    public const int Err_Cannot_Open_Stream = (int)(0x51 + 0);
    public const int Err_Invalid_Stream_Seek = (int)(0x52 + 0);
    public const int Err_Invalid_Stream_Skip = (int)(0x53 + 0);
    public const int Err_Invalid_Stream_Read = (int)(0x54 + 0);
    public const int Err_Invalid_Stream_Operation = (int)(0x55 + 0);
    public const int Err_Invalid_Frame_Operation = (int)(0x56 + 0);
    public const int Err_Nested_Frame_Access = (int)(0x57 + 0);
    public const int Err_Invalid_Frame_Read = (int)(0x58 + 0);
    public const int Err_Raster_Uninitialized = (int)(0x60 + 0);
    public const int Err_Raster_Corrupted = (int)(0x61 + 0);
    public const int Err_Raster_Overflow = (int)(0x62 + 0);
    public const int Err_Raster_Negative_Height = (int)(0x63 + 0);
    public const int Err_Too_Many_Caches = (int)(0x70 + 0);
    public const int Err_Invalid_Opcode = (int)(0x80 + 0);
    public const int Err_Too_Few_Arguments = (int)(0x81 + 0);
    public const int Err_Stack_Overflow = (int)(0x82 + 0);
    public const int Err_Code_Overflow = (int)(0x83 + 0);
    public const int Err_Bad_Argument = (int)(0x84 + 0);
    public const int Err_Divide_By_Zero = (int)(0x85 + 0);
    public const int Err_Invalid_Reference = (int)(0x86 + 0);
    public const int Err_Debug_OpCode = (int)(0x87 + 0);
    public const int Err_ENDF_In_Exec_Stream = (int)(0x88 + 0);
    public const int Err_Nested_DEFS = (int)(0x89 + 0);
    public const int Err_Invalid_CodeRange = (int)(0x8A + 0);
    public const int Err_Execution_Too_Long = (int)(0x8B + 0);
    public const int Err_Too_Many_Function_Defs = (int)(0x8C + 0);
    public const int Err_Too_Many_Instruction_Defs = (int)(0x8D + 0);
    public const int Err_Table_Missing = (int)(0x8E + 0);
    public const int Err_Horiz_Header_Missing = (int)(0x8F + 0);
    public const int Err_Locations_Missing = (int)(0x90 + 0);
    public const int Err_Name_Table_Missing = (int)(0x91 + 0);
    public const int Err_CMap_Table_Missing = (int)(0x92 + 0);
    public const int Err_Hmtx_Table_Missing = (int)(0x93 + 0);
    public const int Err_Post_Table_Missing = (int)(0x94 + 0);
    public const int Err_Invalid_Horiz_Metrics = (int)(0x95 + 0);
    public const int Err_Invalid_CharMap_Format = (int)(0x96 + 0);
    public const int Err_Invalid_PPem = (int)(0x97 + 0);
    public const int Err_Invalid_Vert_Metrics = (int)(0x98 + 0);
    public const int Err_Could_Not_Find_Context = (int)(0x99 + 0);
    public const int Err_Invalid_Post_Table_Format = (int)(0x9A + 0);
    public const int Err_Invalid_Post_Table = (int)(0x9B + 0);
    public const int Err_Syntax_Error = (int)(0xA0 + 0);
    public const int Err_Stack_Underflow = (int)(0xA1 + 0);
    public const int Err_Ignore = (int)(0xA2 + 0);
    public const int Err_Missing_Startfont_Field = (int)(0xB0 + 0);
    public const int Err_Missing_Font_Field = (int)(0xB1 + 0);
    public const int Err_Missing_Size_Field = (int)(0xB2 + 0);
    public const int Err_Missing_Chars_Field = (int)(0xB3 + 0);
    public const int Err_Missing_Startchar_Field = (int)(0xB4 + 0);
    public const int Err_Missing_Encoding_Field = (int)(0xB5 + 0);
    public const int Err_Missing_Bbx_Field = (int)(0xB6 + 0);

    /// <summary>
    /// Initialize a new FreeType library object. The set of modules that are registered by this function is determined at build time.
    /// </summary>
    /// <param name="alibrary">A handle to a new library object.</param>
    /// <returns>FreeType error code. 0 means success.</returns>
    [DllImport(FT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
    public static extern int FT_Init_FreeType(out IntPtr /*IntPtr LibraryRec_*/ alibrary);

    /// <summary>
    /// Return the version of the FreeType library being used. This is useful when dynamically linking to the library, since one cannot use the macros FREETYPE_MAJOR, FREETYPE_MINOR, and FREETYPE_PATCH.
    /// </summary>
    /// <param name="library">A source library handle.</param>
    /// <param name="amajor">The major version number.</param>
    /// <param name="aminor">The minor version number</param>
    /// <param name="apatch">The patch version number</param>
    [DllImport(FT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
    public static extern void FT_Library_Version(IntPtr /*LibraryRec_*/ library, ref int amajor, ref int aminor, ref int apatch);

    /// <summary>
    /// Destroy a given FreeType library object and all of its children, including resources, drivers, faces, sizes, etc.
    /// </summary>
    /// <param name="library">A handle to the target library object</param>
    /// <returns>FreeType error code. 0 means success</returns>
    [DllImport(FT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
    public static extern int FT_Done_FreeType(IntPtr /*LibraryRec_*/ library);

    /// <summary>
    /// This function calls FT_Open_Face to open a font by its pathname.
    /// </summary>
    /// <param name="library">A handle to the library resource.</param>
    /// <param name="filepathname">A path to the font file</param>
    /// <param name="face_index">The index of the face within the font. The first face has index 0</param>
    /// <param name="aface"> A handle to a new face object. If ?face_index? is greater than or equal to zero, it must be non-NULL. See FT_Open_Face for more details.</param>
    /// <returns>FreeType error code. 0 means success.</returns>
    [DllImport(FT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
    public static extern int FT_New_Face(IntPtr /*LibraryRec_*/ library, string filepathname, int face_index, out IntPtr /*IntPtr FaceRec*/ aface);

    /// <summary>
    /// This function calls FT_Open_Face to open a font which has been loaded into memory.
    /// You must not deallocate the memory before calling FT_Done_Face.
    /// </summary>
    /// <param name="library">A handle to the library resource</param>
    /// <param name="file_base">A pointer to the beginning of the font data</param>
    /// <param name="file_size">The size of the memory chunk used by the font data</param>
    /// <param name="face_index">The index of the face within the font. The first face has index 0</param>
    /// <param name="aface">A handle to a new face object. If ?face_index? is greater than or equal to zero, it must be non-NULL. See FT_Open_Face for more details.</param>
    /// <returns>FreeType error code. 0 means success.</returns>
    [DllImport(FT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
    public static extern int FT_New_Memory_Face(IntPtr /*LibraryRec_*/ library, [In] byte[] file_base, int file_size, int face_index, IntPtr /*IntPtr FaceRec*/ aface);

    /// <summary>
    /// Create a face object from a given resource described by FT_Open_Args.
    /// Unlike FreeType 1.x, this function automatically creates a glyph slot for the face object which can be accessed directly through ?face->glyph?.
    /// FT_Open_Face can be used to quickly check whether the font format of a given font resource is supported by FreeType. If the ?face_index? field is negative, the function's return value is 0 if the font format is recognized, or non-zero otherwise; the function returns a more or less empty face handle in ?*aface? (if ?aface? isn't NULL). The only useful field in this special case is ?face->num_faces? which gives the number of faces within the font file. After examination, the returned FT_Face structure should be deallocated with a call to FT_Done_Face.
    /// Each new face object created with this function also owns a default FT_Size object, accessible as ?face->size?.
    /// </summary>
    /// <param name="library">A handle to the library resource</param>
    /// <param name="args">A pointer to an ?FT_Open_Args? structure which must be filled by the caller.</param>
    /// <param name="face_index">The index of the face within the font. The first face has index 0</param>
    /// <param name="aface">A handle to a new face object. If ?face_index? is greater than or equal to zero, it must be non-NULL. See note below</param>
    /// <returns>FreeType error code. 0 means success</returns>
    [DllImport(FT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
    public static extern int FT_Open_Face(IntPtr /*LibraryRec_*/ library, FT_Open_Args args, int face_index, IntPtr /*IntPtr FaceRec*/ aface);

    /// <summary>
    /// This function calls FT_Attach_Stream to attach a file.
    /// </summary>
    /// <param name="face">The target face object.</param>
    /// <param name="filepathname">The pathname</param>
    /// <returns>FreeType error code. 0 means success</returns>
    [DllImport(FT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
    public static extern int FT_Attach_File(IntPtr /*FaceRec*/ face, string filepathname);

    /// <summary>
    /// ?Attach? data to a face object. Normally, this is used to read additional information for the face object. For example, you can attach an AFM file that comes with a Type 1 font to get the kerning values and other metrics
    /// </summary>
    /// <param name="face">The target face object</param>
    /// <param name="parameters">A pointer to FT_Open_Args which must be filled by the caller</param>
    /// <returns>FreeType error code. 0 means success</returns>
    [DllImport(FT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
    public static extern int FT_Attach_Stream(IntPtr /*FaceRec*/ face, ref FT_Open_Args parameters);

    /// <summary>
    /// Discard a given face object, as well as all of its child slots and sizes.
    /// </summary>
    /// <param name="face">A handle to a target face object.</param>
    /// <returns>FreeType error code. 0 means success</returns>
    [DllImport(FT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
    public static extern int FT_Done_Face(IntPtr /*FaceRec*/ face);

    /// <summary>
    /// This function calls FT_Request_Size to request the nominal size (in points).
    /// If either the character width or height is zero, it is set equal to the other value.
    /// If either the horizontal or vertical resolution is zero, it is set equal to the other value.
    /// A character width or height smaller than 1pt is set to 1pt; if both resolution values are zero, they are set to 72dpi.
    /// </summary>
    /// <param name="face">A handle to a target face object</param>
    /// <param name="char_width">The nominal width, in 26.6 fractional points</param>
    /// <param name="char_height">The nominal height, in 26.6 fractional points</param>
    /// <param name="horz_resolution">The horizontal resolution in dpi</param>
    /// <param name="vert_resolution">The vertical resolution in dpi</param>
    /// <returns>FreeType error code. 0 means success</returns>
    [DllImport(FT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
    public static extern int FT_Set_Char_Size(IntPtr /*FaceRec*/ face, int char_width, int char_height, uint horz_resolution, uint vert_resolution);

    /// <summary>
    /// This function calls FT_Request_Size to request the nominal size (in pixels).
    /// </summary>
    /// <param name="face">A handle to the target face object.</param>
    /// <param name="pixel_width">The nominal width, in pixels.</param>
    /// <param name="pixel_height">The nominal height, in pixels</param>
    /// <returns>FreeType error code. 0 means success</returns>
    [DllImport(FT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
    public static extern int FT_Set_Pixel_Sizes(IntPtr /*FaceRec*/ face, uint pixel_width, uint pixel_height);

    /// <summary>
    /// A function used to load a single glyph into the glyph slot of a face object.
    /// The loaded glyph may be transformed. See FT_Set_Transform for the details.
    /// </summary>
    /// <param name="face">A handle to the target face object where the glyph is loaded.</param>
    /// <param name="glyph_index">The index of the glyph in the font file. For CID-keyed fonts (either in PS or in CFF format) this argument specifies the CID value.</param>
    /// <param name="load_flags">A flag indicating what to load for this glyph. The FT_LOAD_XXX constants can be used to control the glyph loading process (e.g., whether the outline should be scaled, whether to load bitmaps or not, whether to hint the outline, etc).</param>
    /// <returns>FreeType error code. 0 means success.</returns>
    [DllImport(FT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
    public static extern int FT_Load_Glyph(IntPtr /*FaceRec*/ face, uint glyph_index, int load_flags);

    /// <summary>
    /// A function used to load a single glyph into the glyph slot of a face object, according to its character code.
    /// This function simply calls FT_Get_Char_Index and FT_Load_Glyph.
    /// </summary>
    /// <param name="face">A handle to a target face object where the glyph is loaded.</param>
    /// <param name="char_code">The glyph's character code, according to the current charmap used in the face</param>
    /// <param name="load_flags">A flag indicating what to load for this glyph. The FT_LOAD_XXX constants can be used to control the glyph loading process (e.g., whether the outline should be scaled, whether to load bitmaps or not, whether to hint the outline, etc).</param>
    /// <returns>FreeType error code. 0 means success</returns>
    [DllImport(FT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
    public static extern int FT_Load_Char(IntPtr /*FaceRec*/ face, uint char_code, int load_flags);

    /// <summary>
    /// A function used to set the transformation that is applied to glyph images when they are loaded into a glyph slot through FT_Load_Glyph.
    /// The transformation is only applied to scalable image formats after the glyph has been loaded. It means that hinting is unaltered by the transformation and is performed on the character size given in the last call to FT_Set_Char_Size or FT_Set_Pixel_Sizes.
    /// Note that this also transforms the ?face.glyph.advance? field, but not the values in ?face.glyph.metrics?
    /// </summary>
    /// <param name="face">A handle to the source face object</param>
    /// <param name="matrix">A pointer to the transformation's 2x2 matrix. Use 0 for the identity matrix</param>
    /// <param name="delta">A pointer to the translation vector. Use 0 for the null vector</param>
    [DllImport(FT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
    public static extern void FT_Set_Transform(IntPtr /*FaceRec*/ face, ref FT_Matrix matrix, ref FT_Vector delta);

    /// <summary>
    /// Convert a given glyph image to a bitmap. It does so by inspecting the glyph image format, finding the relevant renderer, and invoking it
    /// </summary>
    /// <param name="slot">A handle to the glyph slot containing the image to convert</param>
    /// <param name="render_mode">This is the render mode used to render the glyph image into a bitmap. See FT_Render_Mode for a list of possible values</param>
    /// <returns>FreeType error code. 0 means success</returns>
    [DllImport(FT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
    public static extern int FT_Render_Glyph(ref FT_GlyphSlotRec slot, FT_Render_Mode render_mode);

    /// <summary>
    /// Return the kerning vector between two glyphs of a same face
    /// </summary>
    /// <param name="face">A handle to a source face object</param>
    /// <param name="left_glyph">The index of the left glyph in the kern pair</param>
    /// <param name="right_glyph">The index of the right glyph in the kern pair</param>
    /// <param name="kern_mode">See FT_Kerning_Mode for more information. Determines the scale and dimension of the returned kerning vector</param>
    /// <param name="akerning">The kerning vector. This is either in font units or in pixels (26.6 format) for scalable formats, and in pixels for fixed-sizes formats</param>
    /// <returns>FreeType error code. 0 means success</returns>
    [DllImport(FT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
    public static extern int FT_Get_Kerning(IntPtr /*FaceRec*/ face, uint left_glyph, uint right_glyph, uint kern_mode, out FT_Vector akerning);

    /// <summary>
    /// Retrieve the ASCII name of a given glyph in a face. This only works for those faces where FT_HAS_GLYPH_NAMES(face) returns 1
    /// An error is returned if the face doesn't provide glyph names or if the glyph index is invalid. In all cases of failure, the first byte of ?buffer? is set to 0 to indicate an empty name.
    /// The glyph name is truncated to fit within the buffer if it is too long. The returned string is always zero-terminated.
    /// This function is not compiled within the library if the config macro ?FT_CONFIG_OPTION_NO_GLYPH_NAMES? is defined in ?include/freetype/config/ftoptions.h?
    /// </summary>
    /// <param name="face">A handle to a source face object</param>
    /// <param name="glyph_index">The glyph index</param>
    /// <param name="buffer">A pointer to a target buffer where the name is copied to</param>
    /// <param name="buffer_max">The maximal number of bytes available in the buffer</param>
    /// <returns>FreeType error code. 0 means success</returns>
    [DllImport(FT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
    public static extern int FT_Get_Glyph_Name(IntPtr /*FaceRec*/ face, uint glyph_index, IntPtr buffer, uint buffer_max);

    /// <summary>
    /// Retrieve the ASCII Postscript name of a given face, if available. This only works with Postscript and TrueType fonts
    /// The returned pointer is owned by the face and is destroyed with it
    /// </summary>
    /// <param name="face">A handle to the source face object</param>
    /// <returns>A pointer to the face's Postscript name. NULL if unavailable</returns>
    [DllImport(FT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
    public static extern IntPtr /*sbyte*/ FT_Get_Postscript_Name(IntPtr /*FaceRec*/ face);

    /// <summary>
    /// Select a given charmap by its encoding tag (as listed in ?freetype.h?).
    /// This function returns an error if no charmap in the face corresponds to the encoding queried here.
    /// Because many fonts contain more than a single cmap for Unicode encoding, this function has some special code to select the one which covers Unicode best. It is thus preferable to FT_Set_Charmap in this case
    /// </summary>
    /// <param name="face">A handle to the source face object</param>
    /// <param name="encoding">A handle to the selected encoding</param>
    /// <returns>FreeType error code. 0 means success</returns>
    [DllImport(FT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
    public static extern int FT_Select_Charmap(IntPtr /*FaceRec*/ face, FT_Encoding encoding);

    /// <summary>
    /// Select a given charmap for character code to glyph index mapping
    /// This function returns an error if the charmap is not part of the face (i.e., if it is not listed in the ?face->charmaps? table)
    /// </summary>
    /// <param name="face">A handle to the source face object</param>
    /// <param name="charmap">A handle to the selected charmap</param>
    /// <returns>FreeType error code. 0 means success</returns>
    [DllImport(FT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
    public static extern int FT_Set_Charmap(IntPtr /*FaceRec*/ face, ref FT_CharMapRec charmap);

    /// <summary>
    /// Retrieve index of a given charmap
    /// </summary>
    /// <param name="charmap">A handle to a charmap</param>
    /// <returns>The index into the array of character maps within the face to which ?charmap? belongs</returns>
    [DllImport(FT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
    public static extern int FT_Get_Charmap_Index(ref FT_CharMapRec charmap);

    /// <summary>
    /// Return the glyph index of a given character code. This function uses a charmap object to do the mapping
    /// If you use FreeType to manipulate the contents of font files directly, be aware that the glyph index returned by this function doesn't always correspond to the internal indices used within the file. This is done to ensure that value 0 always corresponds to the ?missing glyph?.
    /// </summary>
    /// <param name="face">A handle to the source face object</param>
    /// <param name="charcode">The character code</param>
    /// <returns>The glyph index. 0 means ?undefined character code?</returns>
    [DllImport(FT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
    public static extern uint FT_Get_Char_Index(IntPtr /*FaceRec*/ face, uint charcode);

    /// <summary>
    /// This function is used to return the first character code in the current charmap of a given face. It also returns the corresponding glyph index.
    /// You should use this function with FT_Get_Next_Char to be able to parse all character codes available in a given charmap.
    /// Note that ?agindex? is set to 0 if the charmap is empty. The result itself can be 0 in two cases: if the charmap is empty or when the value 0 is the first valid character code
    /// </summary>
    /// <param name="face">A handle to the source face object</param>
    /// <param name="agindex">Glyph index of first character code. 0 if charmap is empty</param>
    /// <returns>The charmap's first character code</returns>
    [DllImport(FT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
    public static extern uint FT_Get_First_Char(IntPtr /*FaceRec*/ face, [In, Out] uint[] agindex);

    /// <summary>
    /// This function is used to return the next character code in the current charmap of a given face following the value ?char_code?, as well as the corresponding glyph index.
    /// You should use this function with FT_Get_First_Char to walk over all character codes available in a given charmap. See the note for this function for a simple code example.
    /// Note that ?*agindex? is set to 0 when there are no more codes in the charmap.
    /// </summary>
    /// <param name="face">A handle to the source face object</param>
    /// <param name="char_code">The starting character code</param>
    /// <param name="agindex">Glyph index of first character code. 0 if charmap is empty</param>
    /// <returns>The charmap's next character code</returns>
    [DllImport(FT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
    public static extern uint FT_Get_Next_Char(IntPtr /*FaceRec*/ face, uint char_code, [In, Out] uint[] agindex);

    /// <summary>
    /// Return the glyph index of a given glyph name. This function uses driver specific objects to do the translation
    /// </summary>
    /// <param name="face">A handle to the source face object</param>
    /// <param name="glyph_name">The glyph name</param>
    /// <returns>The glyph index. 0 means ?undefined character code?</returns>
    [DllImport(FT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
    public static extern uint FT_Get_Name_Index(IntPtr /*FaceRec*/ face, [In, Out] sbyte[] glyph_name);

    /// <summary>
    /// A very simple function used to perform the computation ?(a*b)/c? with maximal accuracy (it uses a 64-bit intermediate integer whenever necessary).
    /// This function isn't necessarily as fast as some processor specific operations, but is at least completely portable.
    /// </summary>
    /// <param name="a">The first multiplier</param>
    /// <param name="b">The second multiplier</param>
    /// <param name="c">The divisor</param>
    /// <returns>The result of ?(a*b)/c?. This function never traps when trying to divide by zero; it simply returns ?MaxInt? or ?MinInt? depending on the signs of ?a? and ?b?</returns>
    [DllImport(FT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
    public static extern int FT_MulDiv(int a, int b, int c);

    /// <summary>
    /// A very simple function used to perform the computation ?(a*b)/0x10000? with maximal accuracy. Most of the time this is used to multiply a given value by a 16.16 fixed float factor
    /// This function has been optimized for the case where the absolute value of ?a? is less than 2048, and ?b? is a 16.16 scaling factor. As this happens mainly when scaling from notional units to fractional pixels in FreeType, it resulted in noticeable speed improvements between versions 2.x and 1.x.
    /// As a conclusion, always try to place a 16.16 factor as the second argument of this function; this can make a great difference
    /// </summary>
    /// <param name="a">The first multiplier</param>
    /// <param name="b">The second multiplier. Use a 16.16 factor here whenever possible</param>
    /// <returns>The result of ?(a*b)/0x10000?</returns>
    [DllImport(FT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
    public static extern int FT_MulFix(int a, int b);

    /// <summary>
    /// A very simple function used to perform the computation ?(a*0x10000)/b? with maximal accuracy. Most of the time, this is used to divide a given value by a 16.16 fixed float factor
    /// The optimization for FT_DivFix() is simple: If (a &lt;&lt; 16) fits in 32 bits, then the division is computed directly. Otherwise, we use a specialized version of FT_MulDiv
    /// </summary>
    /// <param name="a">The first multiplier</param>
    /// <param name="b">The second multiplier. Use a 16.16 factor here whenever possible</param>
    /// <returns>The result of ?(a*0x10000)/b?</returns>
    [DllImport(FT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
    public static extern int FT_DivFix(int a, int b);

    /// <summary>
    /// A very simple function used to round a 16.16 fixed number
    /// </summary>
    /// <param name="a">The number to be rounded</param>
    /// <returns>The result of ?(a + 0x8000) &amp; -0x10000?</returns>
    [DllImport(FT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
    public static extern int FT_RoundFix(int a);

    /// <summary>
    /// A very simple function used to compute the ceiling function of a 16.16 fixed number
    /// </summary>
    /// <param name="a">The number for which the ceiling function is to be computed</param>
    /// <returns>The result of ?(a + 0x10000 - 1) &amp;-0x10000?</returns>
    [DllImport(FT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
    public static extern int FT_CeilFix(int a);

    /// <summary>
    /// A very simple function used to compute the floor function of a 16.16 fixed number
    /// </summary>
    /// <param name="a">The number for which the floor function is to be computed</param>
    /// <returns>The result of ?a &amp; -0x10000?</returns>
    [DllImport(FT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
    public static extern int FT_FloorFix(int a);

    /// <summary>
    /// Transform a single vector through a 2x2 matrix.  
    /// The result is undefined if either ?vector? or ?matrix? is invalid 
    /// </summary>
    /// <param name="vec">The target vector to transform</param>
    /// <param name="matrix">A pointer to the source 2x2 matrix</param>
    [DllImport(FT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
    public static extern void FT_Vector_Transform(ref FT_Vector vec, ref FT_Matrix matrix);
    
    /// <summary>
    /// A function used to extract a glyph image from a slot.
    /// </summary>
    /// <param name="slot">A handle to the source glyph slot.</param>
    /// <param name="aglyph">A handle to the glyph object</param>
    [DllImport(FT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
    public static extern int FT_Get_Glyph(IntPtr /*GlyphSlotRec*/ slot, out IntPtr /*FT_Glyph*/ aglyph);

    /// <summary>
    /// Convert a given glyph object to a bitmap glyph object.
    /// </summary>
    /// <param name="glyph">A pointer to a handle to the target glyph.</param>
    /// <param name="render_mode">An enumeration that describe how the data is rendered.</param>
    /// <param name="origin">A pointer to a vector used to translate the glyph image before rendering. Can be 0 (if no translation). The origin is expressed in 26.6 pixels.</param>
    /// <param name="destroy">A boolean that indicates that the original glyph image should be destroyed by this function. It is never destroyed in case of error.</param>
    [DllImport(FT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
    public static extern int FT_Glyph_To_Bitmap(ref IntPtr /*FT_Glyph* */ glyph, FT_Render_Mode render_mode, IntPtr /*FT_Vector* */ origin, byte /*FT_Bool*/destroy);

    /// <summary>
    /// Destroys a given glyph.
    /// </summary>
    /// <param name="glyph">A pointer to a handle to the target glyph.</param>
    [DllImport(FT_NATIVE_LIBRARY, CallingConvention = CALLING_CONVENTION), SuppressUnmanagedCodeSecurity]
    public static extern int FT_Done_Glyph(IntPtr /*FT_Glyph* */ glyph);
  }
}

