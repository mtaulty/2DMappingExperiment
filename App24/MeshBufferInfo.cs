namespace App24
{
  using Windows.Graphics.DirectX;
  using D3D11 = SharpDX.Direct3D11;

  class MeshBufferInfo
  {
    public D3D11.Buffer vertexBuffer;
    public D3D11.Buffer indexBuffer;
    public D3D11.Buffer constantsBuffer;
    public DirectXPixelFormat indexFormat;
    public int vertexCount;
    public int indexCount;
    public uint vertexStride;
  }
}

