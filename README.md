# dotnet boilerplate

### New Project initialization (don't need to run these as they're already in this project)
```
dotnet add package Temporalio --prerelease
dotnet add package Microsoft.Extensions.Logging --version 7.0.0
dotnet add package Microsoft.Extensions.Logging.Console --version 7.0.0
dotnet add package Microsoft.VisualStudio.Threading.Analyzers --version 17.4.33
dotnet add package System.CommandLine --prerelease
dotnet add package Newtonsoft.Json
```

### Run
```
cd TemporalSamples
```

First, we have to run a worker. In a separate terminal, run the worker from this directory:
```
    dotnet run run-worker --target-host $TEMPORAL_ADDRESS --namespace $TEMPORAL_NAMESPACE --client-cert $TEMPORAL_CERT_PATH --client-key $TEMPORAL_KEY_PATH
```
This will start a worker. To run against Temporal Cloud, `--target-host` may be something like
`my-namespace.a1b2c.tmprl.cloud:7233` and `--namespace` may be something like `my-namespace.a1b2c`.

With that running, in a separate terminal execute the workflow from this directory:
```
    dotnet run execute-workflow --target-host $TEMPORAL_ADDRESS --namespace $TEMPORAL_NAMESPACE --client-cert $TEMPORAL_CERT_PATH --client-key $TEMPORAL_KEY_PATH
```

Get the workflow ID and signal it to prematurely fail the workflow
```
dotnet run signal-workflow --target-host $TEMPORAL_ADDRESS --namespace $TEMPORAL_NAMESPACE --client-cert $TEMPORAL_CERT_PATH --client-key $TEMPORAL_KEY_PATH --workflow-id random-numbers-workflow-a9a0239a-7b23-4d6d-93ef-8446e43d3f1b
```

Run web client ("/" will trigger a workflow execution)
```
cd AspNet
dotnet run
```