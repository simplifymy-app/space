using Avalonia.Threading;
using SimplifyMySpace.Core;
using SimplifyMySpace.Core.Settings;
using Velopack;
using Velopack.Logging;
using Velopack.Sources;

namespace SimplifyMySpace.App;

public enum UpdateState
{
    Idle,
    NotInstalled,
    Checking,
    UpToDate,
    Ready,
    Failed
}

public static class Updates
{
    public const string Repository = "https://github.com/simplifymy-app/space";

    private static UpdateInfo? _ready;

    public static UpdateState State { get; private set; } = UpdateState.Idle;

    public static string? ReadyVersion => _ready?.TargetFullRelease.Version.ToString();

    public static event Action? Changed;

    public static void CheckInBackground(AppSettings settings)
    {
        if (settings.AutoUpdate)
        {
            Check();
        }
    }

    public static void Check() => Run(async manager =>
    {
        Publish(UpdateState.Checking);
        if (await manager.CheckForUpdatesAsync() is not { } update)
        {
            Publish(UpdateState.UpToDate);
            return;
        }

        await manager.DownloadUpdatesAsync(update);
        _ready = update;
        Publish(UpdateState.Ready);
    });

    public static void Restart() => Run(manager =>
    {
        if (_ready is { } update)
        {
            Dispatcher.UIThread.Post(() => manager.ApplyUpdatesAndRestart(update));
        }

        return Task.CompletedTask;
    });

    private static void Run(Func<UpdateManager, Task> work) => Task.Run(async () =>
    {
        try
        {
            var manager = new UpdateManager(new GithubSource(Repository, null, false));
            if (!manager.IsInstalled)
            {
                Publish(UpdateState.NotInstalled);
                return;
            }

            await work(manager);
        }
        catch (Exception exception)
        {
            Log.Error("Update failed", exception);
            Publish(UpdateState.Failed);
        }
    });

    private static void Publish(UpdateState state) => Dispatcher.UIThread.Post(() =>
    {
        State = state;
        Changed?.Invoke();
    });
}

public sealed class UpdateLog : IVelopackLogger
{
    public static readonly UpdateLog Instance = new();

    public void Log(VelopackLogLevel level, string? message, Exception? exception)
    {
        if (level < VelopackLogLevel.Information)
        {
            return;
        }

        if (level >= VelopackLogLevel.Error)
        {
            Core.Log.Error($"Velopack {message}", exception);
        }
        else
        {
            Core.Log.Info($"Velopack {message}");
        }
    }
}
