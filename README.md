# Task Flow

## New Version (`develop` branch)

The new version, now named TaskFlow, allows you to write Tasks similar to Unity's Job System. You 
will still schedule Task structs, however a `TaskHandle` is returned when scheduling. This allows 
you to take the `TaskHandle` and feed it as a dependency to the next `Task` that needs to be 
scheduling, creating a dependency chain.

The **_most independent_** Tasks will be scheduled to run **first** and the **_least independent_** tasks will be 
scheduled to run **last**. This allows you to chain a bunch of Tasks together to run later in your pipeline.

The core features of this new system are in, the remaining work that needs to be done are:

- [x] Setting up configurations through a Builder struct
- [x] Reducing GC Pressure when scheduling Tasks each frame
- [ ] Combining dependencies together

Supports the following .NET versions:
* .NET 8 LTS
* .NET 2.1 Standard

<details>
<summary>Legacy information of the main branch.</summary>

Allows you to write `Task` structs by implementing

* `ITask`
* `ITaskParallelFor`

and calling `Schedule(...)`.

This follows a similar style to Unity's Job System where you implement Job structs with `IJob` and 
`IJobParallelFor`.

## Example
```cs
struct ParallelAddTask : ITaskParallelFor {
    public int[] a;
    public int[] b;

    public void Execute(int index) {
        b[index] = a[index] + 2;
    }
}

async void Example() {
    // Flush all previously cached Tasks for the GC to collect.
    // You only need to call Flush once per frame, do this in your app's control flow.
    TaskHelper.Flush();
    var a = new [] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }; // The input array
    var b = new int[10]; // The output array

    // Schedules 2 Tasks each processing 5 elements.
    // If you have 9 elements, but delegate 5 units of work per task, then the last
    // task will process 4 units.
    await new ParallelAddTask {
        a = a,
        b = b
    }.Schedule(total: a.Length, workPerTask: 5);

    foreach (var element in b) {
        Console.WriteLine(element);
    }
}
```

```
Output:
3
4
5
6
7
8
9
10
11
12
```

## Build instructions
* Clone the repository
* Run `dotnet build --configuration Release`
* Use the DLL as a dependency in your project

## Future Plans
* Looking into Task Dependency Handle and TaskQueue
* Custom control on queuing the Task but not launching it

</details>
