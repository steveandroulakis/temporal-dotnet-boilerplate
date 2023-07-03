using System.CommandLine;
using System.CommandLine.Invocation;
using Microsoft.Extensions.Logging;
using Temporalio.Client;
using Temporalio.Worker;
using TemporalioSamples.ActivitySimple;

var rootCommand = new RootCommand("Client mTLS sample");

// Helper for client commands
void AddClientCommand(
    string name,
    string desc,
    Func<ITemporalClient, CancellationToken, Task> func)
{
    var cmd = new Command(name, desc);
    rootCommand!.AddCommand(cmd);

    // Add options
    var targetHostOption = new Option<string>("--target-host", "Host:port to connect to");
    targetHostOption.IsRequired = true;
    var namespaceOption = new Option<string>("--namespace", "Namespace to connect to");
    namespaceOption.IsRequired = true;
    var clientCertOption = new Option<FileInfo>("--client-cert", "Client certificate file for auth");
    clientCertOption.IsRequired = true;
    var clientKeyOption = new Option<FileInfo>("--client-key", "Client key file for auth");
    clientKeyOption.IsRequired = true;
    cmd.AddOption(targetHostOption);
    cmd.AddOption(namespaceOption);
    cmd.AddOption(clientCertOption);
    cmd.AddOption(clientKeyOption);

    // Set handler
    cmd.SetHandler(async ctx =>
    {
        // Create client
        var client = await TemporalClient.ConnectAsync(
            new(ctx.ParseResult.GetValueForOption(targetHostOption)!)
            {
                Namespace = ctx.ParseResult.GetValueForOption(namespaceOption)!,
                // Set TLS options with client certs. Note, more options could
                // be added here for server CA (i.e. "ServerRootCACert") or SNI
                // override (i.e. "Domain") for self-hosted environments with
                // self-signed certificates.
                Tls = new()
                {
                    ClientCert =
                        File.ReadAllBytes(ctx.ParseResult.GetValueForOption(clientCertOption)!.FullName),
                    ClientPrivateKey =
                        File.ReadAllBytes(ctx.ParseResult.GetValueForOption(clientKeyOption)!.FullName),
                },
            });
        // Run
        await func(client, ctx.GetCancellationToken());
    });
}

// Command to run worker
AddClientCommand("run-worker", "Run worker", async (client, cancelToken) =>
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
AddClientCommand("execute-workflow", "Execute workflow", async (client, cancelToken) =>
{
    Console.WriteLine("Executing workflow");
    var order = new Order("DataSamples/order.json");
    await client.ExecuteWorkflowAsync(
        (MyWorkflow wf) => wf.RunAsync(order),
        new(id: $"random-numbers-workflow-{Guid.NewGuid()}", taskQueue: "random-numbers-example"));

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