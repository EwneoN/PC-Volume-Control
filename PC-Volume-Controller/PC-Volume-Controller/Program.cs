using System.ServiceProcess;

namespace PC_Volume_Controller
{
  static class Program
  {
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    static void Main()
    {
      ServiceBase.Run(new PcVolumeControlService());
    }
  }
}