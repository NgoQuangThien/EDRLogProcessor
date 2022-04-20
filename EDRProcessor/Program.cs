using System.ServiceProcess;

namespace EDRProcessor
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new EDRProcessor()
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
