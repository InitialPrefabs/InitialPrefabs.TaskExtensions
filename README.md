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
    var a = new [] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }; // The input array
    var b = new int[10]; // The output array

    await new ParallelAddTask {
        a = a,
        b = b
    }.Schedule(a.Length, 5);

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
