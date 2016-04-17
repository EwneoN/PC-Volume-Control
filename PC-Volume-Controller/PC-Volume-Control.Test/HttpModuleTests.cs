using System;
using Audio;
using CsQuery.ExtensionMethods;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nancy;
using Nancy.Testing;
using Newtonsoft.Json;
using PC_Volume_Controller;

namespace PC_Volume_Control.Test
{
  /// <summary>
  /// This class is for testing the dummy methods defined on PC-Volume-Controller.HttpModule.
  /// The main idea is this class will help ensure the HTML serving side of the PC-Volume-Controller works as expected.
  /// </summary>
  [TestClass]
  public class HttpModuleTests
  {
    [TestMethod]
    public void TestDummyIndex()
    {
      Browser browser = new Browser(with => with.Module(new HttpModule()));
      BrowserResponse result = browser.Get("/Dummy", with => with.HttpRequest());

      Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);

      Assert.IsFalse(string.IsNullOrWhiteSpace(result.Body.AsString()));
    }

    [TestMethod]
    public void TestGetDummyVolume()
    {
      Browser browser = new Browser(with => with.Module(new HttpModule()));
      BrowserResponse result = browser.Get("/DummyVolume", with =>
      {
        with.AjaxRequest();
        with.Header("Accept", "application/json");
      });

      Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);

      string json = result.Body.AsString();

      Assert.IsFalse(string.IsNullOrWhiteSpace(json));

      VolumeData data = JsonConvert.DeserializeObject<VolumeData>(json);

      Assert.IsTrue(Math.Abs(data.Volume - 75) < 1);
    }

    [TestMethod]
    public void TestSetDummyVolume()
    {
      Browser browser = new Browser(with => with.Module(new HttpModule()));
      BrowserResponse result = browser.Post("/DummyVolume", with =>
      {
        with.AjaxRequest();
        with.Header("Content-Type", "application/json");
        with.Body(JsonConvert.SerializeObject(new VolumeData
        {
          Volume = 25
        }));
      });

      Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

    [TestMethod]
    public void TestGetDummyDefaultPlaybackDevice()
    {
      Browser browser = new Browser(with => with.Module(new HttpModule()));
      BrowserResponse result = browser.Get("/DummyDefaultPlaybackDevice", with =>
      {
        with.AjaxRequest();
        with.Header("Accept", "application/json");
      });

      Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);

      string json = result.Body.AsString();

      Assert.IsFalse(string.IsNullOrWhiteSpace(json));

      AudioDevice data = JsonConvert.DeserializeObject<AudioDevice>(json);

      Assert.IsNotNull(data);
      Assert.IsTrue(data.Id == "1");
      Assert.IsTrue(data.IsCurrentDevice);
      Assert.IsTrue(data.Name == "Speaker");
    }

    [TestMethod]
    public void TestSetDummyDefaultPlaybackDevice()
    {
      Browser browser = new Browser(with => with.Module(new HttpModule()));
      BrowserResponse result = browser.Post("/DummyDefaultPlaybackDevice", with =>
      {
        with.AjaxRequest();
        with.Header("Content-Type", "application/json");
        with.Body(JsonConvert.SerializeObject(new AudioDevice
        {
          Id = "1",
          IsCurrentDevice = true,
          Name = "Speaker"
        }));
      });

      Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

    [TestMethod]
    public void TestGetDummyPlaybackDevices()
    {
      Browser browser = new Browser(with => with.Module(new HttpModule()));
      BrowserResponse result = browser.Get("/DummyPlaybackDevices", with =>
      {
        with.AjaxRequest();
        with.Header("Accept", "application/json");
      });

      Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);

      string json = result.Body.AsString();

      Assert.IsFalse(string.IsNullOrWhiteSpace(json));

      AudioDeviceList data = JsonConvert.DeserializeObject<AudioDeviceList>(json);

      Assert.IsNotNull(data);
      Assert.IsTrue(data.Count == 3);

      string[] deviceNames = { "Speaker", "Headphones", "Monitor" };

      data.ForEach((d, i) =>
      {
        Assert.IsTrue(d.Id == (i + 1).ToString());
        Assert.IsTrue(i > 0 || d.IsCurrentDevice);
        Assert.IsTrue(d.Name == deviceNames[i]);
      });
    }
  }
}