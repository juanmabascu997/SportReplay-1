using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SportReplay.Application.Abstractions;
using SportReplay.Application.Options;

namespace SportReplay.Infrastructure.Video;

public class FfmpegService : IFfmpegService
{
    private readonly VideoOptions _options;
    private readonly ILogger<FfmpegService> _logger;
    private static readonly ConcurrentDictionary<string, Process> Running = new();

    public FfmpegService(IOptions<VideoOptions> options, ILogger<FfmpegService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<int> RunAsync(string arguments, CancellationToken cancellationToken = default)
    {
        var start = new ProcessStartInfo
        {
            FileName = _options.FfmpegPath,
            Arguments = arguments,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = start };
        try
        {
            process.Start();
            var error = await process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);
            if (process.ExitCode != 0)
            {
                _logger.LogWarning("FFmpeg exited {Code}: {Error}", process.ExitCode, Truncate(error));
            }

            return process.ExitCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FFmpeg failed to start");
            return -1;
        }
    }

    public async Task<bool> ProbeRtspAsync(string rtspUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rtspUrl))
        {
            return false;
        }

        var args = $"-rtsp_transport tcp -i \"{rtspUrl}\" -t 1 -f null -";
        var code = await RunAsync(args, cancellationToken);
        return code == 0;
    }

    public async Task GenerateTestClipAsync(string outputPath, int durationSeconds, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        var args = $"-y -f lavfi -i testsrc=size=1280x720:rate=25 -f lavfi -i sine=frequency=1000:sample_rate=44100 -t {durationSeconds} -c:v libx264 -pix_fmt yuv420p -c:a aac \"{outputPath}\"";
        var code = await RunAsync(args, cancellationToken);
        if (code != 0 && !File.Exists(outputPath))
        {
            await File.WriteAllTextAsync(outputPath, "placeholder-video", cancellationToken);
        }
    }

    private static string Truncate(string value) => value.Length <= 500 ? value : value[..500];
}
