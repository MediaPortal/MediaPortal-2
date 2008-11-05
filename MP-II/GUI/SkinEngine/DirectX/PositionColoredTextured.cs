using System.Runtime.InteropServices;
using SlimDX;
using SlimDX.Direct3D9;

namespace MediaPortal.SkinEngine.DirectX
{
  /// <summary>Vertex with Position and two sets of texture coordinates</summary>
  [StructLayout(LayoutKind.Sequential)]
  public struct PositionColoredTextured
  {
    public float X; //0..3
    public float Y; //4..7
    public float Z; //8..11
    public int Color; //12..15
    public float Tu1; //16..19
    public float Tv1; //20..23

    public static readonly VertexFormat Format = VertexFormat.Position | VertexFormat.Texture1 | VertexFormat.Diffuse;

    public static readonly VertexElement[] Declarator = new VertexElement[]
      {
        new VertexElement(0, 0, DeclarationType.Float3, DeclarationMethod.Default, DeclarationUsage.Position, 0),
        new VertexElement(0, 12, DeclarationType.Color, DeclarationMethod.Default, DeclarationUsage.Color, 0),
        new VertexElement(0, 16, DeclarationType.Float2, DeclarationMethod.Default, DeclarationUsage.TextureCoordinate,0),
        VertexElement.VertexDeclarationEnd
      };

    public static readonly int StrideSize = 24;


    public static void Set(VertexBuffer buffer, ref PositionColoredTextured[] verts)
    {
      using (DataStream stream = buffer.Lock(0, 0, LockFlags.None))
      {
        stream.WriteRange(verts);
      }
      buffer.Unlock();
    }
    public static VertexBuffer Create(int verticeCount)
    {
      return new VertexBuffer(GraphicsDevice.Device, PositionColoredTextured.StrideSize * verticeCount, Usage.WriteOnly, PositionColored2Textured.Format, Pool.Default);
    }
  }
}