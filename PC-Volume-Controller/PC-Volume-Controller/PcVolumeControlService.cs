using System;
using System.Configuration;
using System.IO;
using System.ServiceProcess;
using Nancy;
using Nancy.Conventions;
using Nancy.Hosting.Self;

namespace PC_Volume_Controller
{
  public partial class PcVolumeControlService : ServiceBase
  {
    #region Variables

    private NancyHost _NancyHost;

    #endregion


    #region Constructors

    public PcVolumeControlService()
    {
      InitializeComponent();
    }

    #endregion

    #region Private Methods

    protected override void OnStart(string[] args)
    {
      string protocol = ConfigurationManager.AppSettings["HttpProtocol"];
      string host = ConfigurationManager.AppSettings["Host"];
      string port = ConfigurationManager.AppSettings["Port"];
      string rootPath = ConfigurationManager.AppSettings["RootPath"];

      Uri uri = new Uri($"{protocol}://{host}:{port}{rootPath}");

      _NancyHost = new NancyHost(uri, new DefaultNancyBootstrapper(), new HostConfiguration
      {
        UrlReservations =
        {
          CreateAutomatically = bool.Parse(ConfigurationManager.AppSettings["AutoCreateUrlReservations"]),
          User = ConfigurationManager.AppSettings["User"]
        }
      });

      StaticConfiguration.DisableErrorTraces = false;
      StaticContentConventionBuilder.AddDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content"));

      _NancyHost.Start();
    }

    protected override void OnStop()
    {
      _NancyHost.Stop();
      _NancyHost.Dispose();
    }

    protected override void OnContinue()
    {
      _NancyHost.Start();
    }

    protected override void OnPause()
    {
      _NancyHost.Stop();
    }

    #endregion
  }
}
