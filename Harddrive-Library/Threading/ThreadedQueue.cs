// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HDDL.Threading
{
    /// <summary>
    /// Provides a means of evenly distributing work among a set number of threads in a safe manner
    /// </summary>
    class ThreadedQueue<T> where T : class
    {
        /// <summary>
        /// The number of threads the ThreadedQueue is supposed to use
        /// </summary>
        private int _desiredThreads;

        /// <summary>
        /// Contains the work to be done
        /// </summary>
        private ConcurrentQueue<T> _allWork;

        /// <summary>
        /// When Starting or Active, stores the total number of records
        /// </summary>
        private int _total;

        /// <summary>
        /// The work assigned to individual threads
        /// </summary>
        private readonly ConcurrentDictionary<Guid, T> _workerJobs;

        /// <summary>
        /// The worker threads
        /// </summary>
        private readonly List<Task> _threads;

        /// <summary>
        /// The feeder thread's ending point
        /// </summary>
        private Task _ending;

        /// <summary>
        /// The methods the threads will call
        /// </summary>
        private readonly List<Action<T>> _actions;

        private readonly object _activeTasksLock = new object();

        /// <summary>
        /// The number of active tasks
        /// </summary>
        private int _activeTasks;

        /// <summary>
        /// Indicates whether or not the feeder task is filling the jobs for the workers
        /// </summary>
        private bool _feeding;

        /// <summary>
        /// The ThreadedQueue's current state
        /// </summary>
        public ThreadQueueStatus Status { get; private set; }

        /// <summary>
        /// If the status is Faulted, this property will contain the exception
        /// </summary>
        public Exception FaultCause { get; private set; }

        /// <summary>
        /// Create a ThreadedQueue
        /// </summary>
        /// <param name="action">The method used to perform the work</param>
        /// <param name="threadCount">The number of threads to use</param>
        public ThreadedQueue(Action<T> action, int threadCount = 4) : 
            this(new Action<T>[] { action }, threadCount)
        {

        }

        /// <summary>
        /// Create a ThreadedQueue
        /// </summary>
        /// <param name="actions">The methods to perform the work</param>
        /// <param name="threadCount">The number of threads to use</param>
        public ThreadedQueue(IEnumerable<Action<T>> actions, int threadCount = 4)
        {
            _actions = new List<Action<T>>(actions);
            _allWork = null;
            _workerJobs = new ConcurrentDictionary<Guid, T>();
            _threads = new List<Task>();
            _desiredThreads = threadCount;
            Status = ThreadQueueStatus.Idle;
            _total = -1;
            FaultCause = null;
            _feeding = false;

            if (_actions.Count > threadCount)
            {
                throw new InvalidOperationException($"Action count cannot exceed thread count.");
            }
        }

        /// <summary>
        /// Starts the system
        /// </summary>
        /// <param name="work">The items to work on</param>
        /// <returns>Returns a Task that will complete when the operation has full completed</returns>
        public void Start(IEnumerable<T> work)
        {
            if (Status == ThreadQueueStatus.Idle)
            {
                _workerJobs.Clear();
                _threads.Clear();
                _allWork = new ConcurrentQueue<T>(work);
                _total = work.Count();
                _feeding = false;

                if (_allWork.Count > 0)
                {
                    // Create each of the workers
                    Status = ThreadQueueStatus.Starting;
                    for (int i = 0; i < _desiredThreads; i++)
                    {
                        AddRunner(i);
                    }

                    // create the feeder thread and store the ending point
                    _ending =
                        Task.Run(() =>
                        {
                            try
                            {
                                while (GetTaskCount() > 0)
                                {
                                    var needy = (from q in _workerJobs
                                                 where q.Value == null
                                                 select q.Key);

                                    if (needy.Count() > 0)
                                    {
                                        _feeding = true;
                                    }

                                    foreach (var key in needy)
                                    {
                                        T work;
                                        if (_allWork.TryDequeue(out work))
                                        {
                                            _workerJobs[key] = work;
                                        }
                                        else
                                        {
                                            break;
                                        }
                                    }
                                    _feeding = false;

                                    if (needy.Count() == _workerJobs.Count &&
                                        _allWork.IsEmpty)
                                    {
                                        break;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {

                            }
                        })
                        .ContinueWith((tsk) =>
                        {
                            while (GetTaskCount() > 0)
                            {
                                // wait until all threads complete their work.
                            }

                            Status = ThreadQueueStatus.Idle;
                        });
                    Status = ThreadQueueStatus.Active;
                }
            }
            else
            {
                throw new InvalidOperationException($"ThreadedQueue is active.");
            }
        }

        /// <summary>
        /// Task execution method
        /// </summary>
        /// <param name="taskWorkQueueId">Used to obtain work from the _workerJobs dictionary</param>
        /// <param name="action">The action to execute the job with</param>
        private void ActionRunner(Guid taskWorkQueueId, Action<T> action)
        {
            try
            {
                while (Status == ThreadQueueStatus.Starting)
                {

                }

                // loop while we have work waiting and we are executing or we have work in our bucket
                while (((!_allWork.IsEmpty || _workerJobs[taskWorkQueueId] != null) && 
                    Status == ThreadQueueStatus.Active) || _feeding)
                {
                    if (_workerJobs[taskWorkQueueId] != null)
                    {
                        try
                        {
                            action(_workerJobs[taskWorkQueueId]);
                        }
                        catch (Exception ex)
                        {
                            Status = ThreadQueueStatus.Faulted;
                            FaultCause = ex;
                        }
                        _workerJobs[taskWorkQueueId] = null;
                    }
                }
            }
            catch (KeyNotFoundException ex)
            {
                // we don't care about the exception that occurred.  If one does occur, though, kill the thread and start a new one.
            }
            finally
            {
                // Indicate that we are done.
                SubTask();
            }
        }

        /// <summary>
        /// Adds a Runner at the given index, or the end if the index is too low
        /// </summary>
        /// <param name="index">The index to add the runner at</param>
        private void AddRunner(int index)
        {
            // setup the queue for this thread
            Guid threadId = Guid.NewGuid();
            _workerJobs.TryAdd(threadId, null);

            // get the action this thread will perform
            var action = index < _actions.Count ? _actions[index] : _actions.Last();

            // get the task
            _threads.Add(Task.Run(() => ActionRunner(threadId, action)));

            // Track the new task
            AddTask();
        }

        /// <summary>
        /// Returns a WhenAll task
        /// </summary>
        /// <returns></returns>
        public Task WhenAll()
        {
            return Task.WhenAll(_ending);
        }

        /// <summary>
        /// Waits until the operation has concluded
        /// </summary>
        /// <returns></returns>
        public void WaitAll()
        {
            _ending.Wait();
        }

        /// <summary>
        /// Thread safely returns the number of active tasks
        /// </summary>
        /// <returns></returns>
        private int GetTaskCount()
        {
            var result = 0;
            lock(_activeTasksLock)
            {
                result = _activeTasks;
            }

            return result;
        }

        /// <summary>
        /// Thread safely increments the number of active tasks
        /// </summary>
        private void AddTask()
        {
            lock(_activeTasksLock)
            {
                _activeTasks++;
            }
        }

        /// <summary>
        /// Thread safely decrements the number of active tasks
        /// </summary>
        private void SubTask()
        {
            lock (_activeTasksLock)
            {
                _activeTasks--;
            }
        }

        /// <summary>
        /// Provides a string summary of the worker
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (Status == ThreadQueueStatus.Starting)
            {
                return $"Status: {Status}; {_allWork.Count} total work";
            }
            else if (Status == ThreadQueueStatus.Active)
            {
                return $"Status: {Status}; {_allWork.Count} of {_total} total work";
            }
            else if (Status == ThreadQueueStatus.Faulted)
            {
                return $"Status: {Status};\n Cause: {FaultCause}";
            }
            else
            {
                return $"Status: {Status}";
            }
        }
    }
}
