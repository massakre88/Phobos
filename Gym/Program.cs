using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Gym;

public static class Program
{
    public static void Main()
    {
        // Enter setup code here
        var foo = new Foo();
        IFoo ifoo = foo;
        var actions = new[]
        {
            new TimedAction("control", () =>
            {
                // do nothing
            }),
            new TimedAction("non-virtual instance", () => { foo.DoSomething(); }),
            new TimedAction("virtual instance", () => { foo.DoSomethingVirtual(); }),
            new TimedAction("static", () => { Foo.DoSomethingStatic(); }),
            new  TimedAction("interface", () => { ifoo.DoSomethingInterface(); }),
        };
        const int timesToRun = 100000000; // Tweak this as necessary
        
        TimeActions(timesToRun, actions);
    }

    public interface IFoo
    {
        void DoSomethingInterface();
    }

    public class Foo : IFoo
    {
        [MethodImpl(MethodImplOptions.NoInlining)] 
        public void DoSomething()
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)] 
        public virtual void DoSomethingVirtual()
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)] 
        public static void DoSomethingStatic()
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void DoSomethingInterface()
        {
        }
    }


    // Define other methods and classes here
    public static void TimeActions(int iterations, params TimedAction[] actions)
    {
        var s = new Stopwatch();
        var length = actions.Length;
        var results = new ActionResult[actions.Length];
        // Perform the actions in their initial order.
        for (int i = 0; i < length; i++)
        {
            var action = actions[i];
            var result = results[i] = new ActionResult { Message = action.Message };
            // Do a dry run to get things ramped up/cached
            result.DryRun1 = s.Time(action.Action, 10);
            result.FullRun1 = s.Time(action.Action, iterations);
        }

        // Perform the actions in reverse order.
        for (var i = length - 1; i >= 0; i--)
        {
            var action = actions[i];
            var result = results[i];
            // Do a dry run to get things ramped up/cached
            result.DryRun2 = s.Time(action.Action, 10);
            result.FullRun2 = s.Time(action.Action, iterations);
        }

        foreach (var result in results)
        {
            Console.WriteLine($"{result.Message} DryRun1: {result.DryRun1} DryRun2: {result.DryRun2} FullRun1: {result.FullRun1} FullRun2: {result.FullRun2}");
        }
    }

    public class ActionResult
    {
        public string Message { get; set; }
        public double DryRun1 { get; set; }
        public double DryRun2 { get; set; }
        public double FullRun1 { get; set; }
        public double FullRun2 { get; set; }
    }

    public class TimedAction(string message, Action action)
    {
        public string Message { get; } = message;
        public Action Action { get; } = action;
    }
}

public static class StopwatchExtensions
{
    public static double Time(this Stopwatch sw, Action action, int iterations)
    {
        sw.Restart();
        for (var i = 0; i < iterations; i++)
        {
            action();
        }
        sw.Stop();

        return sw.Elapsed.TotalMilliseconds;
    }
}