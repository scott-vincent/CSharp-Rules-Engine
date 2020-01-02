dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover /p:CoverletOutput=..\coverage.xml
ReportGenerator.exe -reports:.\coverage.xml -targetdir:.\CoverageReports