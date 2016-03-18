//*****************************************************************************************
//                           LICENSE INFORMATION
//*****************************************************************************************
//   PC_VolumeControl Version 1.0.0.0
//   A class library for creating a mixer control and controlling the volume on your computer
//
//   Copyright (C) 2007  
//   Richard L. McCutchen 
//   Email: psychocoder@dreamincode.net
//   Created: 04OCT06
//
//   This program is free software: you can redistribute it and/or modify
//   it under the terms of the GNU General Public License as published by
//   the Free Software Foundation, either version 3 of the License, or
//   (at your option) any later version.
//
//   This program is distributed in the hope that it will be useful,
//   but WITHOUT ANY WARRANTY; without even the implied warranty of
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//   GNU General Public License for more details.
//
//   You should have received a copy of the GNU General Public License
//   along with this program.  If not, see <http://www.gnu.org/licenses/>.
//*****************************************************************************************

using System;
using System.Runtime.InteropServices;

namespace Audio
{
	public static class VolumeMixer
	{
		public static float? GetApplicationVolume(int pid)
		{
			ISimpleAudioVolume volume = GetVolumeObject(pid);
			if (volume == null)
				return null;

			float level;
			volume.GetMasterVolume(out level);
			Marshal.ReleaseComObject(volume);
			return level * 100;
		}

		public static bool? GetApplicationMute(int pid)
		{
			ISimpleAudioVolume volume = GetVolumeObject(pid);
			if (volume == null)
				return null;

			bool mute;
			volume.GetMute(out mute);
			Marshal.ReleaseComObject(volume);
			return mute;
		}

		public static void SetApplicationVolume(int pid, float level)
		{
			ISimpleAudioVolume volume = GetVolumeObject(pid);
			if (volume == null)
				return;

			Guid guid = Guid.Empty;
			volume.SetMasterVolume(level / 100, ref guid);
			Marshal.ReleaseComObject(volume);
		}

		public static void SetApplicationMute(int pid, bool mute)
		{
			ISimpleAudioVolume volume = GetVolumeObject(pid);
			if (volume == null)
				return;

			Guid guid = Guid.Empty;
			volume.SetMute(mute, ref guid);
			Marshal.ReleaseComObject(volume);
		}

		private static ISimpleAudioVolume GetVolumeObject(int pid)
		{
			// get the speakers (1st render + multimedia) device
			IMMDeviceEnumerator deviceEnumerator = (IMMDeviceEnumerator)(new MMDeviceEnumerator());
			IMMDevice speakers;
			deviceEnumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out speakers);

			// activate the session manager. we need the enumerator
			Guid IID_IAudioSessionManager2 = typeof(IAudioSessionManager2).GUID;
			object o;
			speakers.Activate(IID_IAudioSessionManager2, 0, IntPtr.Zero, out o);
			IAudioSessionManager2 mgr = (IAudioSessionManager2)o;

			// enumerate sessions for on this device
			IAudioSessionEnumerator sessionEnumerator;
			mgr.GetSessionEnumerator(out sessionEnumerator);
			int count;
			sessionEnumerator.GetCount(out count);

			// search for an audio session with the required name
			// NOTE: we could also use the process id instead of the app name (with IAudioSessionControl2)
			ISimpleAudioVolume volumeControl = null;
			for (int i = 0; i < count; i++)
			{
				IAudioSessionControl2 ctl;
				sessionEnumerator.GetSession(i, out ctl);
				int cpid;
				ctl.GetProcessId(out cpid);

				if (cpid == pid)
				{
					volumeControl = ctl as ISimpleAudioVolume;
					break;
				}
				Marshal.ReleaseComObject(ctl);
			}
			Marshal.ReleaseComObject(sessionEnumerator);
			Marshal.ReleaseComObject(mgr);
			Marshal.ReleaseComObject(speakers);
			Marshal.ReleaseComObject(deviceEnumerator);
			return volumeControl;
		}
	}
}