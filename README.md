# Confext (**Conf**iguration **Ext**raction)

Tool to extract configuration values from appsettings.json

## Install

`dotnet tool install -g Joacar.Confext` or for preview version `dotnet tool install -g Joacar.Confext --version <version>`. For local install `dotnet new tool-manifest && dotnet tool install Joacar.Confext`

## Usage

Piping and redirection of input/output is fully supported. `$cat appsettings.json | confext -p "#{\w+}" -o docker.conf` or `$confext < appsettings.json >> docker.conf` and on Windows `>type appsettings.json | dotnet run`.

When done entering text via the console, send the EOF byte (`Ctrl+Z` or F6 on Windows) to start parsing content.

### API

If exection completed successfully zero exit code will be set, otherwise exit code will be non-zero.

| Arguments | Default | Description |
|-----------|---------|-------------|
| -p\|--pattern | `#{\w+}` | Match configuration values against provided Regex |
| -i\|--input | `stdin` | Path to file or empty to read from stdin |
| -o\|--ouput | `stdout` | File to write or empty to write to stdout |
| -s\|--settings | | List of configuration values pre-pended to output |

## Licensing

confext is licensed under the Apache License, Version 2.0. See [LICENSE](LICENSE) for the full license text.
