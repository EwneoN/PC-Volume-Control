using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Nancy;
using Nancy.ModelBinding;
using Audio;
using Nancy.Responses.Negotiation;

namespace PC_Volume_Controller
{
  /// <summary>
  /// A HttpModule class for controlling the current volume and default audio device on the host machine via a web browser or
  /// another application.
  /// </summary>
  /// <seealso cref="NancyModule" />
  public class HttpModule : NancyModule
  {
    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpModule"/> class.
    /// </summary>
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

    /// <summary>
    /// Fetches the contents of Content\Index.html and formats it using dummy values when requests to DummyIndex.
    /// This allows testing of the module without affecting volume or any audio devices.
    /// </summary>
    /// <returns>Content\Index.html with dummy values.</returns>
    public Negotiator DummyIndex()
    {
      AudioDeviceList devices = new AudioDeviceList
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

      string file = File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Content\ng-knob-options.json"));
      dynamic model = new IndexModel
      {
        Volume = "75",
        Devices = $"[{string.Join(",", devices.Select(MakeJavaScriptObjectString))}]",
        KnobOptions = $"{FormatJson(file)}"
      };
       
      return View["Index", model];
    }

    /// <summary>
    /// Fetches the contents of Content\Index.html and formats it using the current volume and available audio devices 
    /// found on the host machine using classes and methods defined in the referenced Audio library.
    /// </summary>
    /// <returns>Content\Index.html with live values.</returns>
    public Negotiator Index()
    {
      float volume = GetCurrentVolume();
      AudioDeviceList devices = GetAudioDeviceList();
      string file = File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Content\ng-knob-options.json"));
      dynamic model = new IndexModel
      {
        Volume = volume.ToString(),
        Devices = $"[{string.Join(",", devices.Select(MakeJavaScriptObjectString))}]",
        KnobOptions = $"{FormatJson(file)}"
      };

      return View["Index", model];
    }

    /// <summary>
    /// Gets the volume of the default audio device and returns it as either xml or json depending on the request.
    /// </summary>
    /// <returns></returns>
    public VolumeData GetVolume()
    {
      VolumeData info = new VolumeData
      {
        Volume = GetCurrentVolume()
      };

      return info;
    }

    /// <summary>
    /// Gets the dummy volume.
    /// </summary>
    /// <returns></returns>
    public VolumeData GetDummyVolume()
    {
      return new VolumeData
      {
        Volume = 75
      };
    }

    /// <summary>
    /// Sets the dummy volume.
    /// </summary>
    /// <returns></returns>
    public HttpStatusCode SetDummyVolume()
    {
      VolumeData data = this.Bind<VolumeData>();

      return data != null ? HttpStatusCode.OK : HttpStatusCode.BadRequest;
    }

    /// <summary>
    /// Sets the volume.
    /// </summary>
    /// <returns></returns>
    public HttpStatusCode SetVolume()
    {
      bool succes;

      try
      {
        VolumeData data = this.Bind<VolumeData>();

        if (data == null)
        {
          return HttpStatusCode.BadRequest;
        }

        SetVolume(data.Volume);
        succes = true;
      }
      catch (Exception)
      {
        succes = false;
      }

      return succes ? HttpStatusCode.OK : HttpStatusCode.InternalServerError;
    }

    /// <summary>
    /// Gets the playback devices.
    /// </summary>
    /// <returns></returns>
    public AudioDeviceList GetPlaybackDevices()
    {
      return GetAudioDeviceList();
    }

    /// <summary>
    /// Gets the dummy playback devices.
    /// </summary>
    /// <returns></returns>
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

    /// <summary>
    /// Gets the default playback device.
    /// </summary>
    /// <returns></returns>
    public AudioDevice GetDefaultPlaybackDevice()
    {
      return GetCurrentDevice();
    }

    /// <summary>
    /// Sets the default playback device.
    /// </summary>
    /// <returns></returns>
    public HttpStatusCode SetDefaultPlaybackDevice()
    {
      bool succes;

      try
      {
        AudioDevice device = this.Bind<AudioDevice>();

        if (device == null)
        {
          return HttpStatusCode.BadRequest;
        }

        SetCurrentDevice(device);
        succes = true;
      }
      catch (Exception)
      {
        succes = false;
      }

      return succes ? HttpStatusCode.OK : HttpStatusCode.InternalServerError;
    }

    /// <summary>
    /// Gets the dummy default playback device.
    /// </summary>
    /// <returns></returns>
    public AudioDevice GetDummyDefaultPlaybackDevice()
    {
      return new AudioDevice
      {
        Id = "1",
        IsCurrentDevice = true,
        Name = "Speaker"
      };
    }

    /// <summary>
    /// Sets the dummy default playback device.
    /// </summary>
    /// <returns></returns>
    public HttpStatusCode SetDummyDefaultPlaybackDevice()
    {
      AudioDevice data = this.Bind<AudioDevice>();

      return data != null ? HttpStatusCode.OK : HttpStatusCode.BadRequest;
    }

    #endregion

    #region Private Methods

    private float GetCurrentVolume()
    {
      IMMDevice speakers = AudioUtilities.GetCurrentSpeakers();
      IAudioEndpointVolume currentEndpointVolume = AudioUtilities.GetAudioEndpointVolume(speakers);

      float volume;

      currentEndpointVolume.GetMasterVolumeLevelScalar(out volume);

      //volume comes out as value between 0-1 but we want the value as 0-100
      return volume * 100;
    }

    private void SetVolume(float volume)
    {
      IMMDevice speakers = AudioUtilities.GetCurrentSpeakers();

      IAudioEndpointVolume currentEndpointVolume = AudioUtilities.GetAudioEndpointVolume(speakers);

      //as we work with volume as a value between 0-100 we need to convert back to a value between 0-1.
      //MasterVolumeLevelScalar capped at 100, any value above 100 is treated as 100.
      currentEndpointVolume.SetMasterVolumeLevelScalar(volume > 0 ? volume / 100 : 0, Guid.NewGuid());
    }

    private AudioDeviceList GetAudioDeviceList()
    {
      string id;
      IMMDevice speakers = AudioUtilities.GetCurrentSpeakers();
      speakers.GetId(out id);

      //get active playback devices, make sure we mark which one is the current default playback device
      return new AudioDeviceList(AudioUtilities.GetAllActiveSpeakers().Select(d => new AudioDevice(d, d.Id == id)));
    }

    private AudioDevice GetCurrentDevice()
    {
      return new AudioDevice(AudioUtilities.CreateDevice(AudioUtilities.GetCurrentSpeakers()));
    }

    private void SetCurrentDevice(AudioDevice device)
    {
      Playback.SetDefaultPlaybackDevice(device.Id);
    }

    private string MakeJavaScriptObjectString(AudioDevice device)
    {
      return "{" + 
             $"Id:\"{device.Id}\",Name:\"{device.Name}\"," +
             $"IsCurrentDevice:{device.IsCurrentDevice.ToString().ToLower()}" + 
             "}";
    }

    private string FormatJson(string json)
    {
      Regex regex1 = new Regex("(?i)\"(?=(\\s*:+))|(?<=(\\{|,)\\s*)\"(?=[a-zA-Z_][a-zA-Z0-9_])(?-i)");
      Regex regex2 = new Regex("(?<=(\\{|,|:))\\s+");

      string newJson = regex1.Replace(json, string.Empty).Replace("\r", "").Replace("\n", "").Replace("\t", "");

      return regex2.Replace(newJson, "");
    }

    #endregion

    #region Nested Types

    public class IndexModel
    {
      public string Volume { get; set; }

      public string Devices { get; set; }

      public string KnobOptions { get; set; }
    }

    #endregion
  }
}
