using Serilog;

namespace Server
{
    internal class Program
    {
        public static CancellationTokenSource Cts = new();

        private static void Main()
        {
            Log.Logger = new LoggerConfiguration()
              .MinimumLevel.Debug()
              .WriteTo.Console(
                  restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information,
                  outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u5}] [{SourceContext}] {Message:lj}{NewLine}"
              ).WriteTo.File(
                  "Logs/server.log",
                  rollingInterval: RollingInterval.Day,
                  restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Debug,
                  outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u5}] [{SourceContext}] {Message:lj}{NewLine}"
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
