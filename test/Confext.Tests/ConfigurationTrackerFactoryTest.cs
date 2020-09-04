using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

using static Xunit.Assert;

namespace Confext
{
    public sealed class ConfigurationTrackerFactoryTest
    {
        [Fact]
        public void Create_ValidJsonFile_CreateConfigurationTracker()
        {
            // Arrange
            var fakeFile = new FakeFileInfo();

            // Act
            var tracker = ConfigurationTrackerFactory.CreateFromJson(
                fakeFile.CreateByteArray(),
                Shared.DefaultPattern);

            // Assert
            Equal(2, tracker.CapturedCount);
        }

        [Fact]
        public async Task Create_ValidJsonFile_CreateConfigurationTrackerPrefix()
        {
            // Arrange
            const string prefix = "PREFIX_";
            var fakeFile = new FakeFileInfo();
            var writer = new StringWriter();

            // Act
            var tracker = ConfigurationTrackerFactory.CreateFromJson(
                fakeFile.CreateByteArray(),
                Shared.DefaultPattern,
                prefix);
            await tracker.WriteTo(writer).ConfigureAwait(false);

            // Assert
            Equal(2, tracker.CapturedCount);
            var values = writer.ToString().Split(Environment.NewLine);
            foreach (var value in values)
            {
                StartsWith(prefix, value, StringComparison.InvariantCulture);
            }
        }
    }
}
