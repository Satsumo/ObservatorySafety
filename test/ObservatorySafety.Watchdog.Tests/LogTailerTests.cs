using ObservatorySafety.Watchdog.Infrastructure;

namespace ObservatorySafety.Watchdog.Tests
{
    public class LogTailerTests
    {
        [Fact]
        public async Task ReadNewLinesAsync_ReturnsNewLines()
        {
            var tempFile = Path.GetTempFileName();
            await File.WriteAllLinesAsync(tempFile, new[] { "Line1", "Line2" });

            var tailer = new LogTailer();
            var lines = await tailer.ReadNewLinesAsync(tempFile, CancellationToken.None);

            Assert.Equal(2, lines.Count);

            await File.AppendAllLinesAsync(tempFile, new[] { "Line3" });
            var moreLines = await tailer.ReadNewLinesAsync(tempFile, CancellationToken.None);

            Assert.Single(moreLines);
            Assert.Equal("Line3", moreLines[0]);

            File.Delete(tempFile);
        }
    }
}
