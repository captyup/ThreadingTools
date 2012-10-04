using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace ThreadingTools
{
    public delegate void ConsumeEventHandler<T>(T taskItem);

    public class TaskQueue<T> :IDisposable where T:class
    {
        public ConsumeEventHandler<T> OnConsume;
        
        object locker = new object();
        Thread[] workers;

        Queue<T> taskQ = new Queue<T>();
        public TaskQueue(int workerCount, ConsumeEventHandler<T> OnConsume)
        {
            this.OnConsume = OnConsume;
            workers = new Thread[workerCount];
            // Create and start a separate thread for each worker
            for (int i = 0; i < workerCount; i++)
            {
                workers[i] = new Thread(Consume);
                workers[i].Name = string.Format("Worker{0}", i);
                workers[i].Start();
            }
        }
        public void Dispose()
        {
            // Enqueue one null task per worker to make each exit.
            // 加入null作為Thread中止的訊號，每一個thread需要一個null。
            foreach (Thread worker in workers) InnerEnqueueTask(null);
            //等待所有的thread中止。
            foreach (Thread worker in workers) worker.Join();
        }
        /// <summary>
        /// 將工作項目加入工作佇列中。請勿將null加入佇列，null在佇列中被用來作為中止訊號。
        /// </summary>
        /// <param name="task">欲執行的工作項目</param>
        /// <exception cref="ArgumentNullException"></exception>
        public void EnqueueTask(T task)
        {
            lock (locker)
            {
                if (task == null)
                {
                    throw new System.ArgumentNullException("task", "請勿將null加入佇列，null在佇列中被用來作為中止訊號。");
                }
                taskQ.Enqueue(task);
                Monitor.PulseAll(locker);
            }
        }
        void InnerEnqueueTask(T task)
        {
            lock (locker)
            {
                taskQ.Enqueue(task);
                Monitor.PulseAll(locker);
            }
        }
        void Consume()
        {
           

                while (true)
                {
                    T task;
                    lock (locker)
                    {
                        while (taskQ.Count == 0)
                        {

                            Monitor.Wait(locker);
                        }
                        task = taskQ.Dequeue();
                    }
                    if (task == null)
                    {

                        return; // This signals our exit
                    }

                    //do something

                    if (OnConsume != null)
                    {
                        OnConsume(task);
                    }

                }


           
        }



    }
}
