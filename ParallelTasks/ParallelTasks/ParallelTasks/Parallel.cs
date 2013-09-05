﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace ParallelTasks
{
    /// <summary>
    /// A static class containing factory methods for creating tasks.
    /// </summary>
    public static class Parallel
    {

        static readonly WorkOptions DefaultOptions = new WorkOptions() { DetachFromParent = false, MaximumThreads = 1 };
        static IWorkScheduler scheduler;
        static Pool<List<Task>> taskPool = new Pool<List<Task>>();
        static List<WorkItem> callbackBuffer = new List<WorkItem>();

        /// <summary>
        /// Executes all task callbacks on a single thread.
        /// This method is not re-entrant. It is suggested you call it only on the main thread.
        /// </summary>
        public static void RunCallbacks()
        {
            lock (WorkItem.AwaitingCallbacks)
            {
                callbackBuffer.AddRange(WorkItem.AwaitingCallbacks);
                WorkItem.AwaitingCallbacks.Clear();
            }

            for (int i = 0; i < callbackBuffer.Count; i++)
            {
                var item = callbackBuffer[i];
                item.Callback();
                item.Callback = null;
                item.Requeue();
            }

            callbackBuffer.Clear();
        }


        // MartinG@DigitalRune: I made the processor affinity configurable. In some cases a we want 
        // to dedicate a hardware thread to a certain service and don't want the ParallelTasks worker 
        // to run on that same hardware thread.

        /// <summary>
        /// Gets or sets the processor affinity of the worker threads.
        /// </summary>
        /// <value>
        /// The processor affinity of the worker threads. The default value is <c>{ 3, 4, 5, 1 }</c>.
        /// </value>
        /// <remarks>
        /// <para>
        /// In the .NET Compact Framework for Xbox 360 the processor affinity determines the processors 
        /// on which a thread runs. 
        /// </para>
        /// <para>
        /// <strong>Note:</strong> The processor affinity is only relevant in the .NET Compact Framework 
        /// for Xbox 360. Setting the processor affinity has no effect in Windows!
        /// </para>
        /// <para>
        /// <strong>Important:</strong> The processor affinity needs to be set before any parallel tasks
        /// are created. Changing the processor affinity afterwards has no effect.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="value" /> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The specified array is empty or contains invalid values.
        /// </exception>
        public static int[] ProcessorAffinity
        {
            get { return _processorAffinity; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                if (value.Length < 1)
                    throw new ArgumentException("The Parallel.ProcessorAffinity must contain at least one value.", "value");

                if (value.Any(id => id < 0))
                    throw new ArgumentException("The processor affinity must not be negative.", "value");

#if XBOX
                if (value.Any(id => id == 0 || id == 2))
                    throw new ArgumentException("The hardware threads 0 and 2 are reserved and should not be used on Xbox 360.", "value");

                if (value.Any(id => id > 5))
                    throw new ArgumentException("Invalid value. The Xbox 360 has max. 6 hardware threads.", "value");
#endif

                _processorAffinity = value;
            }
        }
        private static int[] _processorAffinity = { 3, 4, 5, 1 };


        /// <summary>
        /// Gets or sets the work scheduler.
        /// This defaults to a <see cref="SimpleScheduler"/> instance.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="value"/> is <see langword="null"/>.
        /// </exception>
        public static IWorkScheduler Scheduler
        {
            get
            {
                if (scheduler == null)
                {
                    IWorkScheduler newScheduler = new WorkStealingScheduler();
                    Interlocked.CompareExchange(ref scheduler, newScheduler, null);
                }

                return scheduler;
            }

            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                Interlocked.Exchange(ref scheduler, value);
            }
        }

        /// <summary>
        /// Starts a task in a secondary worker thread. Intended for long running, blocking, work
        /// such as I/O.
        /// </summary>
        /// <param name="work">The work to execute.</param>
        /// <returns>A task which represents one execution of the work.</returns>
        public static Task StartBackground(IWork work)
        {
            return StartBackground(work, null);
        }

        /// <summary>
        /// Starts a task in a secondary worker thread. Intended for long running, blocking, work
        /// such as I/O.
        /// </summary>
        /// <param name="work">The work to execute.</param>
        /// <param name="completionCallback">A method which will be called in Parallel.RunCallbacks() once this task has completed.</param>
        /// <returns>A task which represents one execution of the work.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="work"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Invalid number of maximum threads set in <see cref="IWork.Options"/>.
        /// </exception>
        public static Task StartBackground(IWork work, Action completionCallback)
        {
            if (work == null)
                throw new ArgumentNullException("work");

            if (work.Options.MaximumThreads < 1)
                throw new ArgumentException("work.Options.MaximumThreads cannot be less than one.");
            var workItem = WorkItem.Get();
            workItem.Callback = completionCallback;
            var task = workItem.PrepareStart(work);
            BackgroundWorker.StartWork(task);
            return task;
        }

        /// <summary>
        /// Starts a task in a secondary worker thread. Intended for long running, blocking, work
        /// such as I/O.
        /// </summary>
        /// <param name="action">The work to execute.</param>
        /// <returns>A task which represents one execution of the action.</returns>
        public static Task StartBackground(Action action)
        {
            return StartBackground(action, null);
        }

        /// <summary>
        /// Starts a task in a secondary worker thread. Intended for long running, blocking, work
        /// such as I/O.
        /// </summary>
        /// <param name="action">The work to execute.</param>
        /// <param name="completionCallback">A method which will be called in Parallel.RunCallbacks() once this task has completed.</param>
        /// <returns>A task which represents one execution of the action.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="action"/> is <see langword="null"/>.
        /// </exception>
        public static Task StartBackground(Action action, Action completionCallback)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            var work = DelegateWork.GetInstance();
            work.Action = action;
            work.Options = DefaultOptions;
            return StartBackground(work, completionCallback);
        }

        /// <summary>
        /// Creates and starts a task to execute the given work.
        /// </summary>
        /// <param name="work">The work to execute in parallel.</param>
        /// <returns>A task which represents one execution of the work.</returns>
        public static Task Start(IWork work)
        {
            return Start(work, null);
        }

        /// <summary>
        /// Creates and starts a task to execute the given work.
        /// </summary>
        /// <param name="work">The work to execute in parallel.</param>
        /// <param name="completionCallback">A method which will be called in Parallel.RunCallbacks() once this task has completed.</param>
        /// <returns>A task which represents one execution of the work.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="work"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Invalid number of maximum threads set in <see cref="IWork.Options"/>.
        /// </exception>
        public static Task Start(IWork work, Action completionCallback)
        {
            if (work == null)
                throw new ArgumentNullException("work");

            if (work.Options.MaximumThreads < 1)
                throw new ArgumentException("work.Options.MaximumThreads cannot be less than one.");

            var workItem = WorkItem.Get();
            workItem.Callback = completionCallback;
            var task = workItem.PrepareStart(work);
            Scheduler.Schedule(task);
            return task;
        }

        /// <summary>
        /// Creates and starts a task to execute the given work.
        /// </summary>
        /// <param name="action">The work to execute in parallel.</param>
        /// <returns>A task which represents one execution of the work.</returns>
        public static Task Start(Action action)
        {
            return Start(action, null);
        }

        /// <summary>
        /// Creates and starts a task to execute the given work.
        /// </summary>
        /// <param name="action">The work to execute in parallel.</param>
        /// <param name="completionCallback">A method which will be called in Parallel.RunCallbacks() once this task has completed.</param>
        /// <returns>A task which represents one execution of the work.</returns>
        public static Task Start(Action action, Action completionCallback)
        {
            return Start(action, new WorkOptions() { MaximumThreads = 1, DetachFromParent = false, QueueFIFO = false }, completionCallback);
        }

        /// <summary>
        /// Creates and starts a task to execute the given work.
        /// </summary>
        /// <param name="action">The work to execute in parallel.</param>
        /// <param name="options">The work options to use with this action.</param>
        /// <returns>A task which represents one execution of the work.</returns>
        public static Task Start(Action action, WorkOptions options)
        {
            return Start(action, options, null);
        }

        /// <summary>
        /// Creates and starts a task to execute the given work.
        /// </summary>
        /// <param name="action">The work to execute in parallel.</param>
        /// <param name="options">The work options to use with this action.</param>
        /// <param name="completionCallback">A method which will be called in Parallel.RunCallbacks() once this task has completed.</param>
        /// <returns>A task which represents one execution of the work.</returns>
        public static Task Start(Action action, WorkOptions options, Action completionCallback)
        {
            if (options.MaximumThreads < 1)
                throw new ArgumentOutOfRangeException("options", "options.MaximumThreads cannot be less than 1.");
            var work = DelegateWork.GetInstance();
            work.Action = action;
            work.Options = options;
            return Start(work, completionCallback);
        }

        /// <summary>
        /// Creates an starts a task which executes the given function and stores the result for later retrieval.
        /// </summary>
        /// <typeparam name="T">The type of result the function returns.</typeparam>
        /// <param name="function">The function to execute in parallel.</param>
        /// <returns>A future which represults one execution of the function.</returns>
        public static Future<T> Start<T>(Func<T> function)
        {
            return Start(function, null);
        }

        /// <summary>
        /// Creates an starts a task which executes the given function and stores the result for later retrieval.
        /// </summary>
        /// <typeparam name="T">The type of result the function returns.</typeparam>
        /// <param name="function">The function to execute in parallel.</param>
        /// <param name="completionCallback">A method which will be called in Parallel.RunCallbacks() once this task has completed.</param>
        /// <returns>A future which represults one execution of the function.</returns>
        public static Future<T> Start<T>(Func<T> function, Action completionCallback)
        {
            return Start<T>(function, DefaultOptions, completionCallback);
        }

        /// <summary>
        /// Creates an starts a task which executes the given function and stores the result for later retrieval.
        /// </summary>
        /// <typeparam name="T">The type of result the function returns.</typeparam>
        /// <param name="function">The function to execute in parallel.</param>
        /// <param name="options">The work options to use with this action.</param>
        /// <returns>A future which represents one execution of the function.</returns>
        public static Future<T> Start<T>(Func<T> function, WorkOptions options)
        {
            return Start<T>(function, options, null);
        }

        /// <summary>
        /// Creates an starts a task which executes the given function and stores the result for later retrieval.
        /// </summary>
        /// <typeparam name="T">The type of result the function returns.</typeparam>
        /// <param name="function">The function to execute in parallel.</param>
        /// <param name="options">The work options to use with this action.</param>
        /// <param name="completionCallback">A method which will be called in Parallel.RunCallbacks() once this task has completed.</param>
        /// <returns>A future which represents one execution of the function.</returns>
        public static Future<T> Start<T>(Func<T> function, WorkOptions options, Action completionCallback)
        {
            if (options.MaximumThreads < 1)
                throw new ArgumentOutOfRangeException("options", "options.MaximumThreads cannot be less than 1.");
            var work = FutureWork<T>.GetInstance();
            work.Function = function;
            work.Options = options;
            var task = Start(work, completionCallback);
            return new Future<T>(task, work);
        }

        /// <summary>
        /// Executes the given work items potentially in parallel with each other.
        /// This method will block until all work is completed.
        /// </summary>
        /// <param name="a">Work to execute.</param>
        /// <param name="b">Work to execute.</param>
        public static void Do(IWork a, IWork b)
        {
            Task task = Start(b);
            a.DoWork();
            task.Wait();
        }

        /// <summary>
        /// Executes the given work items potentially in parallel with each other.
        /// This method will block until all work is completed.
        /// </summary>
        /// <param name="work">The work to execute.</param>
        public static void Do(params IWork[] work)
        {
            List<Task> tasks = taskPool.Get();

            for (int i = 0; i < work.Length; i++)
            {
                tasks.Add(Start(work[i]));
            }

            for (int i = 0; i < tasks.Count; i++)
            {
                tasks[i].Wait();
            }

            tasks.Clear();
            taskPool.Return(tasks);
        }

        /// <summary>
        /// Executes the given work items potentially in parallel with each other.
        /// This method will block until all work is completed.
        /// </summary>
        /// <param name="action1">The work to execute.</param>
        /// <param name="action2">The work to execute.</param>
        public static void Do(Action action1, Action action2)
        {
            var work = DelegateWork.GetInstance();
            work.Action = action2;
            work.Options = DefaultOptions;
            var task = Start(work);
            action1();
            task.Wait();
        }

        /// <summary>
        /// Executes the given work items potentially in parallel with each other.
        /// This method will block until all work is completed.
        /// </summary>
        /// <param name="actions">The work to execute.</param>
        public static void Do(params Action[] actions)
        {
            List<Task> tasks = taskPool.Get();

            for (int i = 0; i < actions.Length; i++)
            {
                var work = DelegateWork.GetInstance();
                work.Action = actions[i];
                work.Options = DefaultOptions;
                tasks.Add(Start(work));
            }

            for (int i = 0; i < actions.Length; i++)
            {
                tasks[i].Wait();
            }

            tasks.Clear();
            taskPool.Return(tasks);
        }

        /// <summary>
        /// Executes a for loop, where each iteration can potentially occur in parallel with others.
        /// </summary>
        /// <param name="startInclusive">The index (inclusive) at which to start iterating.</param>
        /// <param name="endExclusive">The index (exclusive) at which to end iterating.</param>
        /// <param name="body">The method to execute at each iteration. The current index is supplied as the parameter.</param>
        public static void For(int startInclusive, int endExclusive, Action<int> body)
        {
            For(startInclusive, endExclusive, body, 8);
        }

        /// <summary>
        /// Executes a for loop, where each iteration can potentially occur in parallel with others.
        /// </summary>
        /// <param name="startInclusive">The index (inclusive) at which to start iterating.</param>
        /// <param name="endExclusive">The index (exclusive) at which to end iterating.</param>
        /// <param name="body">The method to execute at each iteration. The current index is supplied as the parameter.</param>
        /// <param name="stride">The number of iterations that each processor takes at a time.</param>
        public static void For(int startInclusive, int endExclusive, Action<int> body, int stride)
        {
            var work = ForLoopWork.Get();
            work.Prepare(body, startInclusive, endExclusive, stride);
            var task = Start(work);
            task.Wait();
            work.Return();
        }

        /// <summary>
        /// Executes a foreach loop, where each iteration can potentially occur in parallel with others.
        /// </summary>
        /// <typeparam name="T">The type of item to iterate over.</typeparam>
        /// <param name="collection">The enumerable data source.</param>
        /// <param name="action">The method to execute at each iteration. The item to process is supplied as the parameter.</param>
        public static void ForEach<T>(IEnumerable<T> collection, Action<T> action)
        {
            var work = ForEachLoopWork<T>.Get();
            work.Prepare(action, collection.GetEnumerator());
            var task = Start(work);
            task.Wait();
            work.Return();
        }
    }
}
