using System;
using System.IO;
using System.Text;
using Microsoft.Extensions.FileProviders;

namespace Confext
{
    internal sealed class FakeFileInfo : IFileInfo
    {
        public const string DefaultPhysicalPath = "C:\\Some\\Folder\\appsettings.json";

        /// <summary>
        /// Get content bytes.
        /// </summary>
        public byte[] CreateByteArray() => Encoding.UTF8.GetBytes(Content);

        private string Content { get; } = @"{
            ""Logging"": {
                ""IsEnabled"": ""#{IsEnabled}"",
                ""LogLevel"": {
                    ""Default"": ""Information"",
                    ""Confext"" : ""#{Debug}""
                },
            }
        }";

        /// <inheritdoc />
        public Stream CreateReadStream() => new MemoryStream(Encoding.UTF8.GetBytes(Content));

        /// <inheritdoc />
        public bool Exists { get; set; } = true;

        /// <inheritdoc />
        public long Length => Content?.Length ?? -1;

        /// <inheritdoc />
        public string PhysicalPath { get; set; } = DefaultPhysicalPath;

        /// <inheritdoc />
        public string Name { get; set; } = "appsettings.json";

        /// <inheritdoc />
        public DateTimeOffset LastModified { get; } = DateTimeOffset.Now;

        /// <inheritdoc />
        public bool IsDirectory { get; set; }
    }
}
