@echo off
REM Pre-Requisites
REM   Install NuGet package coverlet.msbuild and add to UnitTest projects
REM   Install NuGet package ReportGenerator as a global tool:
REM     dotnet tool install --global dotnet-reportgenerator-globaltool --version 4.4.0
REM
REM You can run this script directly from Visual Studio if you wish:
REM   Tools -> External Tools...
REM     Title: Run Code Coverage, Command: cmd.exe, Arguments: /c "Run Code Coverage.bat", Initial dir: $(SolutionDir), Use Output window  

call :RunCoverage RulesEngine-UnitTests
goto :eof

:RunCoverage
pushd %1%
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover /p:CoverletOutput=coverage.xml
if %ERRORLEVEL% neq 0 (
  echo ***********************
  echo ** Unit Tests failed **
  echo ***********************
  exit
)
ReportGenerator.exe -reports:coverage.xml -targetdir:CoverageReports
echo *
echo * Coverage Report: %cd%\CoverageReports\index.htm
echo *
popd
goto :eof