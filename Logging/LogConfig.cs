using Serilog;
using Serilog.Events;
using System.IO;

namespace OrbitBubble.Logging;

public static class LogConfig {
  public static Serilog.ILogger CreateLogger() {
    var logDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "OrbitBubble",
        "logs");

    Directory.CreateDirectory(logDir);

    return new LoggerConfiguration()
        .MinimumLevel.Debug()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
        .Enrich.FromLogContext()
        .WriteTo.Debug()
        .WriteTo.File(
            path: Path.Combine(logDir, "orbitbubble-.log"),
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 14,
            shared: true,
            flushToDiskInterval: TimeSpan.FromSeconds(1))
        .CreateLogger();
  }
}
