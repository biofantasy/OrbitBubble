using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Extensions.Logging;
using OrbitBubble.Core.Common;
using OrbitBubble.Domain.Bubbles;

namespace OrbitBubble.Domain.Launch;

public sealed class WindowsLauncherService : ILauncherService {

  private readonly ILogger<WindowsLauncherService> _logger;

  public WindowsLauncherService(ILogger<WindowsLauncherService> logger) {
    _logger = logger;
  }

  public Result<bool> Launch(BubbleItem item) {
    try {
      switch (item.Type) {
        case BubbleItemType.File:
          if (string.IsNullOrWhiteSpace(item.Path) || !File.Exists(item.Path))
            return Result<bool>.Fail(new StoreError("LAUNCH_FILE_NOT_FOUND", $"File not found: {item.Path}"));

          Process.Start(new ProcessStartInfo(item.Path) { UseShellExecute = true });
          _logger.LogInformation("Launched file. path={Path}", item.Path);
          return Result<bool>.Ok(true);

        case BubbleItemType.Folder:
          if (string.IsNullOrWhiteSpace(item.Path) || !Directory.Exists(item.Path))
            return Result<bool>.Fail(new StoreError("LAUNCH_DIR_NOT_FOUND", $"Folder not found: {item.Path}"));

          Process.Start(new ProcessStartInfo(item.Path) { UseShellExecute = true });
          _logger.LogInformation("Launched folder. path={Path}", item.Path);
          return Result<bool>.Ok(true);

        case BubbleItemType.Command:
          // 先用最簡：Metadata["command"]、Metadata["args"]（你可自行定 schema）
          if (item.Metadata == null || !item.Metadata.TryGetValue("command", out var cmd) || string.IsNullOrWhiteSpace(cmd))
            return Result<bool>.Fail(new StoreError("LAUNCH_CMD_EMPTY", "Command is empty."));

          item.Metadata.TryGetValue("args", out var args);

          Process.Start(new ProcessStartInfo(cmd) {
            Arguments = args ?? "",
            UseShellExecute = true
          });

          _logger.LogInformation("Launched command. cmd={Cmd} args={Args}", cmd, args);
          return Result<bool>.Ok(true);

        case BubbleItemType.Collection:
          // Collection 不在這裡 launch；由 VM 做 navigation
          return Result<bool>.Fail(new StoreError("LAUNCH_COLLECTION", "Collection cannot be launched."));

        default:
          return Result<bool>.Fail(new StoreError("LAUNCH_UNKNOWN_TYPE", $"Unknown type: {item.Type}"));
      }
    } catch (Exception ex) {
      _logger.LogError(ex, "Launch failed. id={Id} type={Type} path={Path}", item.Id, item.Type, item.Path);
      return Result<bool>.Fail(new StoreError("LAUNCH_EXCEPTION", "Launch threw exception.", ex));
    }
  }
}
