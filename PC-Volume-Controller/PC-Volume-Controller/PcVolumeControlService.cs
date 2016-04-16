using System;
using System.Configuration;
using System.IO;
using System.ServiceProcess;
using Nancy;
using Nancy.Conventions;
using Nancy.Hosting.Self;
using static System.DateTime;
using static PC_Volume_Controller.Constants;

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

      //we set the controller service name here using the service name used when installing the service
      Controller.ServiceName = ServiceName;
    }

    #endregion

    #region Private Methods

    private NancyHost CreateHost()
    {
      //if https is to be used the following cmd line operation will need to be performed:
      //--netsh http add sslcert ipport=(host)(:port?)(/rootpath?) certhash=(hash) 
      //  appid=Guid.NewGuid() clientcertnegotiation=enable
      string protocol = ConfigurationManager.AppSettings["HttpProtocol"] ?? DEFAULT_HTTP_PROTOCOL;
      string host = ConfigurationManager.AppSettings["Host"] ?? DEFAULT_HOST;
      string port = ConfigurationManager.AppSettings["Port"] ?? DEFAULT_PORT;
      string rootPath = ConfigurationManager.AppSettings["RootPath"] ?? DEFAULT_ROOT_PATH;

      if (!string.IsNullOrWhiteSpace(rootPath) && rootPath[0] != '/')
      {
        rootPath = "/" + rootPath;
      }

      if (!string.IsNullOrWhiteSpace(port) && port[0] != ':')
      {
        port = ":" + port;
      }

      Uri uri = new Uri($"{protocol}://{host}{port}{rootPath}");

      string autoCreateUrlReservations = ConfigurationManager.AppSettings["AutoCreateUrlReservations"];
      string user = ConfigurationManager.AppSettings["User"];

      NancyHost nancyHost = new NancyHost(uri, new DefaultNancyBootstrapper(), new HostConfiguration
      {
        UrlReservations =
        {
          CreateAutomatically = bool.Parse(autoCreateUrlReservations ?? DEFAULT_AUTO_CREATE_URL_RESERVATIONS),
          User = user ?? DEFAULT_USER
        }
      });

      string disableErrorTracesString = ConfigurationManager.AppSettings["DisableErrorTraces"];

      StaticConfiguration.DisableErrorTraces = !bool.Parse(disableErrorTracesString ?? DEFAULT_DISABLE_ERROR_TRACES);
      StaticContentConventionBuilder.AddDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content"));

      return nancyHost;
    }

    private void Restart()
    {
      //we stop the current host and dispose it then create a new one.
      //we do this in case the service is not authorised to run under the new user
      _NancyHost.Stop();
      _NancyHost.Dispose();

      _NancyHost = CreateHost();
      _NancyHost.Start();
    }

    protected override void OnStart(string[] args)
    {
      try
      {
        AutoLog = bool.Parse(ConfigurationManager.AppSettings["AutoLog"] ?? "true");

        _NancyHost = CreateHost();
        _NancyHost.Start();
      }
      catch (Exception ex) when (!AutoLog)
      {
        string directory = AppDomain.CurrentDomain.BaseDirectory;
        string filePath = Path.Combine(directory, UtcNow.ToString("OnStart failure at yyyy-MM-dd HH_mm_ss"));

        File.AppendAllText(filePath, ex.ToString());
      }
    }

    protected override void OnStop()
    {
      try
      {
        _NancyHost.Stop();
        _NancyHost.Dispose();
      }
      catch (Exception ex) when (!AutoLog)
      {
        string directory = AppDomain.CurrentDomain.BaseDirectory;
        string filePath = Path.Combine(directory, UtcNow.ToString("OnStop failure at yyyy-MM-dd HH_mm_ss"));

        File.AppendAllText(filePath, ex.ToString());
      }
    }

    protected override void OnContinue()
    {
      try
      {
        _NancyHost.Start();
      }
      catch (Exception ex) when (!AutoLog)
      {
        string directory = AppDomain.CurrentDomain.BaseDirectory;
        string filePath = Path.Combine(directory, UtcNow.ToString("OnContinue failure at yyyy-MM-dd HH_mm_ss"));

        File.AppendAllText(filePath, ex.ToString());
      }
    }

    protected override void OnPause()
    {
      try
      {
        _NancyHost.Stop();
      }
      catch (Exception ex) when (!AutoLog)
      {
        string directory = AppDomain.CurrentDomain.BaseDirectory;
        string filePath = Path.Combine(directory, UtcNow.ToString("OnPause failure at yyyy-MM-dd HH_mm_ss"));

        File.AppendAllText(filePath, ex.ToString());
      }
    }

    protected override void OnSessionChange(SessionChangeDescription changeDescription)
    {
      try
      {
        Restart();
      }
      catch (Exception ex) when (!AutoLog)
      {
        string directory = AppDomain.CurrentDomain.BaseDirectory;
        string filePath = Path.Combine(directory, UtcNow.ToString("OnSessionChange failure at yyyy-MM-dd HH_mm_ss"));

        File.AppendAllText(filePath, ex.ToString());
      }
    }

    protected override void OnShutdown()
    {
      try
      {
        _NancyHost.Stop();
        _NancyHost.Dispose();
      }
      catch (Exception ex) when (!AutoLog)
      {
        string directory = AppDomain.CurrentDomain.BaseDirectory;
        string filePath = Path.Combine(directory, UtcNow.ToString("OnShutdown failure at yyyy-MM-dd HH_mm_ss"));

        File.AppendAllText(filePath, ex.ToString());
      }
    }

    protected override void OnCustomCommand(int command)
    {
      try
      {
        if(command != (int)Command.Restart)
        {
          return;
        }

        //useful for config changes
        Restart();
      }
      catch (Exception ex) when (!AutoLog)
      {
        string directory = AppDomain.CurrentDomain.BaseDirectory;
        string filePath = Path.Combine(directory, UtcNow.ToString("OnCustomCommand failure at yyyy-MM-dd HH_mm_ss"));

        File.AppendAllText(filePath, ex.ToString());
      }
    }

    #endregion

    #region Nested Types

    public enum Command
    {
      Unknown = 0,
      Restart = 128
    }

    #endregion
  }
}
