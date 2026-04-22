using System.Diagnostics;

namespace WatchTracker.Api.Services;

public class BackgroundRemovalService(ILogger<BackgroundRemovalService> logger) : IBackgroundRemovalService
{
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static readonly TimeSpan ProcessTimeout = TimeSpan.FromMinutes(2);

    public bool IsAvailable
    {
        get
        {
            try
            {
                var psi = new ProcessStartInfo("rembg", ["--help"])
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                using var proc = Process.Start(psi);
                proc?.WaitForExit(5000);
                return proc?.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }
    }

    public async Task<string> RemoveBackgroundAsync(string inputPath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(inputPath))
            throw new FileNotFoundException("Source image not found.", inputPath);

        var dir = Path.GetDirectoryName(inputPath)!;
        var outputFileName = $"{Guid.NewGuid()}_nobg.png";
        var outputPath = Path.Combine(dir, outputFileName);

        await Gate.WaitAsync(cancellationToken);
        try
        {
            var psi = new ProcessStartInfo("rembg")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            psi.ArgumentList.Add("i");
            psi.ArgumentList.Add(inputPath);
            psi.ArgumentList.Add(outputPath);

            using var process = Process.Start(psi)
                ?? throw new InvalidOperationException("Failed to start rembg process.");

            var stderr = await process.StandardError.ReadToEndAsync(cancellationToken);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(ProcessTimeout);

            try
            {
                await process.WaitForExitAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                process.Kill(entireProcessTree: true);
                if (File.Exists(outputPath)) File.Delete(outputPath);
                throw new TimeoutException("Background removal timed out.");
            }

            if (process.ExitCode != 0)
            {
                logger.LogError("rembg failed (exit {ExitCode}): {Stderr}", process.ExitCode, stderr);
                if (File.Exists(outputPath)) File.Delete(outputPath);
                throw new InvalidOperationException("Background removal failed.");
            }

            if (!File.Exists(outputPath))
            {
                logger.LogError("rembg succeeded but output file not found: {Path}", outputPath);
                throw new InvalidOperationException("Background removal produced no output.");
            }

            return outputFileName;
        }
        finally
        {
            Gate.Release();
        }
    }
}
