using Unity.Jobs;

namespace DibMultithreading.Scheduler
{
    public struct AvailableTaskResolver
    {
        // Implement this somewhere in your class to tell the scheduler which methods you've actually
        // implemented. That way we can skip scheduling calls to methods that do nothing. 
        public AvailableTaskResolver(
            bool evalUpdateSetting,
            bool applyUpdateSetting,
            bool evalFixedUpdateSetting,
            bool applyFixedUpdateSetting,
            bool evalLateUpdateSetting,
            bool applyLateUpdateSetting)
        {
            evalUpdate = evalUpdateSetting;
            applyUpdate = applyUpdateSetting;
            evalFixedUpdate = evalFixedUpdateSetting;
            applyFixedUpdate = applyFixedUpdateSetting;
            evalLateUpdate = evalLateUpdateSetting;
            applyLateUpdate = applyLateUpdateSetting;
        }
        private readonly bool evalUpdate;
        public bool EvalUpdate { get { return evalUpdate; } }
        private readonly bool applyUpdate;
        public bool ApplyUpdate { get { return applyUpdate; } }
        private readonly bool evalFixedUpdate;
        public bool EvalFixedUpdate { get { return evalFixedUpdate; } }
        private readonly bool applyFixedUpdate;
        public bool ApplyFixedUpdate { get { return applyFixedUpdate; } }
        private readonly bool evalLateUpdate;
        public bool EvalLateUpdate { get { return evalLateUpdate; } }
        private readonly bool applyLateUpdate;
        public bool ApplyLateUpdate { get { return applyLateUpdate; } }
    }

    // I wish we had access to C# 8.0 features, because I could make default implementations do nothing
    // and then we wouldn't have to implement them all on every class...
    public interface ITaskThreadedGameObject {
        void KickoffEvalUpdateTask();
        void KickoffApplyUpdateTask();
        void KickoffEvalLateUpdateTask();
        void KickoffApplyLateUpdateTask();
        void KickoffEvalFixedUpdateTask();
        void KickoffApplyFixedUpdateTask();
        AvailableTaskResolver GetSupportedTasks();
    }

    // I wish we had access to C# 8.0 features, because I could make default implementations do nothing
    // and then we wouldn't have to implement them all on every class...
    public interface IJobThreadedGameObject
    {
        JobHandle KickoffEvalUpdateJob();
        JobHandle KickoffApplyUpdateJob();
        JobHandle KickoffEvalLateUpdateJob();
        JobHandle KickoffApplyLateUpdateJob();
        JobHandle KickoffEvalFixedUpdateJob();
        JobHandle KickoffApplyFixedUpdateJob();
    }
}
