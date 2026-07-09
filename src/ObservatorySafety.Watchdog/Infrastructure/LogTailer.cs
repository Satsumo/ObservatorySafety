using System.Text;

namespace ObservatorySafety.Watchdog.Infrastructure
{
    public class LogTailer
    {
        private readonly object _lock = new();
        private long _lastPosition = 0;

        public async Task<IReadOnlyList<string>> ReadNewLinesAsync(string filePath, CancellationToken cancellationToken)
        {
            var lines = new List<string>();

            if (!File.Exists(filePath))
                return lines;

            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            lock (_lock)
            {
                if (_lastPosition > stream.Length)
                {
                    _lastPosition = 0;
                }

                stream.Seek(_lastPosition, SeekOrigin.Begin);

                using var reader = new StreamReader(stream, Encoding.UTF8, true, 1024, leaveOpen: true);
                string? line;
                while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
                {
                    line = reader.ReadLine();
                    if (line != null)
                    {
                        lines.Add(line);
                    }
                }

                _lastPosition = stream.Position;
            }

            await Task.CompletedTask;
            return lines;
        }
    }
}
