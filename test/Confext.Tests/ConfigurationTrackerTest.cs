using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using static Xunit.Assert;

namespace Confext
{
    public class ConfigurationTrackerTest
    {
        [Fact]
        public void Ctor_PatternIsEmpty_Throw()
        {
            // Arrange
            static ConfigurationTracker Sut()
            {
                return new ConfigurationTracker(string.Empty);
            }

            // Act
            var exception = Throws<ArgumentException>(Sut);

            // Assert
            NotEmpty(exception.ParamName);
        }

        [Fact]
        public void Ctor_PatternIsInvalidRegex_Throw()
        {
            // Arrange
            static ConfigurationTracker Sut()
            {
                return new ConfigurationTracker("#{[AZ-az+}");
            }

            // Act
            var exception = Throws<ArgumentException>(Sut);

            // Assert
            Equal("RegexParseException", exception.InnerException!.GetType().Name);
            NotEmpty(exception.ParamName);
        }

        public static IEnumerable<object[]> GetKeys()
        {
            static IEnumerable<string> Params(params string[] keys)
            {
                return keys;
            }

            return new List<object[]>
            {
                new object[]
                {
                    Params("Logging", "LogLevel", "Default"), "#{1}", "Logging__LogLevel__Default=#{1}"
                },
                new object[]
                {
                    Params("Jaeger", "AGENT_HOST"), "$(1)", string.Empty
                }
            };
        }

        [Fact]
        public void Value_NoKeysPushed_Throw()
        {
            // Arrange
            var tracker = new ConfigurationTracker(Shared.DefaultPattern);

            void Sut()
            {
                tracker.Value("#{1}");
            }

            // Act
            var exception = Throws<InvalidOperationException>(Sut);

            // Assert
            NotEmpty(exception.Message);
        }

        [Fact]
        public void Value_ValueDoesNotMatch_NotCaptured()
        {
            // Arrange
            var tracker = new ConfigurationTracker(Shared.DefaultPattern);

            // Act
            tracker.Push("Logging");
            tracker.Push("IsEnabled");
            tracker.Value("$LoggingIsEnabled$");

            // Assert
            Equal(0, tracker.CapturedCount);
        }

        [Fact]
        public void Value_ValueMatch_Captured()
        {
            // Arrange
            var tracker = new ConfigurationTracker(Shared.DefaultPattern);

            // Act
            tracker.Push("Logging");
            tracker.Push("IsEnabled");
            tracker.Value("#{LoggingIsEnabled}");

            // Assert
            Equal(1, tracker.CapturedCount);
        }

        [Fact]
        public void Value_GroupCapturedEmpty_GenerateValue()
        {
            // Arrange
            var tracker = new ConfigurationTracker(@"#{(\w*)}");

            // Act
            tracker.Push("Logging");
            tracker.Push("IsEnabled");
            tracker.Value("#{}");

            // Assert
            Equal(1, tracker.CapturedCount);
            var node = tracker.Nodes[0];
            Equal("#{Logging:IsEnabled}", node.Value);
        }

        [Fact]
        public void Value_GroupCapturedEmpty_PatternDoubleBraces_GenerateValue()
        {
            // Arrange
            var tracker = new ConfigurationTracker(@"^{{(\w*)}}$");

            // Act
            tracker.Push("Logging");
            tracker.Push("IsEnabled");
            tracker.Value("{{}}");

            // Assert
            Equal(1, tracker.CapturedCount);
            var node = tracker.Nodes[0];
            Equal("#{Logging:IsEnabled}", node.Value);
        }

        [Fact]
        public void Value_GroupCaptured_TakeValue()
        {
            // Arrange
            var tracker = new ConfigurationTracker(@"#{(\w*)}");

            // Act
            tracker.Push("Logging");
            tracker.Push("IsEnabled");
            tracker.Value("#{LoggingIsEnabled}");

            // Assert
            Equal(1, tracker.CapturedCount);
            var node = tracker.Nodes[0];
            Equal("{{LoggingIsEnabled}}", node.Value);
        }

        [Theory]
        [MemberData(nameof(GetKeys))]
        public async Task WriteTo_KeysValues_WriteKeyValue(string[] keys, string value, string expected)
        {
            // Arrange
            var tracker = new ConfigurationTracker(Shared.DefaultPattern);
            var sw = new StringWriter();

            // Act
            foreach (var key in keys)
            {
                tracker.Push(key);
            }

            tracker.Value(value);

            await tracker.WriteTo(sw).ConfigureAwait(false);

            // Assert
            Equal(expected, sw.ToString());
        }

        [Fact]
        public async Task WriteTo_WithPrefix_PrefixPrependedToKey()
        {
            // Arrange
            var tracker = new ConfigurationTracker(Shared.DefaultPattern, "PREFIX_");
            var sw = new StringWriter();

            // Act
            tracker.Push("Key");
            tracker.Value("#{Value}");
            await tracker.WriteTo(sw).ConfigureAwait(false);

            // Assert
            Equal("PREFIX_Key=#{Value}", sw.ToString());
        }
    }
}
