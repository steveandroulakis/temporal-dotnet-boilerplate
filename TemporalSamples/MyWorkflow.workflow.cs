namespace TemporalioSamples.ActivitySimple;

using Microsoft.Extensions.Logging;
using Temporalio.Workflows;

[Workflow]
public class MyWorkflow
{
    [WorkflowRun]
    public async Task<string> RunAsync(Order order)
    {
        // Run an async instance method activity.
        var result1 = await Workflow.ExecuteActivityAsync(
            (MyActivities act) => act.SelectFromDatabaseAsync("some-db-table"),
            new()
            {
                StartToCloseTimeout = TimeSpan.FromMinutes(5),
            });
        Workflow.Logger.LogInformation("Activity instance method result: {Result}", result1);

        // Run a sync static method activity.
        var result2 = await Workflow.ExecuteActivityAsync(
            () => MyActivities.DoStaticThing(),
            new()
            {
                StartToCloseTimeout = TimeSpan.FromMinutes(5),
            });
        Workflow.Logger.LogInformation("Activity static method result: {Result}", result2);

        Workflow.Logger.LogInformation("Waiting 10 seconds just because", result2);
        await Workflow.DelayAsync(10000);
        Workflow.Logger.LogInformation("Finished Workflow!", result2);

        // We'll go ahead and return this result
        // return result2;
        return order.OrderId;
    }
}