# dotnet boilerplate

### New Project initialization (don't need to run these as they're already in this project)
```
dotnet add package Temporalio --prerelease
dotnet add package Microsoft.Extensions.Logging --version 7.0.0
dotnet add package Microsoft.Extensions.Logging.Console --version 7.0.0
dotnet add package Microsoft.VisualStudio.Threading.Analyzers --version 17.4.33
```

### Run
```
cd TemporalSamples
dotnet worker run
dotnet workflow run
```