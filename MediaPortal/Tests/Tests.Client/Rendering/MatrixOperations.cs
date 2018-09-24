using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SharpDX;

namespace Tests.Client.Rendering
{
  class MatrixOperations
  {
    [Test]
    public void Matrix3x2_to_Matrix()
    {
      Matrix3x2 source = Matrix3x2.Identity;
      source *= Matrix3x2.Translation(0.1f, 0.2f); // Row 3, TranslationVector, M31, M32
      source *= Matrix3x2.Scaling(0.3f, 0.4f); // Row 1+2, ScaleVector, M11, M22
     // source *= Matrix3x2.Rotation(45f); // Row 1+2, ScaleVector, M11, M22

      Matrix target = Matrix.Identity;
      target *= Matrix.Translation(0.1f, 0.2f, 0f);
      target *= Matrix.Scaling(0.3f, 0.4f, 1f);

      Matrix target2 = Matrix.Identity;
      target2.M11 = source.M11;
      target2.M22 = source.M22;
      target2.M41 = source.M31;
      target2.M42 = source.M32;

      var eq = target == target2;
    }

    public static void Rotation(float angle, out Matrix3x2 result)
    {
      float num1 = (float)Math.Cos((double)angle);
      float num2 = (float)Math.Sin((double)angle);
      result = Matrix3x2.Identity;
      result.M11 = num1;
      result.M12 = num2;
      result.M21 = -num2;
      result.M22 = num1;
    }
  }
}
