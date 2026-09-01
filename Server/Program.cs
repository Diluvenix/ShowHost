using Serilog;
using Server.Model;

namespace Server
{
    internal class Program
    {
        private static readonly LoggingFormatter loggingFormatter = new();

        private static void Main()
        {
            Log.Logger = new LoggerConfiguration()
              .MinimumLevel.Debug()
              .WriteTo.Console(
                  loggingFormatter,
                  restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information
              ).WriteTo.File(
                  loggingFormatter,
                  "Logs/server.log",
                  rollingInterval: RollingInterval.Day,
                  restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Debug
              ).CreateLogger();

            using Server server = new(45678);

            Console.CancelKeyPress += (sender, e) =>
            {
                ServerContext.Cts.Cancel();
                e.Cancel = true;
            };

            try
            {
                Task.Delay(Timeout.Infinite).Wait(ServerContext.Cts.Token);
            }
            catch (OperationCanceledException) { }
        }
    }
}
