using System.Threading;

namespace DibMultithreading.Scheduler.Task
{
    delegate void TaskImplementation();

    internal class DibTask
    {
        public DibTask(TaskImplementation passedTask)
        {
            gameTask = passedTask;
        }

        private readonly TaskImplementation gameTask;
        private readonly ManualResetEvent manualResetEvent = new ManualResetEvent(false);

        public void KickoffTask()
        {
            gameTask();
        }

        public ManualResetEvent GetResetEvent()
        {
            return manualResetEvent;
        }
    }
}
