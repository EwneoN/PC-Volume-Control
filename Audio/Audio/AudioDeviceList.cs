using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;

namespace Audio
{
	public class AudioDeviceList : List<AudioDevice>
	{
	  public AudioDeviceList() { }

	  public AudioDeviceList(IEnumerable<AudioDevice> devices)
		{
			AddRange(devices);
		}
	}
}
