using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;

[assembly: DebuggerDisplay("{Name}, Exists = {Exists}, Path = {PhysicalPath, nq}", Target = typeof(PhysicalFileInfo))]

namespace Confext
{
    /// <summary>
    /// Stream that handle input from file and console.
    /// </summary>
    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + "(),nq}")]
    internal sealed class InputStream : Stream
    {
        private readonly IFileInfo _fileInfo;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Stream _stream;

        /// <summary>
        /// Create instance reading from <paramref name="fullFilePath" /> if exists, otherwise
        /// <see cref="Console.OpenStandardInput()" />.
        /// </summary>
        /// <param name="fullFilePath">Full file path.</param>
        public InputStream(string fullFilePath) : this(FileInfo(fullFilePath)) { }

        // Used internally for testing so lets keep param name synched with public ctor
        internal InputStream(IFileInfo fullFilePath)
        {
            _fileInfo = fullFilePath;
            if (_fileInfo.IsDirectory)
            {
                throw new ArgumentException("Path can not be directory", nameof(fullFilePath));
            }

            if (!_fileInfo.Exists)
            {
                throw new ArgumentException("File does not exists", nameof(fullFilePath));
            }

            _stream = _fileInfo.CreateReadStream();
        }

        /// <summary>
        /// Gets input source.
        /// </summary>
        // Since I'm abusing FileInfo and require Exists=true to pass ctor we can't use that property
        // to determine source correctly.
        public string Source =>
            ConsoleFile.IsConsoleFile(_fileInfo) ? "stdin(console)" : $"file('{_fileInfo.PhysicalPath}')";

        public override string ToString() => Console.IsInputRedirected ? "stdin(redirected)" : Source;

        private string DebuggerDisplay() => ToString();

        private static IFileInfo FileInfo(string fullFilePath) =>
            string.IsNullOrEmpty(fullFilePath)
                ? ConsoleFile.Default
                : new PhysicalFileInfo(new FileInfo(fullFilePath));

        private static InvalidOperationException ReadOnly() => new InvalidOperationException("Read-only stream");

        [DebuggerDisplay("Virtual file {" + nameof(Name) + "}")]
        [ExcludeFromCodeCoverage]
        private sealed class ConsoleFile : IFileInfo
        {
            private const string _fileName = "Console";
            public static readonly IFileInfo Default = new ConsoleFile();

            private ConsoleFile() { }

            /// <inheritdoc />
            public Stream CreateReadStream() => Console.OpenStandardInput();

            /// <inheritdoc />
            public bool Exists => true;

            /// <inheritdoc />
            public long Length => -1L;

            /// <inheritdoc />
            public string PhysicalPath => string.Empty;

            /// <inheritdoc />
            public string Name => _fileName;

            /// <inheritdoc />
            public DateTimeOffset LastModified => DateTimeOffset.MinValue;

            /// <inheritdoc />
            public bool IsDirectory => false;

            public static bool IsConsoleFile(IFileInfo fileInfo) => fileInfo.Name == _fileName;
        }

        #region Overrides of Stream

        /// <inheritdoc />
        [ExcludeFromCodeCoverage]
        public override void Flush() => _stream.Flush();

        /// <inheritdoc />
        [ExcludeFromCodeCoverage]
        public override int Read(byte[] buffer, int offset, int count) => _stream.Read(buffer, offset, count);

        /// <inheritdoc />
        [ExcludeFromCodeCoverage]
        public override long Seek(long offset, SeekOrigin origin) => _stream.Seek(offset, origin);

        /// <inheritdoc />
        [ExcludeFromCodeCoverage]
        public override void SetLength(long value) => _stream.SetLength(value);

        /// <inheritdoc />
        [ExcludeFromCodeCoverage]
        public override void Write(byte[] buffer, int offset, int count) => throw ReadOnly();

        /// <inheritdoc />
        [ExcludeFromCodeCoverage]
        public override bool CanRead => _stream.CanRead;

        /// <inheritdoc />
        [ExcludeFromCodeCoverage]
        public override bool CanSeek => _stream.CanSeek;

        /// <inheritdoc />
        [ExcludeFromCodeCoverage]
        public override bool CanWrite => throw ReadOnly();

        /// <inheritdoc />
        public override long Length => _stream.Length;

        /// <inheritdoc />
        [ExcludeFromCodeCoverage]
        public override long Position
        {
            get => _stream.Position;
            set => _stream.Position = value;
        }

        /// <inheritdoc />
        [ExcludeFromCodeCoverage]
        public override async ValueTask DisposeAsync()
        {
            await _stream.DisposeAsync().ConfigureAwait(false);
            await base.DisposeAsync().ConfigureAwait(false);
        }

        #endregion
    }
}
