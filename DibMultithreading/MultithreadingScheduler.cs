using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using Unity.Jobs;
using DibMultithreading.Scheduler.Task;

namespace DibMultithreading.Scheduler
{
    public class MultithreadingScheduler
    {
        #region Constructor

        public MultithreadingScheduler()
        {
            processorCount = Environment.ProcessorCount;
            internalThreadPool = new Thread[processorCount];
            for (int i = 0; i < processorCount; ++i) {
                internalThreadPool[i] = new Thread(ThreadExecution);
                internalThreadPool[i].Start();
            }
        }

        #endregion

        #region Data

        private static Main modReference;

        private readonly Dictionary<ITaskThreadedGameObject, DibTask> evalUpdateTaskMap = new Dictionary<ITaskThreadedGameObject, DibTask>();
        private readonly List<ManualResetEvent> evalUpdateTaskHandles = new List<ManualResetEvent>();
        private readonly Dictionary<ITaskThreadedGameObject, DibTask> applyUpdateTaskMap = new Dictionary<ITaskThreadedGameObject, DibTask>();
        private readonly List<ManualResetEvent> applyUpdateTaskHandles = new List<ManualResetEvent>();
        private readonly Dictionary<ITaskThreadedGameObject, DibTask> evalFixedUpdateTaskMap = new Dictionary<ITaskThreadedGameObject, DibTask>();
        private readonly List<ManualResetEvent> evalFixedUpdateTaskHandles = new List<ManualResetEvent>();
        private readonly Dictionary<ITaskThreadedGameObject, DibTask> applyFixedUpdateTaskMap = new Dictionary<ITaskThreadedGameObject, DibTask>();
        private readonly List<ManualResetEvent> applyFixedUpdateTaskHandles = new List<ManualResetEvent>();
        private readonly Dictionary<ITaskThreadedGameObject, DibTask> evalLateUpdateTaskMap = new Dictionary<ITaskThreadedGameObject, DibTask>();
        private readonly List<ManualResetEvent> evalLateUpdateTaskHandles = new List<ManualResetEvent>();
        private readonly Dictionary<ITaskThreadedGameObject, DibTask> applyLateUpdateTaskMap = new Dictionary<ITaskThreadedGameObject, DibTask>();
        private readonly List<ManualResetEvent> applyLateUpdateTaskHandles = new List<ManualResetEvent>();

        private readonly List<IJobThreadedGameObject> jobList = new List<IJobThreadedGameObject>();
        private readonly List<JobHandle> jobHandles = new List<JobHandle>();

        private readonly Thread[] internalThreadPool;
        private readonly int processorCount;
        private readonly ConcurrentQueue<DibTask> threadTasks = new ConcurrentQueue<DibTask>();
        private readonly ManualResetEvent threadTriggerEvent = new ManualResetEvent(false);

        #endregion

        #region Registration methods

        public static void RegisterBaseModToScheduler(Main mod)
        {
            modReference = mod;
        }

        // Allows registration and removal from anywhere!
        public static MultithreadingScheduler RegisterGameObjectAsMultithreaded(ITaskThreadedGameObject gameObject)
        {
            MultithreadingScheduler schedulerInstance = null;

            if (gameObject == null) {
                modReference.LogWarning("DMT: Null object passed to RegisterGameObjectAsMultithreaded");
            }
            else if (modReference == null) {
                modReference.LogWarning("DMT: Multithreaded object registered before mod loaded");
            }
            else {
                schedulerInstance = modReference.RegisterGameObjectToScheduler(gameObject);
            }

            return schedulerInstance;
        }

        public static MultithreadingScheduler RegisterGameObjectAsMultithreaded(IJobThreadedGameObject gameObject)
        {
            MultithreadingScheduler schedulerInstance = null;

            if (gameObject == null) {
                modReference.LogWarning("DMT: Null object passed to RegisterGameObjectAsMultithreaded");
            }
            else if (modReference == null) {
                modReference.LogWarning("DMT: Multithreaded object registered before mod loaded");
            }
            else {
                schedulerInstance = modReference.RegisterGameObjectToScheduler(gameObject);
            }

            return schedulerInstance;
        }
        public static void RemoveGameObjectAsMultithreaded(ITaskThreadedGameObject gameObject)
        {
            if (gameObject == null) {
                modReference.LogWarning("DMT: Null object passed to RemoveGameObjectAsMultithreaded");
            }
            else if (modReference == null) {
                modReference.LogWarning("DMT: Multithreaded object removed before mod loaded");
            }
            else {
                modReference.RemoveGameObjectFromScheduler(gameObject);
            }
        }

        public static void RemoveGameObjectAsMultithreaded(IJobThreadedGameObject gameObject)
        {
            if (gameObject == null) {
                modReference.LogWarning("DMT: Null object passed to RemoveGameObjectAsMultithreaded");
            }
            else if (modReference == null) {
                modReference.LogWarning("DMT: Multithreaded object removed before mod loaded");
            }
            else {
                modReference.RemoveGameObjectFromScheduler(gameObject);
            }
        }

        public void RemoveGameObjectInternal(ITaskThreadedGameObject gameObject)
        {
            if (gameObject.GetSupportedTasks().EvalUpdate) {
                if (evalUpdateTaskMap.TryGetValue(gameObject, out DibTask task)) {
                    evalUpdateTaskHandles.Remove(task.GetResetEvent());
                }
                evalUpdateTaskMap.Remove(gameObject);
            }
            if (gameObject.GetSupportedTasks().ApplyUpdate) {
                if (applyUpdateTaskMap.TryGetValue(gameObject, out DibTask task)) {
                    applyUpdateTaskHandles.Remove(task.GetResetEvent());
                }
                applyUpdateTaskMap.Remove(gameObject);
            }
            if (gameObject.GetSupportedTasks().EvalFixedUpdate) {
                if (evalFixedUpdateTaskMap.TryGetValue(gameObject, out DibTask task)) {
                    evalFixedUpdateTaskHandles.Remove(task.GetResetEvent());
                }
                evalFixedUpdateTaskMap.Remove(gameObject);
            }
            if (gameObject.GetSupportedTasks().ApplyFixedUpdate) {
                if (applyFixedUpdateTaskMap.TryGetValue(gameObject, out DibTask task)) {
                    applyFixedUpdateTaskHandles.Remove(task.GetResetEvent());
                }
                applyFixedUpdateTaskMap.Remove(gameObject);
            }
            if (gameObject.GetSupportedTasks().EvalLateUpdate) {
                if (evalLateUpdateTaskMap.TryGetValue(gameObject, out DibTask task)) {
                    evalLateUpdateTaskHandles.Remove(task.GetResetEvent());
                }
                evalLateUpdateTaskMap.Remove(gameObject);
            }
            if (gameObject.GetSupportedTasks().ApplyLateUpdate) {
                if (evalLateUpdateTaskMap.TryGetValue(gameObject, out DibTask task)) {
                    evalLateUpdateTaskHandles.Remove(task.GetResetEvent());
                }
                applyLateUpdateTaskMap.Remove(gameObject);
            }
        }

        public void RemoveGameObjectInternal(IJobThreadedGameObject gameObject)
        {
            if (jobList.Contains(gameObject)) {
                jobList.Remove(gameObject);
            }
        }

        public MultithreadingScheduler RegisterGameObjectInternal(ITaskThreadedGameObject gameObject)
        {
            if (gameObject.GetSupportedTasks().EvalUpdate) {
                DibTask task = new DibTask(gameObject.KickoffEvalUpdateTask);
                evalUpdateTaskMap.Add(gameObject, task);
                evalUpdateTaskHandles.Add(task.GetResetEvent());
            }
            if (gameObject.GetSupportedTasks().ApplyUpdate) {
                DibTask task = new DibTask(gameObject.KickoffApplyUpdateTask);
                applyUpdateTaskMap.Add(gameObject, task);
                applyUpdateTaskHandles.Add(task.GetResetEvent());
            }
            if (gameObject.GetSupportedTasks().EvalFixedUpdate) {
                DibTask task = new DibTask(gameObject.KickoffEvalFixedUpdateTask);
                evalFixedUpdateTaskMap.Add(gameObject, task);
                applyUpdateTaskHandles.Add(task.GetResetEvent());
            }
            if (gameObject.GetSupportedTasks().ApplyFixedUpdate) {
                DibTask task = new DibTask(gameObject.KickoffApplyFixedUpdateTask);
                applyFixedUpdateTaskMap.Add(gameObject, task);
                applyUpdateTaskHandles.Add(task.GetResetEvent());
            }
            if (gameObject.GetSupportedTasks().EvalLateUpdate) {
                DibTask task = new DibTask(gameObject.KickoffEvalLateUpdateTask);
                evalLateUpdateTaskMap.Add(gameObject, task);
                applyUpdateTaskHandles.Add(task.GetResetEvent());
            }
            if (gameObject.GetSupportedTasks().ApplyLateUpdate) {
                DibTask task = new DibTask(gameObject.KickoffApplyLateUpdateTask);
                applyLateUpdateTaskMap.Add(gameObject, task);
                applyUpdateTaskHandles.Add(task.GetResetEvent());
            }

            return this;
        }

        public MultithreadingScheduler RegisterGameObjectInternal(IJobThreadedGameObject gameObject)
        {
            jobList.Add(gameObject);
            return this;
        }

        #endregion

        #region Physics, Frames and Coroutine handling

        public void HandleUpdate()
        {
            // Kick off EvalUpdate for each game object, wait for results, then clear the handle list
            foreach (var taskData in evalUpdateTaskMap) {
                threadTasks.Enqueue(taskData.Value);
                threadTriggerEvent.Set();
            }
            jobList.ForEach(gameObject => jobHandles.Add(gameObject.KickoffEvalUpdateJob()));

            evalUpdateTaskHandles.ForEach(taskHandle => { taskHandle.WaitOne(); taskHandle.Reset(); });
            jobHandles.ForEach(jobHandle => jobHandle.Complete());
            jobHandles.Clear();

            // Now repeat the process, but for ApplyUpdate
            foreach (var taskData in applyUpdateTaskMap) {
                threadTasks.Enqueue(taskData.Value);
                threadTriggerEvent.Set();
            }
            jobList.ForEach(gameObject => jobHandles.Add(gameObject.KickoffApplyUpdateJob()));

            applyUpdateTaskHandles.ForEach(taskHandle => { taskHandle.WaitOne(); taskHandle.Reset(); });
            jobHandles.ForEach(jobHandle => jobHandle.Complete());
            jobHandles.Clear();
        }

        public void HandleLateUpdate()
        {
            // Kick off EvalUpdate for each game object, wait for results, then clear the jobhandle list
            foreach (var taskData in evalFixedUpdateTaskMap) {
                threadTasks.Enqueue(taskData.Value);
                threadTriggerEvent.Set();
            }
            jobList.ForEach(gameObject => jobHandles.Add(gameObject.KickoffEvalFixedUpdateJob()));

            evalFixedUpdateTaskHandles.ForEach(taskHandle => { taskHandle.WaitOne(); taskHandle.Reset(); });
            jobHandles.ForEach(jobHandle => jobHandle.Complete());
            jobHandles.Clear();

            // Now repeat the process, but for ApplyUpdate
            foreach (var taskData in applyFixedUpdateTaskMap) {
                threadTasks.Enqueue(taskData.Value);
                threadTriggerEvent.Set();
            }
            jobList.ForEach(gameObject => jobHandles.Add(gameObject.KickoffApplyFixedUpdateJob()));

            applyFixedUpdateTaskHandles.ForEach(taskHandle => { taskHandle.WaitOne(); taskHandle.Reset(); });
            jobHandles.ForEach(jobHandle => jobHandle.Complete());
            jobHandles.Clear();
        }

        public void HandleFixedUpdate()
        {
            // Kick off EvalUpdate for each game object, wait for results, then clear the jobhandle list
            foreach (var taskData in evalLateUpdateTaskMap) {
                threadTasks.Enqueue(taskData.Value);
                threadTriggerEvent.Set();
            }
            jobList.ForEach(gameObject => jobHandles.Add(gameObject.KickoffEvalLateUpdateJob()));

            evalLateUpdateTaskHandles.ForEach(taskHandle => { taskHandle.WaitOne(); taskHandle.Reset(); });
            jobHandles.ForEach(jobHandle => jobHandle.Complete());
            jobHandles.Clear();

            // Now repeat the process, but for ApplyUpdate
            foreach (var taskData in applyLateUpdateTaskMap) {
                threadTasks.Enqueue(taskData.Value);
                threadTriggerEvent.Set();
            }
            jobList.ForEach(gameObject => jobHandles.Add(gameObject.KickoffApplyLateUpdateJob()));

            applyLateUpdateTaskHandles.ForEach(taskHandle => { taskHandle.WaitOne(); taskHandle.Reset(); });
            jobHandles.ForEach(jobHandle => jobHandle.Complete());
            jobHandles.Clear();
        }

        #endregion

        #region Private methods

        void ThreadExecution()
        {
            while (true) {
                threadTriggerEvent.WaitOne();
                threadTriggerEvent.Reset();
                while (threadTasks.TryDequeue(out DibTask task)) {
                    task.KickoffTask();
                    task.GetResetEvent().Set();
                }
            }
        }

        #endregion
    }
}
