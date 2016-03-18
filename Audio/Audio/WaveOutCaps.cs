using System.Runtime.InteropServices;

namespace Audio
{
	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct WaveOutCaps
	{
		public short wMid;
		public short wPid;
		public int vDriverVersion;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
		public char[] szPname;
		public uint dwFormats;
		public short wChannels;
		public short wReserved1;
		public short dwSupport;
	}
}