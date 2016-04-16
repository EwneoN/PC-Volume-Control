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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Audio
{
	// audio utilities
	public static class AudioUtilities
	{
		[DllImport("ole32.dll")]
		public static extern int PropVariantClear(ref PROPVARIANT pvar);

		public static IAudioSessionManager2 GetAudioSessionManager()
		{
			IMMDevice speakers = GetCurrentSpeakers();
			if (speakers == null)
				return null;

			// win7+ only
			object o;
			if (speakers.Activate(typeof(IAudioSessionManager2).GUID, CLSCTX.CLSCTX_ALL, IntPtr.Zero, out o) != 0 || o == null)
				return null;

			return o as IAudioSessionManager2;
		}

		public static IAudioEndpointVolume GetAudioEndpointVolume(IMMDevice device = null)
		{
			IMMDevice speakers = device ?? GetCurrentSpeakers();
			if (speakers == null)
				return null;

			// win7+ only
			object o;
			if (speakers.Activate(typeof(IAudioEndpointVolume).GUID, CLSCTX.CLSCTX_ALL, IntPtr.Zero, out o) != 0 || o == null)
				return null;

			return o as IAudioEndpointVolume;
		}

		public static Win32AudioDevice GetSpeakersDevice()
		{
			return CreateDevice(GetCurrentSpeakers());
		}

		public static Win32AudioDevice CreateDevice(IMMDevice dev)
		{
			if (dev == null)
				return null;

			string id;
			dev.GetId(out id);
			DEVICE_STATE state;
			dev.GetState(out state);
			Dictionary<string, object> properties = new Dictionary<string, object>();
			IPropertyStore store;
			dev.OpenPropertyStore(STGM.STGM_READ, out store);
			if (store != null)
			{
				int propCount;
				store.GetCount(out propCount);
				for (int j = 0; j < propCount; j++)
				{
					PROPERTYKEY pk;
					if (store.GetAt(j, out pk) == 0)
					{
						PROPVARIANT value = new PROPVARIANT();
						int hr = store.GetValue(ref pk, ref value);
						object v = value.GetValue();
						try
						{
							if (value.vt != VARTYPE.VT_BLOB) // for some reason, this fails?
							{
								PropVariantClear(ref value);
							}
						}
						catch
						{
						}
						string name = pk.ToString();
						properties[name] = v;
					}
				}
			}
			return new Win32AudioDevice(id, (AudioDeviceState)state, properties);
		}

		public static IList<Win32AudioDevice> GetAllDevices()
		{
			List<Win32AudioDevice> list = new List<Win32AudioDevice>();
			IMMDeviceEnumerator deviceEnumerator = null;
			try
			{
				deviceEnumerator = (IMMDeviceEnumerator)(new MMDeviceEnumerator());
			}
			catch
			{
			}
			if (deviceEnumerator == null)
				return list;

			IMMDeviceCollection collection;
			deviceEnumerator.EnumAudioEndpoints(EDataFlow.eAll, DEVICE_STATE.MASK_ALL, out collection);
			if (collection == null)
				return list;

			int count;
			collection.GetCount(out count);
			for (int i = 0; i < count; i++)
			{
				IMMDevice dev;
				collection.Item(i, out dev);
				if (dev != null)
				{
					list.Add(CreateDevice(dev));
				}
			}
			return list;
		}

		public static IMMDevice GetCurrentSpeakers()
		{
			// get the speakers (1st render + multimedia) device
			try
			{
				IMMDeviceEnumerator deviceEnumerator = (IMMDeviceEnumerator)new MMDeviceEnumerator();
				IMMDevice speakers;
				deviceEnumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out speakers);
				return speakers;
			}
			catch
			{
				return null;
			}
		}

    public static IList<Win32AudioDevice> GetAllActiveDevices()
    {
      return GetAllDevices()?.Where(d => d.State == AudioDeviceState.Active).ToList();
    }

    public static IList<Win32AudioDevice> GetAllActiveSpeakers()
    {//Realtek Digital Output
      return GetAllActiveDevices()
        ?.Where(d => d.State == AudioDeviceState.Active && 
                     !Regex.IsMatch(d.Description, @"((m|M)icrophone|MICROPHONE)") &&
                     !Regex.IsMatch(d.Description, @"((s|S)tereo (m|M)ix|STEREO MIX)") &&
                     !Regex.IsMatch(d.Description, @"((r|R)ealtek (d|D)igital (o|O)utput|REALTEK DIGITAL OUTPUT)")).ToList();
    }

    public static IList<AudioSession> GetAllSessions()
		{
			List<AudioSession> list = new List<AudioSession>();
			IAudioSessionManager2 mgr = GetAudioSessionManager();
			if (mgr == null)
				return list;

			IAudioSessionEnumerator sessionEnumerator;
			mgr.GetSessionEnumerator(out sessionEnumerator);
			int count;
			sessionEnumerator.GetCount(out count);

			for (int i = 0; i < count; i++)
			{
				IAudioSessionControl2 ctl;
				sessionEnumerator.GetSession(i, out ctl);
				if (ctl == null)
					continue;

				IAudioSessionControl2 ctl2 = ctl;
				list.Add(new AudioSession(ctl2));
			}
			Marshal.ReleaseComObject(sessionEnumerator);
			Marshal.ReleaseComObject(mgr);
			return list;
		}

		public static AudioSession GetProcessSession()
		{
			int id = Process.GetCurrentProcess().Id;
			foreach (AudioSession session in GetAllSessions())
			{
				if (session.ProcessId == id)
					return session;

				session.Dispose();
			}
			return null;
		}
	}

	[Flags]
	public enum CLSCTX
	{
		CLSCTX_INPROC_SERVER = 0x1,
		CLSCTX_INPROC_HANDLER = 0x2,
		CLSCTX_LOCAL_SERVER = 0x4,
		CLSCTX_REMOTE_SERVER = 0x10,
		CLSCTX_ALL = CLSCTX_INPROC_SERVER | CLSCTX_INPROC_HANDLER | CLSCTX_LOCAL_SERVER | CLSCTX_REMOTE_SERVER
	}

	public enum STGM
	{
		STGM_READ = 0x00000000,
	}

	public enum EDataFlow
	{
		eRender,
		eCapture,
		eAll,
	}

	public enum ERole
	{
		eConsole,
		eMultimedia,
		eCommunications,
	}

	public enum DEVICE_STATE
	{
		ACTIVE = 0x00000001,
		DISABLED = 0x00000002,
		NOTPRESENT = 0x00000004,
		UNPLUGGED = 0x00000008,
		MASK_ALL = 0x0000000F
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct PROPERTYKEY
	{
		public Guid fmtid;
		public int pid;

		public override string ToString()
		{
			return fmtid.ToString("B") + " " + pid;
		}
	}

	// NOTE: we only define what we handle
	[Flags]
	public enum VARTYPE : short
	{
		VT_I4 = 3,
		VT_BOOL = 11,
		VT_UI4 = 19,
		VT_LPWSTR = 31,
		VT_BLOB = 65,
		VT_CLSID = 72,
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct PROPVARIANT
	{
		public VARTYPE vt;
		public ushort wReserved1;
		public ushort wReserved2;
		public ushort wReserved3;
		public PROPVARIANTunion union;

		public object GetValue()
		{
			switch (vt)
			{
				case VARTYPE.VT_BOOL:
					return union.boolVal != 0;

				case VARTYPE.VT_LPWSTR:
					return Marshal.PtrToStringUni(union.pwszVal);

				case VARTYPE.VT_UI4:
					return union.lVal;

				case VARTYPE.VT_CLSID:
					return (Guid)Marshal.PtrToStructure(union.puuid, typeof(Guid));

				default:
					return vt.ToString() + ":?";
			}
		}
	}

	[StructLayout(LayoutKind.Explicit)]
	public struct PROPVARIANTunion
	{
		[FieldOffset(0)]
		public int lVal;
		[FieldOffset(0)]
		public ulong uhVal;
		[FieldOffset(0)]
		public short boolVal;
		[FieldOffset(0)]
		public IntPtr pwszVal;
		[FieldOffset(0)]
		public IntPtr puuid;
	}

	[ComImport]
	[Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
	public class MMDeviceEnumerator
	{
	}

	[Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IMMDeviceEnumerator
	{
		[PreserveSig]
		int EnumAudioEndpoints(EDataFlow dataFlow, DEVICE_STATE dwStateMask, out IMMDeviceCollection ppDevices);

		[PreserveSig]
		int GetDefaultAudioEndpoint(EDataFlow dataFlow, ERole role, out IMMDevice ppEndpoint);

		[PreserveSig]
		int GetDevice([MarshalAs(UnmanagedType.LPWStr)] string pwstrId, out IMMDevice ppDevice);

		[PreserveSig]
		int RegisterEndpointNotificationCallback(IMMNotificationClient pClient);

		[PreserveSig]
		int UnregisterEndpointNotificationCallback(IMMNotificationClient pClient);
	}

	[Guid("7991EEC9-7E89-4D85-8390-6C703CEC60C0"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IMMNotificationClient
	{
		void OnDeviceStateChanged([MarshalAs(UnmanagedType.LPWStr)] string pwstrDeviceId, DEVICE_STATE dwNewState);
		void OnDeviceAdded([MarshalAs(UnmanagedType.LPWStr)] string pwstrDeviceId);
		void OnDeviceRemoved([MarshalAs(UnmanagedType.LPWStr)] string deviceId);
		void OnDefaultDeviceChanged(EDataFlow flow, ERole role, string pwstrDefaultDeviceId);
		void OnPropertyValueChanged([MarshalAs(UnmanagedType.LPWStr)] string pwstrDeviceId, PROPERTYKEY key);
	}

	[Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IMMDeviceCollection
	{
		[PreserveSig]
		int GetCount(out int pcDevices);

		[PreserveSig]
		int Item(int nDevice, out IMMDevice ppDevice);
	}

	[Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IMMDevice
	{
		[PreserveSig]
		int Activate([MarshalAs(UnmanagedType.LPStruct)] Guid riid, CLSCTX dwClsCtx, IntPtr pActivationParams, [MarshalAs(UnmanagedType.IUnknown)] out object ppInterface);

		[PreserveSig]
		int OpenPropertyStore(STGM stgmAccess, out IPropertyStore ppProperties);

		[PreserveSig]
		int GetId([MarshalAs(UnmanagedType.LPWStr)] out string ppstrId);

		[PreserveSig]
		int GetState(out DEVICE_STATE pdwState);
	}

	[Guid("6f79d558-3e96-4549-a1d1-7d75d2288814"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IPropertyDescription
	{
		[PreserveSig]
		int GetPropertyKey(out PROPERTYKEY pkey);

		[PreserveSig]
		int GetCanonicalName(out IntPtr ppszName);

		[PreserveSig]
		int GetPropertyType(out short pvartype);

		[PreserveSig]
		int GetDisplayName(out IntPtr ppszName);

		// WARNING: the rest is undefined. you *can't* implement it, only use it.
	}

	[Guid("886d8eeb-8cf2-4446-8d02-cdba1dbdcf99"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IPropertyStore
	{
		[PreserveSig]
		int GetCount(out int cProps);

		[PreserveSig]
		int GetAt(int iProp, out PROPERTYKEY pkey);

		[PreserveSig]
		int GetValue(ref PROPERTYKEY key, ref PROPVARIANT pv);

		[PreserveSig]
		int SetValue(ref PROPERTYKEY key, ref PROPVARIANT propvar);

		[PreserveSig]
		int Commit();
	}

	[Guid("BFA971F1-4D5E-40BB-935E-967039BFBEE4"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IAudioSessionManager
	{
		[PreserveSig]
		int GetAudioSessionControl([MarshalAs(UnmanagedType.LPStruct)] Guid AudioSessionGuid, int StreamFlags, out IAudioSessionControl SessionControl);

		[PreserveSig]
		int GetSimpleAudioVolume([MarshalAs(UnmanagedType.LPStruct)] Guid AudioSessionGuid, int StreamFlags, ISimpleAudioVolume AudioVolume);
	}

	[Guid("77AA99A0-1BD6-484F-8BC7-2C654C9A9B6F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IAudioSessionManager2
	{
		[PreserveSig]
		int GetAudioSessionControl([MarshalAs(UnmanagedType.LPStruct)] Guid AudioSessionGuid, int StreamFlags, out IAudioSessionControl SessionControl);

		[PreserveSig]
		int GetSimpleAudioVolume([MarshalAs(UnmanagedType.LPStruct)] Guid AudioSessionGuid, int StreamFlags, ISimpleAudioVolume AudioVolume);

		[PreserveSig]
		int GetSessionEnumerator(out IAudioSessionEnumerator SessionEnum);

		[PreserveSig]
		int RegisterSessionNotification(IAudioSessionNotification SessionNotification);

		[PreserveSig]
		int UnregisterSessionNotification(IAudioSessionNotification SessionNotification);

		int RegisterDuckNotificationNotImpl();
		int UnregisterDuckNotificationNotImpl();
	}

	[Guid("641DD20B-4D41-49CC-ABA3-174B9477BB08"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IAudioSessionNotification
	{
		void OnSessionCreated(IAudioSessionControl NewSession);
	}

	[Guid("E2F5BB11-0570-40CA-ACDD-3AA01277DEE8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IAudioSessionEnumerator
	{
		[PreserveSig]
		int GetCount(out int SessionCount);

		[PreserveSig]
		int GetSession(int SessionCount, out IAudioSessionControl2 Session);
	}

	[Guid("bfb7ff88-7239-4fc9-8fa2-07c950be9c6d"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IAudioSessionControl2
	{
		// IAudioSessionControl
		[PreserveSig]
		int GetState(out AudioSessionState pRetVal);

		[PreserveSig]
		int GetDisplayName([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);

		[PreserveSig]
		int SetDisplayName([MarshalAs(UnmanagedType.LPWStr)]string Value, [MarshalAs(UnmanagedType.LPStruct)] Guid EventContext);

		[PreserveSig]
		int GetIconPath([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);

		[PreserveSig]
		int SetIconPath([MarshalAs(UnmanagedType.LPWStr)] string Value, [MarshalAs(UnmanagedType.LPStruct)] Guid EventContext);

		[PreserveSig]
		int GetGroupingParam(out Guid pRetVal);

		[PreserveSig]
		int SetGroupingParam([MarshalAs(UnmanagedType.LPStruct)] Guid Override, [MarshalAs(UnmanagedType.LPStruct)] Guid EventContext);

		[PreserveSig]
		int RegisterAudioSessionNotification(IAudioSessionEvents NewNotifications);

		[PreserveSig]
		int UnregisterAudioSessionNotification(IAudioSessionEvents NewNotifications);

		// IAudioSessionControl2
		[PreserveSig]
		int GetSessionIdentifier([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);

		[PreserveSig]
		int GetSessionInstanceIdentifier([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);

		[PreserveSig]
		int GetProcessId(out int pRetVal);

		[PreserveSig]
		int IsSystemSoundsSession();

		[PreserveSig]
		int SetDuckingPreference(bool optOut);
	}

	[Guid("F4B1A599-7266-4319-A8CA-E70ACB11E8CD"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IAudioSessionControl
	{
		[PreserveSig]
		int GetState(out AudioSessionState pRetVal);

		[PreserveSig]
		int GetDisplayName([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);

		[PreserveSig]
		int SetDisplayName([MarshalAs(UnmanagedType.LPWStr)]string Value, [MarshalAs(UnmanagedType.LPStruct)] Guid EventContext);

		[PreserveSig]
		int GetIconPath([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);

		[PreserveSig]
		int SetIconPath([MarshalAs(UnmanagedType.LPWStr)] string Value, [MarshalAs(UnmanagedType.LPStruct)] Guid EventContext);

		[PreserveSig]
		int GetGroupingParam(out Guid pRetVal);

		[PreserveSig]
		int SetGroupingParam([MarshalAs(UnmanagedType.LPStruct)] Guid Override, [MarshalAs(UnmanagedType.LPStruct)] Guid EventContext);

		[PreserveSig]
		int RegisterAudioSessionNotification(IAudioSessionEvents NewNotifications);

		[PreserveSig]
		int UnregisterAudioSessionNotification(IAudioSessionEvents NewNotifications);
	}

	[Guid("24918ACC-64B3-37C1-8CA9-74A66E9957A8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IAudioSessionEvents
	{
		void OnDisplayNameChanged([MarshalAs(UnmanagedType.LPWStr)] string NewDisplayName, [MarshalAs(UnmanagedType.LPStruct)] Guid EventContext);
		void OnIconPathChanged([MarshalAs(UnmanagedType.LPWStr)] string NewIconPath, [MarshalAs(UnmanagedType.LPStruct)] Guid EventContext);
		void OnSimpleVolumeChanged(float NewVolume, bool NewMute, [MarshalAs(UnmanagedType.LPStruct)] Guid EventContext);
		void OnChannelVolumeChanged(int ChannelCount, IntPtr NewChannelVolumeArray, int ChangedChannel, [MarshalAs(UnmanagedType.LPStruct)] Guid EventContext);
		void OnGroupingParamChanged([MarshalAs(UnmanagedType.LPStruct)] Guid NewGroupingParam, [MarshalAs(UnmanagedType.LPStruct)] Guid EventContext);
		void OnStateChanged(AudioSessionState NewState);
		void OnSessionDisconnected(AudioSessionDisconnectReason DisconnectReason);
	}

	[Guid("87CE5498-68D6-44E5-9215-6DA47EF883D8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface ISimpleAudioVolume
	{
		[PreserveSig]
		int SetMasterVolume(float fLevel, [MarshalAs(UnmanagedType.LPStruct)] ref Guid EventContext);

		[PreserveSig]
		int GetMasterVolume(out float pfLevel);

		[PreserveSig]
		int SetMute(bool bMute, [MarshalAs(UnmanagedType.LPStruct)] ref Guid EventContext);

		[PreserveSig]
		int GetMute(out bool pbMute);
	}

	public sealed class AudioSession : IDisposable
	{
		private IAudioSessionControl2 _ctl;
		private Process _process;

		public AudioSession(IAudioSessionControl2 ctl)
		{
			_ctl = ctl;
		}

		public Process Process
		{
			get
			{
				if (_process == null && ProcessId != 0)
				{
					try
					{
						_process = Process.GetProcessById(ProcessId);
					}
					catch
					{
						// do nothing
					}
				}
				return _process;
			}
		}

		public int ProcessId
		{
			get
			{
				CheckDisposed();
				int i;
				_ctl.GetProcessId(out i);
				return i;
			}
		}

		public string Identifier
		{
			get
			{
				CheckDisposed();
				string s;
				_ctl.GetSessionIdentifier(out s);
				return s;
			}
		}

		public string InstanceIdentifier
		{
			get
			{
				CheckDisposed();
				string s;
				_ctl.GetSessionInstanceIdentifier(out s);
				return s;
			}
		}

		public AudioSessionState State
		{
			get
			{
				CheckDisposed();
				AudioSessionState s;
				_ctl.GetState(out s);
				return s;
			}
		}

		public Guid GroupingParam
		{
			get
			{
				CheckDisposed();
				Guid g;
				_ctl.GetGroupingParam(out g);
				return g;
			}
			set
			{
				CheckDisposed();
				_ctl.SetGroupingParam(value, Guid.Empty);
			}
		}

		public string DisplayName
		{
			get
			{
				CheckDisposed();
				string s;
				_ctl.GetDisplayName(out s);
				return s;
			}
			set
			{
				CheckDisposed();
				string s;
				_ctl.GetDisplayName(out s);
				if (s != value)
				{
					_ctl.SetDisplayName(value, Guid.Empty);
				}
			}
		}

		public string IconPath
		{
			get
			{
				CheckDisposed();
				string s;
				_ctl.GetIconPath(out s);
				return s;
			}
			set
			{
				CheckDisposed();
				string s;
				_ctl.GetIconPath(out s);
				if (s != value)
				{
					_ctl.SetIconPath(value, Guid.Empty);
				}
			}
		}

		private void CheckDisposed()
		{
			if (_ctl == null)
				throw new ObjectDisposedException("Control");
		}

		public override string ToString()
		{
			string s = DisplayName;
			if (!string.IsNullOrEmpty(s))
				return "DisplayName: " + s;

			if (Process != null)
				return "Process: " + Process.ProcessName;

			return "Pid: " + ProcessId;
		}

		public void Dispose()
		{
			if (_ctl != null)
			{
				Marshal.ReleaseComObject(_ctl);
				_ctl = null;
			}
		}
	}

	public sealed class Win32AudioDevice
	{
		public Win32AudioDevice(string id, AudioDeviceState state, IDictionary<string, object> properties)
		{
			Id = id;
			State = state;
			Properties = properties;
		}

		public string Id { get; private set; }
		public AudioDeviceState State { get; private set; }
		public IDictionary<string, object> Properties { get; private set; }

		public string Description
		{
			get
			{
				const string PKEY_Device_DeviceDesc = "{a45c254e-df1c-4efd-8020-67d146a850e0} 2";
				object value;
				Properties.TryGetValue(PKEY_Device_DeviceDesc, out value);
				return $"{value}";
			}
		}

		public string ContainerId
		{
			get
			{
				const string PKEY_Devices_ContainerId = "{8c7ed206-3f8a-4827-b3ab-ae9e1faefc6c} 2";
				object value;
				Properties.TryGetValue(PKEY_Devices_ContainerId, out value);
				return $"{value}";
			}
		}

		public string EnumeratorName
		{
			get
			{
				const string PKEY_Device_EnumeratorName = "{a45c254e-df1c-4efd-8020-67d146a850e0} 24";
				object value;
				Properties.TryGetValue(PKEY_Device_EnumeratorName, out value);
				return $"{value}";
			}
		}

		public string InterfaceFriendlyName
		{
			get
			{
				const string DEVPKEY_DeviceInterface_FriendlyName = "{026e516e-b814-414b-83cd-856d6fef4822} 2";
				object value;
				Properties.TryGetValue(DEVPKEY_DeviceInterface_FriendlyName, out value);
				return $"{value}";
			}
		}

		public string FriendlyName
		{
			get
			{
				const string DEVPKEY_Device_FriendlyName = "{a45c254e-df1c-4efd-8020-67d146a850e0} 14";
				object value;
				Properties.TryGetValue(DEVPKEY_Device_FriendlyName, out value);
				return $"{value}";
			}
		}

		public override string ToString()
		{
			return FriendlyName;
		}
	}

	public enum AudioSessionState
	{
		Inactive = 0,
		Active = 1,
		Expired = 2
	}

	public enum AudioDeviceState
	{
		Active = 0x1,
		Disabled = 0x2,
		NotPresent = 0x4,
		Unplugged = 0x8,
	}

	public enum AudioSessionDisconnectReason
	{
		DisconnectReasonDeviceRemoval = 0,
		DisconnectReasonServerShutdown = 1,
		DisconnectReasonFormatChanged = 2,
		DisconnectReasonSessionLogoff = 3,
		DisconnectReasonSessionDisconnected = 4,
		DisconnectReasonExclusiveModeOverride = 5
	}

	[Guid("657804FA-D6AD-4496-8A60-352752AF4F89")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IAudioEndpointVolumeCallback
	{
		/// <summary>
		/// Notifies the client that the volume level or muting state of the audio endpoint device has changed.
		/// </summary>
		/// <param name="notificationData">The volume-notification data.</param>
		/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
		[PreserveSig]
		int OnNotify(
			[In] IntPtr notificationData);
	}

	[Guid("5CDF2C82-841E-4546-9722-0CF74078229A"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IAudioEndpointVolume
	{
		// Note: Any changes to this interface should be repeated in IAudioEndpointVolumeEx.

		/// <summary>
		/// Registers a client's notification callback interface.
		/// </summary>
		/// <param name="client">The <see cref="IAudioEndpointVolumeCallback"/> interface that is registering for notification callbacks.</param>
		/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
		[PreserveSig]
		int RegisterControlChangeNotify(
			[In] [MarshalAs(UnmanagedType.Interface)] IAudioEndpointVolumeCallback client);

		/// <summary>
		/// Deletes the registration of a client's notification callback interface.
		/// </summary>
		/// <param name="client">The <see cref="IAudioEndpointVolumeCallback"/> interface that previously registered for notification callbacks.</param>
		/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
		[PreserveSig]
		int UnregisterControlChangeNotify(
						[In] [MarshalAs(UnmanagedType.Interface)] IAudioEndpointVolumeCallback client);

		/// <summary>
		/// Gets a count of the channels in the audio stream.
		/// </summary>
		/// <param name="channelCount">The number of channels.</param>
		/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
		[PreserveSig]
		int GetChannelCount(
				[Out] [MarshalAs(UnmanagedType.U4)] out UInt32 channelCount);

		/// <summary>
		/// Sets the master volume level of the audio stream, in decibels.
		/// </summary>
		/// <param name="level">The new master volume level in decibels.</param>
		/// <param name="eventContext">A user context value that is passed to the notification callback.</param>
		/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
		[PreserveSig]
		int SetMasterVolumeLevel(
			[In] [MarshalAs(UnmanagedType.R4)] float level,
						[In] [MarshalAs(UnmanagedType.LPStruct)] Guid eventContext);

		/// <summary>
		/// Sets the master volume level, expressed as a normalized, audio-tapered value.
		/// </summary>
		/// <param name="level">The new master volume level expressed as a normalized value between 0.0 and 1.0.</param>
		/// <param name="eventContext">A user context value that is passed to the notification callback.</param>
		/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
		[PreserveSig]
		int SetMasterVolumeLevelScalar(
			[In] [MarshalAs(UnmanagedType.R4)] float level,
						[In] [MarshalAs(UnmanagedType.LPStruct)] Guid eventContext);

		/// <summary>
		/// Gets the master volume level of the audio stream, in decibels.
		/// </summary>
		/// <param name="level">The volume level in decibels.</param>
		/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
		[PreserveSig]
		int GetMasterVolumeLevel(
			[Out] [MarshalAs(UnmanagedType.R4)] out float level);

		/// <summary>
		/// Gets the master volume level, expressed as a normalized, audio-tapered value.
		/// </summary>
		/// <param name="level">The volume level expressed as a normalized value between 0.0 and 1.0.</param>
		/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
		[PreserveSig]
		int GetMasterVolumeLevelScalar(
			[Out] [MarshalAs(UnmanagedType.R4)] out float level);

		/// <summary>
		/// Sets the volume level, in decibels, of the specified channel of the audio stream.
		/// </summary>
		/// <param name="channelNumber">The channel number.</param>
		/// <param name="level">The new volume level in decibels.</param>
		/// <param name="eventContext">A user context value that is passed to the notification callback.</param>
		/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
		[PreserveSig]
		int SetChannelVolumeLevel(
			[In] [MarshalAs(UnmanagedType.U4)] UInt32 channelNumber,
			[In] [MarshalAs(UnmanagedType.R4)] float level,
						[In] [MarshalAs(UnmanagedType.LPStruct)] Guid eventContext);

		/// <summary>
		/// Sets the normalized, audio-tapered volume level of the specified channel in the audio stream.
		/// </summary>
		/// <param name="channelNumber">The channel number.</param>
		/// <param name="level">The new master volume level expressed as a normalized value between 0.0 and 1.0.</param>
		/// <param name="eventContext">A user context value that is passed to the notification callback.</param>
		/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
		[PreserveSig]
		int SetChannelVolumeLevelScalar(
						[In] [MarshalAs(UnmanagedType.U4)] UInt32 channelNumber,
			[In] [MarshalAs(UnmanagedType.R4)] float level,
						[In] [MarshalAs(UnmanagedType.LPStruct)] Guid eventContext);

		/// <summary>
		/// Gets the volume level, in decibels, of the specified channel in the audio stream.
		/// </summary>
		/// <param name="channelNumber">The zero-based channel number.</param>
		/// <param name="level">The volume level in decibels.</param>
		/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
		[PreserveSig]
		int GetChannelVolumeLevel(
				[In] [MarshalAs(UnmanagedType.U4)] UInt32 channelNumber,
	[Out] [MarshalAs(UnmanagedType.R4)] out float level);

		/// <summary>
		/// Gets the normalized, audio-tapered volume level of the specified channel of the audio stream.
		/// </summary>
		/// <param name="channelNumber">The zero-based channel number.</param>
		/// <param name="level">The volume level expressed as a normalized value between 0.0 and 1.0.</param>
		/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
		[PreserveSig]
		int GetChannelVolumeLevelScalar(
				[In] [MarshalAs(UnmanagedType.U4)] UInt32 channelNumber,
	[Out] [MarshalAs(UnmanagedType.R4)] out float level);

		/// <summary>
		/// Sets the muting state of the audio stream.
		/// </summary>
		/// <param name="isMuted">True to mute the stream, or false to unmute the stream.</param>
		/// <param name="eventContext">A user context value that is passed to the notification callback.</param>
		/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
		[PreserveSig]
		int SetMute(
			[In] [MarshalAs(UnmanagedType.Bool)] Boolean isMuted,
						[In] [MarshalAs(UnmanagedType.LPStruct)] Guid eventContext);

		/// <summary>
		/// Gets the muting state of the audio stream.
		/// </summary>
		/// <param name="isMuted">The muting state. True if the stream is muted, false otherwise.</param>
		/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
		[PreserveSig]
		int GetMute(
	[Out] [MarshalAs(UnmanagedType.Bool)] out Boolean isMuted);

		/// <summary>
		/// Gets information about the current step in the volume range.
		/// </summary>
		/// <param name="step">The current zero-based step index.</param>
		/// <param name="stepCount">The total number of steps in the volume range.</param>
		/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
		[PreserveSig]
		int GetVolumeStepInfo(
						[Out] [MarshalAs(UnmanagedType.U4)] out UInt32 step,
						[Out] [MarshalAs(UnmanagedType.U4)] out UInt32 stepCount);

		/// <summary>
		/// Increases the volume level by one step.
		/// </summary>
		/// <param name="eventContext">A user context value that is passed to the notification callback.</param>
		/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
		[PreserveSig]
		int VolumeStepUp(
						[In] [MarshalAs(UnmanagedType.LPStruct)] Guid eventContext);

		/// <summary>
		/// Decreases the volume level by one step.
		/// </summary>
		/// <param name="eventContext">A user context value that is passed to the notification callback.</param>
		/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
		[PreserveSig]
		int VolumeStepDown(
						[In] [MarshalAs(UnmanagedType.LPStruct)] Guid eventContext);

		/// <summary>
		/// Queries the audio endpoint device for its hardware-supported functions.
		/// </summary>
		/// <param name="hardwareSupportMask">A hardware support mask that indicates the capabilities of the endpoint.</param>
		/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
		[PreserveSig]
		int QueryHardwareSupport(
			[Out] [MarshalAs(UnmanagedType.U4)] out UInt32 hardwareSupportMask);

		/// <summary>
		/// Gets the volume range of the audio stream, in decibels.
		/// </summary>
		/// <param name="volumeMin">The minimum volume level in decibels.</param>
		/// <param name="volumeMax">The maximum volume level in decibels.</param>
		/// <param name="volumeStep">The volume increment level in decibels.</param>
		/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
		[PreserveSig]
		int GetVolumeRange(
				[Out] [MarshalAs(UnmanagedType.R4)] out float volumeMin,
	[Out] [MarshalAs(UnmanagedType.R4)] out float volumeMax,
	[Out] [MarshalAs(UnmanagedType.R4)] out float volumeStep);
	}

	[Guid("66E11784-F695-4F28-A505-A7080081A78F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IAudioEndpointVolumeEx
	{
		// Note: We can't derive from IAudioEndpointVolume, as that will produce the wrong vtable.

		#region IAudioEndpointVolume Methods

		/// <summary>
		/// Registers a client's notification callback interface.
		/// </summary>
		/// <param name="client">The <see cref="IAudioEndpointVolumeCallback"/> interface that is registering for notification callbacks.</param>
		/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
		[PreserveSig]
		int RegisterControlChangeNotify(
				[In] IAudioEndpointVolumeCallback client);

		/// <summary>
		/// Deletes the registration of a client's notification callback interface.
		/// </summary>
		/// <param name="clientCallback">The <see cref="IAudioEndpointVolumeCallback"/> interface that previously registered for notification callbacks.</param>
		/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
		[PreserveSig]
		int UnregisterControlChangeNotify(
				[In] IAudioEndpointVolumeCallback clientCallback);

		/// <summary>
		/// Gets a count of the channels in the audio stream.
		/// </summary>
		/// <param name="channelCount">The number of channels.</param>
		/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
		[PreserveSig]
		int GetChannelCount(
				[Out] [MarshalAs(UnmanagedType.U4)] out UInt32 channelCount);

		/// <summary>
		/// Sets the master volume level of the audio stream, in decibels.
		/// </summary>
		/// <param name="level">The new master volume level in decibels.</param>
		/// <param name="eventContext">A user context value that is passed to the notification callback.</param>
		/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
		[PreserveSig]
		int SetMasterVolumeLevel(
				[In] [MarshalAs(UnmanagedType.R4)] float level,
				[In] ref Guid eventContext);

		/// <summary>
		/// Sets the master volume level, expressed as a normalized, audio-tapered value.
		/// </summary>
		/// <param name="level">The new master volume level expressed as a normalized value between 0.0 and 1.0.</param>
		/// <param name="eventContext">A user context value that is passed to the notification callback.</param>
		/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
		[PreserveSig]
		int SetMasterVolumeLevelScalar(
				[In] [MarshalAs(UnmanagedType.R4)] float level,
				[In] ref Guid eventContext);

		/// <summary>
		/// Gets the master volume level of the audio stream, in decibels.
		/// </summary>
		/// <param name="level">The volume level in decibels.</param>
		/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
		[PreserveSig]
		int GetMasterVolumeLevel(
				[Out] [MarshalAs(UnmanagedType.R4)] out float level);

		/// <summary>
		/// Gets the master volume level, expressed as a normalized, audio-tapered value.
		/// </summary>
		/// <param name="level">The volume level expressed as a normalized value between 0.0 and 1.0.</param>
		/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
		[PreserveSig]
		int GetMasterVolumeLevelScalar(
				[Out] [MarshalAs(UnmanagedType.R4)] out float level);

		/// <summary>
		/// Sets the volume level, in decibels, of the specified channel of the audio stream.
		/// </summary>
		/// <param name="channelNumber">The channel number.</param>
		/// <param name="level">The new volume level in decibels.</param>
		/// <param name="eventContext">A user context value that is passed to the notification callback.</param>
		/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
		[PreserveSig]
		int SetChannelVolumeLevel(
				[In] [MarshalAs(UnmanagedType.SysUInt)] UIntPtr channelNumber,
				[In] [MarshalAs(UnmanagedType.R4)] float level,
				[In] ref Guid eventContext);

		/// <summary>
		/// Sets the normalized, audio-tapered volume level of the specified channel in the audio stream.
		/// </summary>
		/// <param name="channelNumber">The channel number.</param>
		/// <param name="level">The new master volume level expressed as a normalized value between 0.0 and 1.0.</param>
		/// <param name="eventContext">A user context value that is passed to the notification callback.</param>
		/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
		[PreserveSig]
		int SetChannelVolumeLevelScalar(
				[In] [MarshalAs(UnmanagedType.SysUInt)] UIntPtr channelNumber,
				[In] [MarshalAs(UnmanagedType.R4)] float level,
				[In] ref Guid eventContext);

		/// <summary>
		/// Gets the volume level, in decibels, of the specified channel in the audio stream.
		/// </summary>
		/// <param name="channelNumber">The zero-based channel number.</param>
		/// <param name="level">The volume level in decibels.</param>
		/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
		[PreserveSig]
		int GetChannelVolumeLevel(
				[In] [MarshalAs(UnmanagedType.SysUInt)] UIntPtr channelNumber,
				[Out] [MarshalAs(UnmanagedType.R4)] out float level);

		/// <summary>
		/// Gets the normalized, audio-tapered volume level of the specified channel of the audio stream.
		/// </summary>
		/// <param name="channelNumber">The zero-based channel number.</param>
		/// <param name="level">The volume level expressed as a normalized value between 0.0 and 1.0.</param>
		/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
		[PreserveSig]
		int GetChannelVolumeLevelScalar(
				[In] [MarshalAs(UnmanagedType.SysUInt)] UIntPtr channelNumber,
				[Out] [MarshalAs(UnmanagedType.R4)] out float level);

		/// <summary>
		/// Sets the muting state of the audio stream.
		/// </summary>
		/// <param name="isMuted">True to mute the stream, or false to unmute the stream.</param>
		/// <param name="eventContext">A user context value that is passed to the notification callback.</param>
		/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
		[PreserveSig]
		int SetMute(
				[In] [MarshalAs(UnmanagedType.Bool)] Boolean isMuted,
				[In] ref Guid eventContext);

		/// <summary>
		/// Gets the muting state of the audio stream.
		/// </summary>
		/// <param name="isMuted">The muting state. True if the stream is muted, false otherwise.</param>
		/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
		[PreserveSig]
		int GetMute(
				[Out] [MarshalAs(UnmanagedType.Bool)] out Boolean isMuted);

		/// <summary>
		/// Gets information about the current step in the volume range.
		/// </summary>
		/// <param name="step">The current zero-based step index.</param>
		/// <param name="stepCount">The total number of steps in the volume range.</param>
		/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
		[PreserveSig]
		int GetVolumeStepInfo(
				[Out] [MarshalAs(UnmanagedType.SysUInt)] out UIntPtr step,
				[Out] [MarshalAs(UnmanagedType.SysUInt)] out UIntPtr stepCount);

		/// <summary>
		/// Increases the volume level by one step.
		/// </summary>
		/// <param name="eventContext">A user context value that is passed to the notification callback.</param>
		/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
		[PreserveSig]
		int VolumeStepUp(
				[In] ref Guid eventContext);

		/// <summary>
		/// Decreases the volume level by one step.
		/// </summary>
		/// <param name="eventContext">A user context value that is passed to the notification callback.</param>
		/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
		[PreserveSig]
		int VolumeStepDown(
				[In] ref Guid eventContext);

		/// <summary>
		/// Queries the audio endpoint device for its hardware-supported functions.
		/// </summary>
		/// <param name="hardwareSupportMask">A hardware support mask that indicates the capabilities of the endpoint.</param>
		/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
		[PreserveSig]
		int QueryHardwareSupport(
				[Out] [MarshalAs(UnmanagedType.U4)] out UInt32 hardwareSupportMask);

		/// <summary>
		/// Gets the volume range of the audio stream, in decibels.
		/// </summary>
		/// <param name="volumeMin">The minimum volume level in decibels.</param>
		/// <param name="volumeMax">The maximum volume level in decibels.</param>
		/// <param name="volumeStep">The volume increment level in decibels.</param>
		/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
		[PreserveSig]
		int GetVolumeRange(
				[Out] [MarshalAs(UnmanagedType.R4)] out float volumeMin,
				[Out] [MarshalAs(UnmanagedType.R4)] out float volumeMax,
				[Out] [MarshalAs(UnmanagedType.R4)] out float volumeStep);

		#endregion

		/// <summary>
		/// Gets the volume range for a specified channel.
		/// </summary>
		/// <param name="channelNumber">The channel number for which to get the volume range.</param>
		/// <param name="volumeMin">The minimum volume level for the channel, in decibels.</param>
		/// <param name="volumeMax">The maximum volume level for the channel, in decibels.</param>
		/// <param name="volumeStep">The volume increment for the channel, in decibels.</param>
		/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
		[PreserveSig]
		int GetVolumeRangeChannel(
				[In] [MarshalAs(UnmanagedType.U4)] UInt32 channelNumber,
				[Out] [MarshalAs(UnmanagedType.R4)] out float volumeMin,
	[Out] [MarshalAs(UnmanagedType.R4)] out float volumeMax,
	[Out] [MarshalAs(UnmanagedType.R4)] out float volumeStep);
	}

	[Guid("C02216F6-8C67-4B5B-9D00-D008E73E0064"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IAudioMeterInformation
	{
		/// <summary>
		/// Gets the peak sample value for the channels in the audio stream.
		/// </summary>
		/// <param name="peak">The peak sample value for the audio stream.</param>
		/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
		[PreserveSig]
		int GetPeakValue(
			[Out] [MarshalAs(UnmanagedType.R4)] out float peak);

		/// <summary>
		/// Gets the number of channels in the audio stream that are monitored by peak meters.
		/// </summary>
		/// <param name="channelCount">The channel count.</param>
		/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
		[PreserveSig]
		int GetMeteringChannelCount(
			[Out] [MarshalAs(UnmanagedType.U4)] out UInt32 channelCount);

		/// <summary>
		/// Gets the peak sample values for all the channels in the audio stream.
		/// </summary>
		/// <param name="channelCount">The channel count.</param>
		/// <param name="peakValues">An array of peak sample values.</param>
		/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
		[PreserveSig]
		int GetChannelsPeakValues(
				[In] [MarshalAs(UnmanagedType.U4)] UInt32 channelCount,
				[In, Out] [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.R4)] float[] peakValues);

		/// <summary>
		/// Queries the audio endpoint device for its hardware-supported functions.
		/// </summary>
		/// <param name="hardwareSupportMask">A hardware support mask that indicates the capabilities of the endpoint.</param>
		/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
		[PreserveSig]
		int QueryHardwareSupport(
	[Out] [MarshalAs(UnmanagedType.U4)] out UInt32 hardwareSupportMask);
	}
}