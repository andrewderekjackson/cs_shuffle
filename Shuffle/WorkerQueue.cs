using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Shuffle
{
    public class WorkerQueue
    {

        private ConcurrentQueue<FileOperation> queue = new ConcurrentQueue<FileOperation>();
        private Thread thread;
        private bool cancel = false;

        private void OnProcessQueue()
        {
            FileOperation operation;
            while (true)
            {

                if (cancel)
                {
                    return;
                }

                if (queue.TryDequeue(out operation))
                {
                    operation.Execute();
                }
                else
                {
                    Thread.Sleep(1000);
                }

                
            }
            
        }

        public void Start()
        {

            Console.WriteLine("Starting worker thread.");
            cancel = false;
            thread = new Thread(OnProcessQueue);
            thread.Start();
        }

        public void Stop()
        {
            if (thread != null)
            {
                Console.WriteLine("Stopping worker thread.");

                cancel = true;
                thread.Join(new TimeSpan(0, 0, 0, 10));
                thread = null;
            }
            
        }

        public void Add(FileOperation operation)
        {
            FileOperation lastOperation;

            // don't enqueue if the last operation is an exact duplicate.
            if (queue.TryPeek(out lastOperation))
            {
                if (operation == lastOperation)
                {
                    return;
                }
            }
            
            queue.Enqueue(operation);
           
        }

        

    }
}