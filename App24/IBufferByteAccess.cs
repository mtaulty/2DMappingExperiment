namespace App24
{
  [
    System.Runtime.InteropServices.GuidAttribute("905a0fef-bc53-11df-8c49-001e4fc686da"),
    System.Runtime.InteropServices.InterfaceType(
      System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)
  ]
  interface IBufferByteAccess
  {
    unsafe void Buffer(out byte* pByte);
  }
}
