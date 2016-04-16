using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Audio.Test
{
  /// <summary>
  /// This class is for testing the methods used to get and set the current volume, 
  /// get and set the current default playback device as well as getting all the available playback devices.
  /// </summary>
  [TestClass]
  public class AudioTests
  {
    [TestMethod]
    public void TestGetAllActiveSpeakers()
    {
      var devices = AudioUtilities.GetAllActiveSpeakers();

      Assert.IsNotNull(devices);
      Assert.IsTrue(devices.Count > 0);
    }

    [TestMethod]
    public void TestGetCurrentSpeakers()
    {
      var speakers = AudioUtilities.GetCurrentSpeakers();

      Assert.IsNotNull(speakers);

      string id;

      Assert.IsTrue(speakers.GetId(out id) == 0);
      Assert.IsFalse(string.IsNullOrWhiteSpace(id));
    }

    [TestMethod]
    public void TestCreateDevice()
    {
      var speakers = AudioUtilities.GetCurrentSpeakers();

      Assert.IsNotNull(speakers);

      string id;

      Assert.IsTrue(speakers.GetId(out id) == 0);
      Assert.IsFalse(string.IsNullOrWhiteSpace(id));

      var device = AudioUtilities.CreateDevice(speakers);

      Assert.IsNotNull(device);
    }

    [TestMethod]
    public void TestGetVolume()
    {
      var devices = AudioUtilities.GetAllActiveSpeakers();
      var speakers = AudioUtilities.GetCurrentSpeakers();

      Assert.IsNotNull(speakers);

      string id;

      Assert.IsTrue(speakers.GetId(out id) == 0);
      Assert.IsFalse(string.IsNullOrWhiteSpace(id));

      IAudioEndpointVolume endpointVolume =
        AudioUtilities.GetAudioEndpointVolume(speakers);

      var device = AudioUtilities.CreateDevice(speakers);

      Assert.IsNotNull(device);

      float level;
      endpointVolume.GetMasterVolumeLevelScalar(out level);

      Assert.AreNotEqual(0, level);

      endpointVolume.SetMasterVolumeLevelScalar(level * 1.5f, Guid.NewGuid());

      Playback.SetDefaultPlaybackDevice(devices.First(d => d.Id != id).Id);
    }
  }
}
