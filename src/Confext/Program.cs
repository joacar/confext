using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Confext
{
    public static class Program
    {
        private static readonly string _informationalVersion =
            typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;

        public static async Task<int> Main(string[] args) =>
            await BuildCommandLine()
                  .UseHost(_ => Host.CreateDefaultBuilder())
                  .UseDefaults()
                  .Build()
                  .InvokeAsync(args)
                  .ConfigureAwait(false);

        private static string[] Aliases(params string[] aliases) => aliases;

        private static CommandLineBuilder BuildCommandLine()
        {
            var root = new RootCommand(
                $"Configuration Extraction version {_informationalVersion} ({ThisAssembly.Git.Commit})")
            {
                new Option<string>(Aliases("-p", "--pattern"))
                {
                    Description = "Patterns for settings to extract",
                    Required = true
                },
                new Option<string>(Aliases("-i", "--input"), () => string.Empty)
                {
                    Description = "File to read or ignore to read from stdin",
                    Required = false
                },
                new Option<string>(Aliases("-o", "--output"), () => string.Empty)
                {
                    Description = "File to write or ignore to write to stdout",
                    Required = false
                },
                new Option<string[]>(Aliases("-s", "--settings"), Array.Empty<string>)
                {
                    Description = "Configuration values pre-pended to output",
                    Required = false
                },
                new Option<string>("--prefix", () => string.Empty)
                {
                    Description = "Value to prefix configuration keys read",
                    Required = false
                }
            };
            root.Name = "confext";
            root.Handler = CommandHandler.Create<VariableExtractionOptions, IHost>(Run);
            return new CommandLineBuilder(root);
        }

        private static async Task<int> Run(VariableExtractionOptions options, IHost host)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var serviceProvider = host.Services;
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger(typeof(Program));

            var applicationLifetime = serviceProvider.GetRequiredService<IHostApplicationLifetime>();
            var stoppingToken = applicationLifetime.ApplicationStopping;
            ConfigurationTracker tracker;

            try
            {
                await using var input = new InputStream(options.InputFilePath);
                logger.LogDebug("Reading input from {InputStream}", input);
                await using var memoryStream = new MemoryStream();
                await input.CopyToAsync(memoryStream, stoppingToken).ConfigureAwait(false);
                var bytes = memoryStream.ToArray();
                if (bytes.Length == 0)
                {
                    logger.LogInformation("Input stream {InputStream} was empty", input);
                    return 1;
                }

                tracker = ConfigurationTrackerFactory.CreateFromJson(bytes, options.Pattern, options.Prefix);
            }
            catch (OperationCanceledException)
            {
                logger.LogDebug("ReadConfiguration cancelled");
                return 1;
            }
            catch (JsonException exception)
            {
                logger.LogError(exception, "Failed to read JSON configuration");
                return 1;
            }

            Stream output;
            if (!string.IsNullOrEmpty(options.OutputFilePath))
            {
                // Redirecting using Console.SetOut(File.CreateText(string)) doesn't work
                output = File.Create(options.OutputFilePath);
            }
            else
            {
                output = Console.OpenStandardOutput();
            }

            // Underlying stream will be disposed
            await using var streamWriter = new StreamWriter(output);
            foreach (var setting in options.Settings)
            {
                await streamWriter.WriteLineAsync(setting).ConfigureAwait(false);
            }

            await tracker.WriteTo(streamWriter, stoppingToken).ConfigureAwait(false);
            await streamWriter.FlushAsync().ConfigureAwait(false);
            return 0;
        }

        [SuppressMessage("Performance", "CA1812:AvoidUninstantiatedInternalClasses",
            Justification = "Instantiated by System.CommandLine")]
        // ReSharper disable once ClassNeverInstantiated.Local
        private sealed class VariableExtractionOptions
        {
            public VariableExtractionOptions(
                string pattern,
                string input,
                string output,
                string[] settings,
                string prefix
            )
            {
                Pattern = pattern;
                InputFilePath = input;
                OutputFilePath = output;
                Settings = settings;
                Prefix = prefix;
            }

            public string InputFilePath { get; }
            public string OutputFilePath { get; }
            public IReadOnlyCollection<string> Settings { get; }
            public string Prefix { get; }
            public string Pattern { get; }
        }
    }
}
