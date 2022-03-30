using System;
using Unity.Jobs;

namespace DibMultithreading.Scheduler
{
    internal class MultiJob : IDisposable
    {
        public MultiJob()
        {

        }

        public void Dispose()
        {
            evalChangesJobForUpdate = null;
            applyChangesJobForUpdate = null;
            evalChangesJobForLateUpdate = null;
            applyChangesJobForLateUpdate = null;
            evalChangesJobForFixedUpdate = null;
            applyChangesJobForFixedUpdate = null;
        }

        public enum JobTypes {
            evalUpdate,
            applyUpdate,
            evalLateUpdate,
            applyLateUpdate,
            evalFixedUpdate,
            applyFixedUpdate
        };

        public void AssignJobType(JobTypes type, IJob handle)
        {
            switch (type) {
                case JobTypes.evalUpdate:
                    evalChangesJobForUpdate = handle;
                    break;
                case JobTypes.applyUpdate:
                    applyChangesJobForUpdate = handle;
                    break;
                case JobTypes.evalLateUpdate:
                    evalChangesJobForLateUpdate = handle;
                    break;
                case JobTypes.applyLateUpdate:
                    applyChangesJobForLateUpdate = handle;
                    break;
                case JobTypes.evalFixedUpdate:
                    evalChangesJobForFixedUpdate = handle;
                    break;
                case JobTypes.applyFixedUpdate:
                    applyChangesJobForFixedUpdate = handle;
                    break;
            }
        }

        private IJob evalChangesJobForUpdate;
        private IJob applyChangesJobForUpdate;

        private IJob evalChangesJobForLateUpdate;
        private IJob applyChangesJobForLateUpdate;

        private IJob evalChangesJobForFixedUpdate;
        private IJob applyChangesJobForFixedUpdate;
    }
}
