namespace TemporalioSamples.ActivitySimple;

using Temporalio.Exceptions;
using Temporalio.Workflows;
using Microsoft.Extensions.Logging;

[Workflow]
public class RandomNumbersChildWorkflow
{
    [WorkflowRun]
    public async Task<List<int>> RunAsync()
    {
        var resultList = new List<int>();

        for (var i = 0; i < 10; i++)
        {

            var resultChild = await Workflow.ExecuteActivityAsync(
                    () => MyActivities.DoRandomThing(),
            new()
            {
                StartToCloseTimeout = TimeSpan.FromMinutes(5),
            });
            await Workflow.DelayAsync(TimeSpan.FromSeconds(resultChild));
            resultList.Add(resultChild);

            // await Workflow.DelayAsync(TimeSpan.FromSeconds(1));
        }

        return resultList;
    }

}