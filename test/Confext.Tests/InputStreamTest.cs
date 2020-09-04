using System;
using System.IO;
using Xunit;

using static Xunit.Assert;

namespace Confext
{
    public class InputStreamTest
    {
        [Fact]
        public void Ctor_FullFilePathEmpty_SourceConsole()
        {
            // Arrange
            // Act
            using var stream = new InputStream(string.Empty);

            // Assert
            Equal("stdin(console)", stream.Source);
        }

        [Fact]
        public void Ctor_FileInfo_SourceFile()
        {
            // Arrange
            var fakeFile = new FakeFileInfo();


            // Act
            using var stream = new InputStream(fakeFile);

            // Assert
            Equal($"file('{FakeFileInfo.DefaultPhysicalPath}')", stream.Source);
        }

        [Fact]
        public void Ctor_FileIsDirectory_Throw()
        {
            // Arrange
            var fakeFile = new FakeFileInfo
            {
                IsDirectory = true
            };

            // Act
            Stream Sut() => new InputStream(fakeFile);

            // Assert
            Throws<ArgumentException>(Sut);
        }

        [Fact]
        public void Ctor_FileDoesNotExists_Throw()
        {
            // Arrange

            var fakeFile = new FakeFileInfo
            {
                Exists = false
            };

            // Act
            Stream Sut() => new InputStream(fakeFile);

            // Assert
            Throws<ArgumentException>(Sut);
        }
    }
}
