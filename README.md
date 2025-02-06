# Task Extensions

Allows you to write `Task` structs by implementing

* `ITask`
* `ITaskParallelFor`

and calling `Schedule(...)`.

This follows a similar style to Unity's Job System where you implement Job structs with `IJob` and 
`IJobParallelFor`.

Supports the following .NET versions:
* .NET 8 LTS
* .NET 2.1 Standard

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
