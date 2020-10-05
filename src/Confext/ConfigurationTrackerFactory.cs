using System.Text.Json;

namespace Confext
{
    internal static class ConfigurationTrackerFactory
    {
        public static ConfigurationTracker CreateFromJson(in byte[] bytes, in string pattern) =>
            CreateFromJson(bytes, pattern, string.Empty);

        public static ConfigurationTracker CreateFromJson(
            in byte[] bytes,
            in string pattern,
            in string prefix)
        {
            var jsonReaderOptions = new JsonReaderOptions
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip
            };

            var jsonReader = new Utf8JsonReader(bytes, jsonReaderOptions);
            var tracker = new ConfigurationTracker(pattern, prefix);
            while (jsonReader.Read())
            {
                switch (jsonReader.TokenType)
                {
                    case JsonTokenType.PropertyName:
                        tracker.Push(jsonReader.GetString());
                        break;
                    case JsonTokenType.String:
                        tracker.Value(jsonReader.GetString());
                        break;
                    case JsonTokenType.EndObject:
                    // Pop nested
                    case JsonTokenType.Number:
                    case JsonTokenType.True:
                    case JsonTokenType.False:
                        // Values that can't be string
                        tracker.Pop();
                        break;
                }
            }

            return tracker;
        }
    }
}
