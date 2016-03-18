﻿namespace Audio
{
	public class AudioDevice
	{
		private string _Id;
		private string _Name;
		private bool _IsCurrentDevice;

		public string Id
		{
			get { return _Id; }
			set { _Id = value; }
		}

		public string Name
		{
			get { return _Name; }
			set { _Name = value; }
		}

		public bool IsCurrentDevice
		{
			get { return _IsCurrentDevice; }
			set { _IsCurrentDevice = value; }
		}

    public AudioDevice() { }

    public AudioDevice(Win32AudioDevice device)
    {
      _Id = device.Id;
      _Name = device.InterfaceFriendlyName;
    }

    public AudioDevice(Win32AudioDevice device, bool isCurrentDevice)
    {
      _Id = device.Id;
      _Name = device.InterfaceFriendlyName;
      _IsCurrentDevice = isCurrentDevice;
    }
	}
}
