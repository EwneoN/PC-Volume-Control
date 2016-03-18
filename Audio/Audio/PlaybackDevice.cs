using System.Runtime.InteropServices;

namespace Audio
{
  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct PlaybackDevice
  {
    [MarshalAs(UnmanagedType.LPWStr)]
    public string DeviceId;
    [MarshalAs(UnmanagedType.LPWStr)]
    public string DeviceName;
  }
}
