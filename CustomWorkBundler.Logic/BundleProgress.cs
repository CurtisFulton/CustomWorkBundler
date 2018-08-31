using System;
using System.Collections.Generic;

namespace CustomWorkBundler
{
    public static class BundleStage
    {
        public static string StageName { get; private set; }
        public static string StageDescription { get; private set; }

        public static event Action OnStageBegun;
        public static event Action OnStageDescriptionChanged;
        public static event Action OnStageCompleted;

        public static void BeginStage(string stageName)
        {
            if (StageName != null)
                throw new InvalidOperationException($"Cannot start the new stage '{stageName}' until the stage '{StageName}' has been completed");

            StageName = stageName;
            StageDescription = "";
            OnStageBegun?.Invoke();
        }

        public static void ClearStage()
        {
            StageName = null;
            StageDescription = null;
            OnStageBegun?.Invoke();
        }

        public static void UpdateCurrentStageDescription(string newDescription)
        {
            StageDescription = newDescription;

            OnStageDescriptionChanged?.Invoke();
        }

        public static void CompleteStage()
        {
            OnStageCompleted?.Invoke();

            StageName = null;
            StageDescription = null;

        }
    }
}
