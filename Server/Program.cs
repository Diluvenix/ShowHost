using Serilog;

namespace Server
{
    internal class Program
    {
        public static CancellationTokenSource Cts = new();
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

            Server server = new(45678);

            Console.CancelKeyPress += (sender, e) =>
            {
                Cts.Cancel();
                e.Cancel = true;
            };

            try
            {
                while (!Cts.IsCancellationRequested)
                {
                    Task.Delay(1000).Wait(Cts.Token);
                }
            }
            catch (OperationCanceledException) { }

            server.Dispose();
        }
    }
}
