﻿using System.CommandLine;
using System.CommandLine.Invocation;
using Microsoft.Extensions.Logging;
using Temporalio.Client;
using Temporalio.Worker;
using TemporalioSamples.ActivitySimple;
using Temporalio.Workflows;
using Temporalio.Api.Enums.V1;

var rootCommand = new RootCommand("Client mTLS sample");

// Helper for client commands
void AddClientCommand(
    string name,
    string desc,
    Func<ITemporalClient, Option<string>, InvocationContext, CancellationToken, Task> func)
{
    var cmd = new Command(name, desc);
    rootCommand!.AddCommand(cmd);

    // Add options
    // var targetHostOption = new Option<string>("--target-host", "Host:port to connect to");
    // targetHostOption.IsRequired = true;
    // var namespaceOption = new Option<string>("--namespace", "Namespace to connect to");
    // namespaceOption.IsRequired = true;
    // var clientCertOption = new Option<FileInfo>("--client-cert", "Client certificate file for auth");
    // clientCertOption.IsRequired = true;
    // var clientKeyOption = new Option<FileInfo>("--client-key", "Client key file for auth");
    // clientKeyOption.IsRequired = true;

    // Read from environment variables
    var temporalAddress = Environment.GetEnvironmentVariable("TEMPORAL_ADDRESS") ?? "localhost:7233";
    var temporalNamespace = Environment.GetEnvironmentVariable("TEMPORAL_NAMESPACE") ?? "default";
    var temporalCertPath = Environment.GetEnvironmentVariable("TEMPORAL_CERT_PATH");
    var temporalKeyPath = Environment.GetEnvironmentVariable("TEMPORAL_KEY_PATH");

    var workflowIdOption = new Option<string>("--workflow-id", "Workflow Id to signal"); // Add this line
    workflowIdOption.IsRequired = false; // Not required

    cmd.AddOption(workflowIdOption); // Add this line

    // Set handler
    cmd.SetHandler(async ctx =>
    {
        // Create client
        var clientOptions = new TemporalClientConnectOptions(temporalAddress)
        {
            Namespace = temporalNamespace!,
        };

        if (!string.IsNullOrEmpty(temporalCertPath) && !string.IsNullOrEmpty(temporalKeyPath))
        {
            clientOptions.Tls = new()
            {
                ClientCert = File.ReadAllBytes(temporalCertPath),
                ClientPrivateKey = File.ReadAllBytes(temporalKeyPath),
            };
        }

        var client = await TemporalClient.ConnectAsync(clientOptions);

        // Run
        await func(client, workflowIdOption, ctx, ctx.GetCancellationToken());
    });
}

// Command to run worker
AddClientCommand("run-worker", "Run worker", async (client, workflowIdOption, ctx, cancelToken) =>
{
    // Cancellation token cancelled on ctrl+c
    using var tokenSource = new CancellationTokenSource();
    Console.CancelKeyPress += (_, eventArgs) =>
    {
        tokenSource.Cancel();
        eventArgs.Cancel = true;
    };

    // Create an activity instance with some state
    var activities = new MyActivities();

    // Run worker until cancelled
    Console.WriteLine("Running worker");
    using var worker = new TemporalWorker(
        client,
        new TemporalWorkerOptions(taskQueue: "random-numbers-example").
            AddActivity(activities.SelectFromDatabaseAsync).
            AddActivity(MyActivities.DoStaticThing).
            AddActivity(MyActivities.DoRandomThing).
            AddWorkflow<MyWorkflow>().
            AddWorkflow<RandomNumbersChildWorkflow>());
    try
    {
        await worker.ExecuteAsync(tokenSource.Token);
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine("Worker cancelled");
    }
});

// Command to run workflow
AddClientCommand("execute-workflow", "Execute workflow", async (client, workflowIdOption, ctx, cancelToken) =>
{
    var workflowId = $"random-numbers-workflow-{Guid.NewGuid()}";
    Console.WriteLine("Executing workflow");
    Console.WriteLine(workflowId);
    var order = new Order("DataSamples/order.json");
    await client.ExecuteWorkflowAsync(
        (MyWorkflow wf) => wf.RunAsync(order),
        new(id: workflowId, taskQueue: "random-numbers-example"));

});

// Command to signal workflow
AddClientCommand("signal-workflow", "Signal workflow", async (client, workflowIdOption, ctx, cancelToken) =>
{
    Console.WriteLine("Sending halt signal to workflow");  

    var workflowId = ctx.ParseResult.GetValueForOption(workflowIdOption) ?? "";
    Console.WriteLine(workflowId);   
    var handle = client.GetWorkflowHandle(workflowId);

    await handle.SignalAsync<MyWorkflow>(wf => wf.HaltSignal());

});

// Add a new standalone command named 'scratch'
var scratchCommand = new Command("scratch", "Prints a test statement");

scratchCommand.SetHandler(
 () =>
    {
        Console.WriteLine("*** test");
        var order = new Order("DataSamples/order.json");
        Console.WriteLine(order.OrderId);
    }
);

rootCommand!.AddCommand(scratchCommand);

// Run
await rootCommand.InvokeAsync(args);