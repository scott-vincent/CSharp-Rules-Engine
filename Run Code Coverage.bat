@echo off
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