namespace App24
{
  using System;
  using Windows.Storage.Streams;

  public static class BufferExtensions
  {
    public unsafe static IntPtr AsIntPtr(this IBuffer buffer, out int size)
    {
      byte* bufferPointer = null;
      size = (int)buffer.Length;
      ((IBufferByteAccess)buffer).Buffer(out bufferPointer);
      return (new IntPtr(bufferPointer));
    }
  }
}
