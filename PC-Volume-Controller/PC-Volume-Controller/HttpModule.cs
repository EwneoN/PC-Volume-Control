using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Nancy;
using Nancy.ModelBinding;
using Audio;
using Newtonsoft.Json;

namespace PC_Volume_Controller
{
  public class HttpModule : NancyModule
  {
    #region Constructors

    public HttpModule()
    {
      Get["/"] = o => Index();
      Get["/Dummy"] = o => DummyIndex();

      Get["/DummyVolume"] = o => GetDummyVolume();
      Post["/DummyVolume"] = o => SetDummyVolume();
      Get["/Volume"] = o => GetVolume();
      Post["/Volume"] = o => SetVolume();

      Get["/DummyPlaybackDevices"] = o => GetDummyPlaybackDevices();
      Get["/PlaybackDevices"] = o => GetPlaybackDevices();

      Get["/DummyDefaultPlaybackDevice"] = o => GetDummyDefaultPlaybackDevice();
      Post["/DummyDefaultPlaybackDevice"] = o => SetDummyDefaultPlaybackDevice();
      Get["/DefaultPlaybackDevice"] = o => GetDefaultPlaybackDevice();
      Post["/DefaultPlaybackDevice"] = o => SetDefaultPlaybackDevice();
    }

    #endregion

    #region Public Methods

    public string DummyIndex()
    {
      string file = File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Content\Index.html"));

      Regex volumeRegex = new Regex(@"(?<=\$scope\.value\s=\s)\d+(?=;//replace\sthis\svalue)");

      file = volumeRegex.Replace(file, "75");

      Regex devicesRegex = new Regex(@"(?<=\$scope\.devices\s=\s)\[\](?=;//replace\sthis\svalue)");

      var devices = new AudioDeviceList
      {
        new AudioDevice
        {
          Id = "1",
          IsCurrentDevice = true,
          Name = "Speaker"
        },
        new AudioDevice
        {
          Id = "2",
          IsCurrentDevice = false,
          Name = "Headphones"
        },
        new AudioDevice
        {
          Id = "3",
          IsCurrentDevice = false,
          Name = "Monitor"
        }
      };

      file = devicesRegex.Replace(file, $"JSON.parse('{{\"devices\":{JsonConvert.SerializeObject(devices)}}}').devices");

      return file;
    }

    public string Index()
    {
      float volume = GetCurrentVolume();

      string html = File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Content\Index.html"));

      Regex volumeRegex = new Regex(@"(?<=\$scope\.value\s=\s)\d+(?=;//replace\sthis\svalue)");

      html = volumeRegex.Replace(html, volume.ToString());

      Regex devicesRegex = new Regex(@"(?<=\$scope\.devices\s=\s)\[\](?=;//replace\sthis\svalue)");

      string json = JsonConvert.SerializeObject(GetAudioDeviceList());

      html = devicesRegex.Replace(html, $"JSON.parse('{{\"devices\":{json}}}').devices");

      return html;
    }

    public VolumeData GetVolume()
		{
			VolumeData info = new VolumeData
			{
			  Volume = GetCurrentVolume()
			};

			return info;
		}

    public VolumeData GetDummyVolume()
    {
      return new VolumeData
      {
        Volume = 75
      };
    }

    public HttpStatusCode SetDummyVolume()
    {
      VolumeData data = this.Bind<VolumeData>();

      return data != null ? HttpStatusCode.OK : HttpStatusCode.BadRequest;
    }

    public HttpStatusCode SetVolume()
		{
			VolumeData data = this.Bind<VolumeData>();

      bool succes;

      try
      {
        SetVolume(data.Volume);
        succes = true;
      }
      catch (Exception)
      {
        succes = false;
      }

			return succes ? HttpStatusCode.OK : HttpStatusCode.InternalServerError;
		}

		public AudioDeviceList GetPlaybackDevices()
		{
			return GetAudioDeviceList();
		}

    public AudioDeviceList GetDummyPlaybackDevices()
    {
      return new AudioDeviceList
      {
        new AudioDevice
        {
          Id = "1",
          IsCurrentDevice = true,
          Name = "Speaker"
        },
        new AudioDevice
        {
          Id = "2",
          IsCurrentDevice = false,
          Name = "Headphones"
        },
        new AudioDevice
        {
          Id = "3",
          IsCurrentDevice = false,
          Name = "Monitor"
        }
      };
    }

    public AudioDevice GetDefaultPlaybackDevice()
		{
			return GetCurrentDevice();
		}

    public HttpStatusCode SetDefaultPlaybackDevice()
		{
      AudioDevice device = this.Bind<AudioDevice>();

      bool succes;

		  try
		  {
        SetCurrentDevice(device);
		    succes = true;
		  }
      catch (Exception)
      {
        succes = false;
      }

      return succes ? HttpStatusCode.OK : HttpStatusCode.InternalServerError;
    }

    public AudioDevice GetDummyDefaultPlaybackDevice()
    {
      return new AudioDevice
      {
        Id = "1",
        IsCurrentDevice = true,
        Name = "Speaker"
      };
    }

    public HttpStatusCode SetDummyDefaultPlaybackDevice()
    {
      AudioDevice data = this.Bind<AudioDevice>();

      return data != null ? HttpStatusCode.OK : HttpStatusCode.BadRequest;
    }

    #endregion

    #region Private Methods

    private float GetCurrentVolume()
    {
      IMMDevice speakers = AudioUtilities.GetSpeakers();

      IAudioEndpointVolume currentEndpointVolume = AudioUtilities.GetAudioEndpointVolume(speakers);

      float volume;

      currentEndpointVolume.GetMasterVolumeLevelScalar(out volume);

      return volume * 100;
    }

    private void SetVolume(float volume)
    {
      IMMDevice speakers = AudioUtilities.GetSpeakers();

      IAudioEndpointVolume currentEndpointVolume = AudioUtilities.GetAudioEndpointVolume(speakers);

      currentEndpointVolume.SetMasterVolumeLevelScalar(volume > 0 ? volume / 100 : 0, Guid.NewGuid());
    }

    private AudioDeviceList GetAudioDeviceList()
    {
      string id;
      IMMDevice speakers = AudioUtilities.GetSpeakers();
      speakers.GetId(out id);

      return new AudioDeviceList(AudioUtilities.GetAllActiveSpeakers().Select(d => new AudioDevice(d, d.Id == id)));
    }

    private AudioDevice GetCurrentDevice()
    {
      return new AudioDevice(AudioUtilities.CreateDevice(AudioUtilities.GetSpeakers()));
    }

    private void SetCurrentDevice(AudioDevice device)
    {
      Playback.SetDefaultPlaybackDevice(device.Id);
    }

    #endregion
  }
}
