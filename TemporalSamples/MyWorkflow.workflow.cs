namespace TemporalioSamples.ActivitySimple;

using Microsoft.Extensions.Logging;
using Temporalio.Api.History.V1;
using Temporalio.Workflows;

[Workflow]
public class MyWorkflow
{
    private List<List<int>> currentResults = new List<List<int>>();

    [WorkflowRun]
    public async Task<List<int>[]> RunAsync(Order order)
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

        // await Workflow.ExecuteChildWorkflowAsync(
        // (RandomNumbersChildWorkflow wf) => wf.RunAsync(),
        // new()
        // {
        //     ID = "random-numbers-child-{Guid.NewGuid()}",
        // });
        Workflow.Logger.LogInformation("about to run child workflows");

        Workflow.Logger.LogInformation("child workflows complete");

        // Container to hold task handles for all workflows
        var workflowHandles = new List<Task<List<int>>>();

        // Start 5 workflows
        for (var i = 0; i < 5; i++)
        {
            var childWorkflowId = $"random-numbers-child-{Guid.NewGuid()}";
            var handle = await Workflow.StartChildWorkflowAsync(
                (RandomNumbersChildWorkflow wf) => wf.RunAsync(),
                new()
                {
                    ID = childWorkflowId,
                });

            // Create a task that will complete when the workflow is done and return the ID and result together
            var resultTask = handle.GetResultAsync().ContinueWith(t =>
                    {
                        Console.WriteLine("result is...");
                        Console.WriteLine(string.Join(", ", t.Result));  // print the result
                        currentResults.Add(t.Result);
                        return t.Result;
                    });

            // Add this task to the list of tasks to wait on
            workflowHandles.Add(resultTask);
        }

        // Wait for all workflows to complete and gather their results
        var childResults = await Task.WhenAll(workflowHandles);

        Workflow.Logger.LogInformation("Waiting 1 second just because", result2);
        await Workflow.DelayAsync(1000);
        Workflow.Logger.LogInformation("Finished Workflow!", result2);

        // We'll go ahead and return this result
        // return result2;
        return childResults;
    }

    [WorkflowQuery]
    public string CurrentResults()
    {
        List<string> resultStrings = new List<string>();

        foreach (var innerList in currentResults)
        {
            string innerListString = "[" + String.Join(", ", innerList) + "]";
            resultStrings.Add(innerListString);
        }

        return "[" + String.Join(", ", resultStrings) + "]";
    }


}