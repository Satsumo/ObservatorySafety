
using Microsoft.Extensions.Logging.Abstractions;

using NUnit.Framework;

using ObservatorySafety.Watchdog.Infrastructure;

namespace ObservatorySafety.Watchdog.Tests
{
  public class LogTailerTests
  {
    private LogTailer CreateTailer() => new LogTailer(NullLogger<LogTailer>.Instance);

    private string CreateTempLogFile(params string[] lines)
    {
      var path = Path.GetTempFileName();
      File.WriteAllLines(path, lines);
      return path;
    }

    [Test]
    public async Task ReadNewLinesFromOffsetAsync_ReadsAllLines_WhenOffsetIsZero()
    {
      var tailer = CreateTailer();
      var file = CreateTempLogFile("Line1", "Line2", "Line3");

      var (lines, newOffset) = await tailer.ReadNewLinesFromOffsetAsync(file, 0, CancellationToken.None);

      Assert.That(lines.Count, Is.EqualTo(3));
      Assert.That(lines[0], Is.EqualTo("Line1"));
      Assert.That(newOffset, Is.GreaterThan(0));
    }

    [Test]
    public async Task ReadNewLinesFromOffsetAsync_ReadsOnlyNewLines_WhenOffsetIsAdvanced()
    {
      var tailer = CreateTailer();
      var file = CreateTempLogFile("Line1", "Line2");

      // First read
      var (lines1, offset1) = await tailer.ReadNewLinesFromOffsetAsync(file, 0, CancellationToken.None);
      Assert.That(lines1.Count, Is.EqualTo(2));

      // Append new lines
      File.AppendAllLines(file, new[] { "Line3", "Line4" });

      // Second read from previous offset
      var (lines2, offset2) = await tailer.ReadNewLinesFromOffsetAsync(file, offset1, CancellationToken.None);

      Assert.That(lines2.Count, Is.EqualTo(2));
      Assert.That(lines2[0], Is.EqualTo("Line3"));
      Assert.That(lines2[1], Is.EqualTo("Line4"));
      Assert.That(offset2, Is.GreaterThan(offset1));
    }

    [Test]
    public async Task ReadNewLinesFromOffsetAsync_ReturnsEmpty_WhenNoNewLines()
    {
      var tailer = CreateTailer();
      var file = CreateTempLogFile("Line1");

      var (lines1, offset1) = await tailer.ReadNewLinesFromOffsetAsync(file, 0, CancellationToken.None);
      Assert.That(lines1.Count, Is.EqualTo(1));

      var (lines2, offset2) = await tailer.ReadNewLinesFromOffsetAsync(file, offset1, CancellationToken.None);
      Assert.That(lines2, Is.Empty);
      Assert.That(offset2, Is.EqualTo(offset1));
    }

    [Test]
    public async Task ReadNewLinesFromOffsetAsync_ResetsOffset_WhenFileShrinks()
    {
      var tailer = CreateTailer();
      var file = CreateTempLogFile("Line1", "Line2", "Line3");

      var (lines1, offset1) = await tailer.ReadNewLinesFromOffsetAsync(file, 0, CancellationToken.None);
      Assert.That(lines1.Count, Is.EqualTo(3));

      // Simulate rollover (truncate file)
      File.WriteAllLines(file, new[] { "NewLine1" });

      var (lines2, offset2) = await tailer.ReadNewLinesFromOffsetAsync(file, offset1, CancellationToken.None);

      Assert.That(lines2.Count, Is.EqualTo(1));
      Assert.That(lines2[0], Is.EqualTo("NewLine1"));
      Assert.That(offset2, Is.GreaterThan(0));
    }
  }
}
