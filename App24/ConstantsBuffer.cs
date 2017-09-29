namespace App24
{
  using SharpDX.Mathematics.Interop;

  struct PerModelConstantsBuffer
  {
    public RawMatrix modelToWorld;
  }
  struct ViewConstantsBuffer
  {
    public RawMatrix view;
    public RawMatrix projection;
  }
}

