using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using OrbitBubble.Logging;
using OrbitBubble.Core.Helpers;
using OrbitBubble.Core.Managers;
using OrbitBubble.Infrastructure.Input;
using OrbitBubble.Domain.Gestures;
using OrbitBubble.Infrastructure.Gestures;
using OrbitBubble.Core.Icons;
using OrbitBubble.Core.Stores;
using OrbitBubble.Domain.Bubbles;
using OrbitBubble.ViewModels;
using OrbitBubble.Domain.Launch;
using OrbitBubble.Domain;

namespace OrbitBubble {
  public partial class App : System.Windows.Application {

    private IHost? _host;

    protected override void OnStartup(StartupEventArgs e) {

      base.OnStartup(e);

      // 1) 建 Serilog logger
      Log.Logger = LogConfig.CreateLogger();



      // 2) 建 Host + DI
      _host = Host.CreateDefaultBuilder()
          .ConfigureServices((context, services) => {
            // Logging
            services.AddLogging(lb => {
              lb.ClearProviders();
              lb.AddSerilog(Log.Logger, dispose: false);
            });

            // ✅ 把 GestureOptions 從 appsettings.json 綁定進來
            //services.Configure<OrbitBubble.Domain.Gestures.GestureOptions>(
            //    context.Configuration.GetSection("Gesture"));

            // Phase 1 會把 IInputTriggerService 註冊在這裡
            // ✅ 你的 AddSingleton 就放這裡
            services.AddSingleton<GlobalMouseHook>();
            services.AddSingleton<HotkeyManager>();

            services.AddSingleton<IInputTriggerService>(sp =>
            {
              var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<OrbitBubble.Infrastructure.Input.InputTriggerService>>();
              var mouseHook = sp.GetRequiredService<GlobalMouseHook>();
              var hotkey = sp.GetRequiredService<HotkeyManager>();

              return new OrbitBubble.Infrastructure.Input.InputTriggerService(
                  logger,
                  System.Windows.Application.Current.Dispatcher,
                  mouseHook,
                  hotkey);
            });

            services.AddSingleton(new GestureOptions {
              WindowMs = 1200,
              MinPoints = 24,
              MinDistanceDip = 3.0,
              MinRadiusDip = 35.0,
              MaxRadiusDip = 450.0,
              AngleCoverageThreshold = 0.85,
              RadiusStdDevRatioMax = 0.50,
              CooldownMs = 500
            });

            services.AddSingleton<IGestureDetector, CircleGestureDetector>();
            services.AddSingleton<IGestureDetectionService, GestureDetectionService>();

            services.AddSingleton<IIconProvider, ShellIconProvider>();

            services.AddSingleton<IBubbleStore, JsonBubbleStore>();

            services.AddSingleton<IBubbleService, BubbleService>();

            services.AddSingleton<MainViewModel>();

            services.AddSingleton<ILauncherService, WindowsLauncherService>();

            // 先讓主視窗也能從 DI 拿到 logger 或 service
            services.AddTransient<MainWindow>();
          })
          .Build();

      AppServices.Provider = _host.Services;

      // 3) 全域例外鉤子
      var logger = _host.Services.GetRequiredService<ILogger<App>>();
      HookGlobalExceptions(logger);

      logger.LogInformation("App starting. Version={Version}", typeof(App).Assembly.GetName().Version);

      // 4) 顯示主視窗（改成 DI new）
      var mainWindow = _host.Services.GetRequiredService<MainWindow>();
      //mainWindow.Show();
      mainWindow.Opacity = 0;
      mainWindow.Show();
      mainWindow.Hide();
      mainWindow.Opacity = 1;
    }

    protected override void OnExit(ExitEventArgs e) {
      var logger = _host?.Services.GetService<ILogger<App>>();
      logger?.LogInformation("App exiting. Code={Code}", e.ApplicationExitCode);

      _host?.Dispose();
      Log.CloseAndFlush();
      base.OnExit(e);
    }

    private void HookGlobalExceptions(Microsoft.Extensions.Logging.ILogger logger) {
      this.DispatcherUnhandledException += (_, ex) =>
      {
        logger.LogCritical(ex.Exception, "DispatcherUnhandledException");
        ex.Handled = true; // 你也可以選擇顯示錯誤後 Shutdown
      };

      AppDomain.CurrentDomain.UnhandledException += (_, ex) =>
      {
        logger.LogCritical(ex.ExceptionObject as Exception,
            "AppDomain.UnhandledException IsTerminating={IsTerminating}",
            ex.IsTerminating);
      };

      TaskScheduler.UnobservedTaskException += (_, ex) =>
      {
        logger.LogCritical(ex.Exception, "TaskScheduler.UnobservedTaskException");
        ex.SetObserved();
      };
    }
  }
}

