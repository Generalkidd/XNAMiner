﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ParallelTasks
{
    /// <summary>
    /// A "work stealing" work scheduler class.
    /// This can give much better performance than <see cref="SimpleScheduler"/> when tasks often launch sub-tasks.
    /// </summary>
    public class WorkStealingScheduler
        : IWorkScheduler
    {
        internal List<Worker> Workers { get; private set; }
        private Queue<Task> tasks;

        /// <summary>
        /// Creates a new instance of the <see cref="WorkStealingScheduler"/> class.
        /// </summary>
        public WorkStealingScheduler()
#if XBOX
            : this(3)     // MartinG@DigitalRune: I recommend using 3 hardware threads on the Xbox 360 
                          // (hardware threads 3, 4, 5). Hardware thread 1 usually runs the main game 
                          // logic and will automatically pick up a Task if it is idle and a Task is 
                          // still queued. My performance experiments (using an actual game) have shown 
                          // that using all 4 hardware threads is not optimal.
#elif WINDOWS_PHONE
            : this(1)     // Cannot access Environment.ProcessorCount on WP7. (Security issue.)
#else
            : this(Environment.ProcessorCount)
#endif
        {
        }

        /// <summary>
        /// Creates a new instance of the <see cref="WorkStealingScheduler"/> class.
        /// </summary>
        /// <param name="numThreads">The number of threads to create.</param>
        public WorkStealingScheduler(int numThreads)
        {
            tasks = new Queue<Task>();
            Workers = new List<Worker>(numThreads);
            for (int i = 0; i < numThreads; i++)
                Workers.Add(new Worker(this, i));

            for (int i = 0; i < numThreads; i++)
            {
                Workers[i].Start();
            }
        }

        internal bool TryGetTask(out Task task)
        {
            if (tasks.Count == 0)
            {
                task = default(Task);
                return false;
            }

            lock (tasks)
            {
                if (tasks.Count > 0)
                {
                    task = tasks.Dequeue();
                    return true;
                }

                task = default(Task);
                return false;
            }
        }

        /// <summary>
        /// Schedules a task for execution.
        /// </summary>
        /// <param name="task">The task to schedule.</param>
        public void Schedule(Task task)
        {
            int threads = task.Item.Work.Options.MaximumThreads;
            var worker = Worker.CurrentWorker;
            if (!task.Item.Work.Options.QueueFIFO && worker != null)
                worker.AddWork(task);
            else
            {
                lock (tasks)
                    tasks.Enqueue(task);
            }

            if (threads > 1)
                WorkItem.Replicable = task;

            for (int i = 0; i < Workers.Count; i++)
            {
                Workers[i].Gate.Set();
            }
        }
    }
}
