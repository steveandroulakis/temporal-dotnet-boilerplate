namespace TemporalioSamples.ActivitySimple;

using Temporalio.Activities;
using System;

public class MyActivities
{
    // comment out when breaking activity with CustomActivityException
    private readonly MyDatabaseClient dbClient = new();

    // Activities can be static and/or sync
    [Activity]
    public static string DoStaticThing() => "some-static-value";

    // Activities can be methods that can access state
    [Activity]
    public Task<string> SelectFromDatabaseAsync(string table) =>
        // To make it work: dbClient.SelectValueAsync(table);
        // To break it: throw new CustomActivityException("This is a generic exception.");
        dbClient.SelectValueAsync(table);

    [Activity]
    public static int DoRandomThing()
    {
        Random rand = new Random();
        return rand.Next(3, 6);
    }


    public class MyDatabaseClient
    {
        public Task<string> SelectValueAsync(string table) =>
            Task.FromResult($"some-db-value from table {table}");
    }
}