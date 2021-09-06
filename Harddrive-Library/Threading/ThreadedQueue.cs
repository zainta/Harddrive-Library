using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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

            if (_actions.Count > threadCount)
            {
                throw new InvalidOperationException($"Action count cannot exceed thread count.");
            }
        }

        /// <summary>
        /// Starts the system
        /// </summary>
        /// <param name="work">The items to work on</param>
        public void Start(IEnumerable<T> work)
        {
            if (Status == ThreadQueueStatus.Idle)
            {
                _workerJobs.Clear();
                _threads.Clear();
                _allWork = new ConcurrentQueue<T>(work);
                _total = work.Count();

                if (_allWork.Count > 0)
                {
                    // Create each of the workers
                    Status = ThreadQueueStatus.Starting;
                    for (int i = 0; i < _desiredThreads; i++)
                    {
                        // get the task
                        _threads.Add(Task.Run(() =>
                        {
                            Guid threadId = Guid.NewGuid();

                            // setup the queue for this thread
                            _workerJobs.TryAdd(threadId, null);

                            // get the action this thread will perform
                            var action = i < _actions.Count ? _actions[i] : _actions.Last();

                            // loop while we have work waiting and we are executing or we have work in our bucket
                            while (Status == ThreadQueueStatus.Starting || Status == ThreadQueueStatus.Active)
                            {
                                if (Status == ThreadQueueStatus.Active)
                                {
                                    if (_workerJobs[threadId] != null)
                                    {
                                        try
                                        {
                                            //Console.WriteLine($"Work Found: {_workerJobs[Thread.CurrentThread.ManagedThreadId]}");
                                            action(_workerJobs[threadId]);
                                        }
                                        catch (Exception ex)
                                        {
                                            Status = ThreadQueueStatus.Faulted;
                                            FaultCause = ex;
                                        }
                                        _workerJobs[threadId] = null;
                                    }
                                }
                            }
                        }));
                    }

                    Status = ThreadQueueStatus.Active;
                    // create the feeder thread and store the ending point
                    _ending =
                        Task.Run(() =>
                        {
                            while (Status == ThreadQueueStatus.Starting || Status == ThreadQueueStatus.Active)
                            {
                                var needy = (from q in _workerJobs 
                                             where q.Value == null 
                                             select q.Key);
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

                                if (needy.Count() == _workerJobs.Count &&
                                    _allWork.IsEmpty)
                                {
                                    break;
                                }
                            }
                        })
                        .ContinueWith((tsk) =>
                        {
                            Status = ThreadQueueStatus.Idle;
                        });
                        
                }
            }
            else
            {
                throw new InvalidOperationException($"ThreadedQueue is active.");
            }
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
