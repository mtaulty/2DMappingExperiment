﻿namespace App24
{
  using SharpDX.Mathematics.Interop;

  static class MatrixExtensions
  {
    public static RawMatrix ToRawMatrix(this System.Numerics.Matrix4x4 matrix)
    {
      return (new RawMatrix()
      {
        M11 = matrix.M11,
        M12 = matrix.M12,
        M13 = matrix.M13,
        M14 = matrix.M14,
        M21 = matrix.M21,
        M22 = matrix.M22,
        M23 = matrix.M23,
        M24 = matrix.M24,
        M31 = matrix.M31,
        M32 = matrix.M32,
        M33 = matrix.M33,
        M34 = matrix.M34,
        M41 = matrix.M41,
        M42 = matrix.M42,
        M43 = matrix.M43,
        M44 = matrix.M44
      });
    }
  }
}
