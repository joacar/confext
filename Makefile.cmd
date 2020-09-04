@echo off

setlocal EnableDelayedExpansion

set DEFAULT_VERSION=99.99.99-local

set repoRoot=%~dp0

set artifactsDir=%repoRoot%artifacts

if /i "%1" == "" (
  @echo usage:
  @echo   test      - Run tests and code coverage
  @echo   pack      - Pack nuget
  @echo     ci      - Emulate running in CI environment
  @echo   install   - Install locally from source %artifactsDir%\nuget
  @echo     version - Version number
)

set testDir=%artifactsDir%\test

if /i "%1" == "test" (
  dotnet test ^
    -c Release ^
    -r %testDir%\results\ ^
    -l trx ^
    --settings:%repoRoot%test\CodeCoverage.runsettings ^
    --collect:"XPlat Code Coverage"
)

if /i "%1" == "pack" (
  set "arg=-c Debug"
  if /i "%2" == "ci" (
    set arg=!arg! -p:CI=true
  ) else (
    echo [Local build] Using version %DEFAULT_VERSION% instead of MinVer
    set arg=!arg! -p:Version=%DEFAULT_VERSION% -p:MinVerSkip=true -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg
  )
  echo dotnet pack -p:RepoRoot=%repoRoot% !arg!
)

if /i "%1" == "install" (
  if [%2] == [] (
    set version=%DEFAULT_VERSION%
    echo [Local build] Install package with version %DEFAULT_VERSION%
  ) else (
    set version=%2
  )
  dotnet tool install Joacar.Confext --add-source %artifactsDir%\nuget --version !version!
)
