#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

/* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * 

Title : Vertex.cs
Author : Chad Vernon
URL : http://www.c-unit.com

Description : Custom vertices.

Created :  10/26/2005
Modified : 12/20/2005

Copyright (c) 2006 C-Unit.com

This software is provided 'as-is', without any express or implied warranty. In no event will 
the authors be held liable for any damages arising from the use of this software.

Permission is granted to anyone to use this software for any purpose, including commercial 
applications, and to alter it and redistribute it freely, subject to the following restrictions:

    1. The origin of this software must not be misrepresented; you must not claim that you wrote 
       the original software. If you use this software in a product, an acknowledgment in the 
       product documentation would be appreciated but is not required.

    2. Altered source versions must be plainly marked as such, and must not be misrepresented 
       as being the original software.

    3. This notice may not be removed or altered from any source distribution.

* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * */

using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.Direct3D9;

namespace MediaPortal.UI.SkinEngine.DirectX
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

    public static readonly VertexFormat Format = VertexFormat.Position | VertexFormat.Texture2 | VertexFormat.Diffuse;

    public static readonly VertexElement[] Declarator = new VertexElement[]
      {
        new VertexElement(0, 0, DeclarationType.Float3, DeclarationMethod.Default, DeclarationUsage.Position, 0),
        new VertexElement(0, 12, DeclarationType.Color, DeclarationMethod.Default, DeclarationUsage.Color, 0),
        new VertexElement(0, 16, DeclarationType.Float2, DeclarationMethod.Default, DeclarationUsage.TextureCoordinate, 0),
        VertexElement.VertexDeclarationEnd
      };

    public static readonly int StrideSize = 24;

    public PositionColoredTextured(float x, float y, float z, float u1, float v1, Color4 color)
    {
      X = x;
      Y = y;
      Z = z;
      Tu1 = u1;
      Tv1 = v1;
      Color = color.ToBgra();
    }

    public PositionColoredTextured(Vector3 position, float u1, float v1, Color4 color)
    {
      X = position.X;
      Y = position.Y;
      Z = position.Z;
      Tu1 = u1;
      Tv1 = v1;
      Color = color.ToBgra();
    }

    /// <summary>Gets and sets the position</summary>
    public Vector3 Position
    {
      get { return new Vector3(X, Y, Z); }
      set
      {
        X = value.X;
        Y = value.Y;
        Z = value.Z;
      }
    }

    public static PositionColoredTextured[] CreateQuad_Fan(float left, float top, float right, float bottom,
      float uLeft, float vTop, float uRight, float vBottom,
      float zPos, Color4 color)
    {
      PositionColoredTextured[] verts = new PositionColoredTextured[4];

      // Upper left
      verts[0].X = left;
      verts[0].Y = top;
      verts[0].Color = color.ToBgra();
      verts[0].Tu1 = uLeft;
      verts[0].Tv1 = vTop;
      verts[0].Z = zPos;

      // Bottom left
      verts[1].X = left;
      verts[1].Y = bottom;
      verts[1].Color = color.ToBgra();
      verts[1].Tu1 = uLeft;
      verts[1].Tv1 = vBottom;
      verts[1].Z = zPos;

      // Bottom right
      verts[2].X = right;
      verts[2].Y = bottom;
      verts[2].Color = color.ToBgra();
      verts[2].Tu1 = uRight;
      verts[2].Tv1 = vBottom;
      verts[2].Z = zPos;

      // Upper right
      verts[3].X = right;
      verts[3].Y = top;
      verts[3].Color = color.ToBgra();
      verts[3].Tu1 = uRight;
      verts[3].Tv1 = vTop;
      verts[3].Z = zPos;

      return verts;
    }

    public static PositionColoredTextured[] CreateQuad_Fan(float left, float top, float right, float bottom, float zPos, Color4 color)
    {
      return CreateQuad_Fan(left, top, right, bottom, 0.0f, 0.0f, 1.0f, 1.0f, zPos, color);
    }
  }
}