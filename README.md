# Confext (**Conf**iguration **Ext**raction)

[![Nuget][nuget-badge] ![NuGet Downloads][nuget-download-badge]][nuget]
[![Nuget][nuget-pre-badge]][nuget-pre]

[nuget]: https://www.nuget.org/packages/joacar.confext/
[nuget-pre]: https://www.nuget.org/packages/joacar.confext/absoluteLatest
[nuget-pre-badge]: https://img.shields.io/nuget/vpre/Joacar.Confext?color=yellow&label=pre&style=flat-square
[nuget-badge]: https://img.shields.io/nuget/v/joacar.confext.svg?color=green&style=flat-square
[nuget-download-badge]: https://img.shields.io/nuget/dt/joacar.confext?style=flat-square

Tool to extract configuration values from appsettings.json

## Install

Install [confext tool][nuget] using the options below

| | |
|---|---|
| Global | `dotnet tool install -g Joacar.Confext`|
| Local  | `dotnet new tool-manifest && dotnet tool install Joacar.Confext`
| Preview | `dotnet tool install -g Joacar.Confext --version <version>`

Using as part of CI then `dotnet tool update -g Joacar.Confext` is preferred since it will install if not already installed, otherwise update. Hence not returning non-zero exit code causing you build to fail.

## Usage

If execution completed successfully zero exit code will be set, otherwise exit code will be non-zero.

| Arguments | Default | Description |
|-----------|---------|-------------|
| -p\|--pattern | none | Regex used to search for configurations to extract |
| -i\|--input | stdin | Path to file or empty to read from stdin. Terminate with `EOF` or `Ctrl+Z`/`F6` on Windows |
| -o\|--ouput | stdout | File to write or empty to write to stdout |
| -s\|--settings | | List of configuration values pre-pended to output |
| --prefix | none | Prefix for configuration keys read from input |

If reading from console, send the EOF byte (`Ctrl+Z` or F6 on Windows) to signal completion and start parsing.

> If there is trouble running tool using short name `confext` try `dotnet confext`. On Windows a group policy might block the executable, hopefully `dotnet` is allowed.

### *nix

Reading from console
```bash
$dotnet confext -p "#{\w+}" -o .env.local <<EOF
>{ "Logging" : { "LogLevel" : "#{LoggingLogLevel}" } }
>EOF
```

Piping and writing to file

`$cat appsettings.json | dotnet confext -p "#{\w+}" -o .env`

### Windows

Reading from console (log messages omitted)
```cmd
confext -p "#{\w+}" -o .env
{ "Logging" : { "LogLevel" : "#{LogLevel}" } }
^Z
```

Piping and writing to file

`$type appsettings.json | dotnet confext -p "#{\w+}" -o .env`

When done entering text via the console, send the EOF byte (`Ctrl+Z` or F6 on Windows) to start parsing content.

## Licensing

confext is licensed under the Apache License, Version 2.0. See [LICENSE](LICENSE) for the full license text.
