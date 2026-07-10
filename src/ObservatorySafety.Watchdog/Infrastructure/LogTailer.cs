using System.Text;

using ObservatorySafety.Core;
using ObservatorySafety.Watchdog.Alerts;

namespace ObservatorySafety.Watchdog.Infrastructure
{
  public class LogTailer
  {
    private ILogger<LogTailer>? _loggerBase;
    private ILogger<LogTailer> _logger =>
        _loggerBase ??= LogProvider.Factory!.CreateLogger<LogTailer>();

    public LogTailer()
    {
    }

    public string? GetLatestLogFile(string directory, string pattern)
    {
      if (!Directory.Exists(directory))
        return null;

      var files = Directory.GetFiles(directory, pattern);
      if (files.Length == 0)
        return null;

      return files
          .Select(f => new FileInfo(f))
          .OrderByDescending(f => f.LastWriteTimeUtc)
          .First()
          .FullName;
    }

    /// <summary>
    /// Reads only new lines from a log file starting at a given byte offset.
    /// Returns (newLines, newOffset).
    /// </summary>
    public async Task<(List<string> lines, long newOffset)> ReadNewLinesFromOffsetAsync(
        string filePath,
        long offset,
        CancellationToken token)
    {
      var result = new List<string>();

      try
      {
        using var fs = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite);

        // Handle rollover (file shrank)
        if (fs.Length < offset)
        {
          offset = 0;
        }

        fs.Seek(offset, SeekOrigin.Begin);

        using var reader = new StreamReader(fs, Encoding.UTF8);

        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
          result.Add(line);
        }

        long newOffset = fs.Position;
        return (result, newOffset);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error tailing log file {File}", filePath);
        return (new List<string>(), offset);
      }
    }
  }
}
