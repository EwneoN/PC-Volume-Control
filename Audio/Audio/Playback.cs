using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Audio
{
  public static class Playback
  {
    [DllImport(@"Native\EndPointController.dll", EntryPoint = "GetDevices", CallingConvention = CallingConvention.Cdecl)]
    static extern IntPtr GetDevices(ref int deviceCount);

    [DllImport(@"Native\EndPointController.dll", EntryPoint = "SetDefaultAudioPlaybackDevice", CallingConvention = CallingConvention.Cdecl)]
    static extern int SetDefaultAudioPlaybackDevice([MarshalAs(UnmanagedType.LPWStr)]string deviceId);

    [DllImport(@"Native\EndPointController.dll", EntryPoint = "FreeAudioDeviceArray", CallingConvention = CallingConvention.Cdecl)]
    static extern void FreeAudioDeviceArray(IntPtr pointer);

    public static Dictionary<string, PlaybackDevice> GetAudioDevices()
    {
      Dictionary<string, PlaybackDevice> devices = new Dictionary<string, PlaybackDevice>();

      int deviceCount = 0;

      IntPtr originalPointer = GetDevices(ref deviceCount);

      try
      {
        IntPtr tempPointer = originalPointer;

        int dataEntrySize = Marshal.SizeOf(typeof(PlaybackDevice));

        for (int i = 0; i < deviceCount; i++)
        {
          PlaybackDevice device = (PlaybackDevice)Marshal.PtrToStructure(tempPointer, typeof(PlaybackDevice));

          devices.Add(device.DeviceId, device);

          tempPointer = new IntPtr(tempPointer.ToInt32() + dataEntrySize);
        }
      }
      finally
      {
        FreeAudioDeviceArray(originalPointer);
      }

      return devices;
    }

    public static void SetDefaultPlaybackDevice(string deviceId)
    {
      int ret = SetDefaultAudioPlaybackDevice(deviceId);

      if (ret < 0)
      {
        throw new Exception("Operation has failed");
      }
    }
  }
}