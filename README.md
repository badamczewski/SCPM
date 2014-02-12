# SCPM - Smart Computation (Concurrent) Programming Model

`SCPM` is a library that aims to bring a concurrent work distribution model by using the notion of computations as primary resource.

Additionally it will expose most of internal structures, algorithms and sync primitives as well as add new ones that will ease concurrent, parrarel and distributed developement (sometimes computation/actor based models are not the way to go but many structures that build them can be used).

A simple example:

**Creating a computation**
```csharp
Computation<int> computation = new Computation<int>((x) => { Console.WriteLine(++x); });
int state = 0;
computation.Run(state);
computation.WaitForCompletion();
```

**Creating a fiber computation**
```csharp
FiberComputation<int> fiber = new FiberComputation<int>(x =>
{
  Console.WriteLine(++x); return new List<FiberStatus>() { FiberStatus.Done };
});
int state = 0;
fiber.Run(state);
fiber.WaitForCompletion();
```

Fiber computations are enumerable based so to get most of them it is advised to use a `yeild` when context switching.

**Using SCPM underlying thread pool**
```csharp
int state = 0;
SmartThreadPool.QueueWorkItem(new Action<int>((x) => Console.WriteLine(++x)), state);
//Or
Computation<int> computation = new Computation<int>((x) => { Console.WriteLine(++x); });
SmartThreadPool.QueueWorkItem(computation);
```

**Using SCPM underlying fiber pool**
```csharp
FiberComputation<int> fiber = new FiberComputation<int>(x =>
{
    Console.WriteLine(++x); return new List<FiberStatus>() { FiberStatus.Done };
});
int state = 0;

FiberPool.QueueWorkItem(fiber);
```



