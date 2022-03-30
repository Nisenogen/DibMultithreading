using System;
using System.Collections.Generic;
using UnityEngine;
using DibMultithreading.Scheduler;

namespace DibMultithreading
{
    public class Main : VTOLMOD
    {
        #region Constructor

        // This method is run once, when the Mod Loader is done initialising this game object
        public override void ModLoaded()
        {
            //This is an event the VTOLAPI calls when the game is done loading a scene
            VTOLAPI.SceneLoaded += SceneLoaded;

            // Create a single instance of the scheduler, it will persist throughout the lifetime of the program
            MultithreadingScheduler.RegisterBaseModToScheduler(this);

            Log("Dib's Multithreading loaded!");

            // Detect and report rendering mode
            UnityEngine.Rendering.RenderingThreadingMode renderThreadingMode = SystemInfo.renderingThreadingMode;
            Dictionary<UnityEngine.Rendering.RenderingThreadingMode, string> logReportStringSelection = new Dictionary<UnityEngine.Rendering.RenderingThreadingMode, string> {
                { UnityEngine.Rendering.RenderingThreadingMode.Direct, "Direct" }, // Terrible (CPU rendering)
                { UnityEngine.Rendering.RenderingThreadingMode.SingleThreaded, "SingleThreaded" }, // Blah (Main thread creating and dispatching rendering jobs)
                { UnityEngine.Rendering.RenderingThreadingMode.MultiThreaded, "MultiThreaded" }, // Slightly better but still blah (Main thread creating jobs, worker thread dispatching and scheduling them)
                { UnityEngine.Rendering.RenderingThreadingMode.LegacyJobified, "LegacyJobified" }, // Deprecated
                { UnityEngine.Rendering.RenderingThreadingMode.NativeGraphicsJobs, "NativeGraphicsJobs" }, // Deprecated
                { UnityEngine.Rendering.RenderingThreadingMode.NativeGraphicsJobsWithoutRenderThread, "NativeGraphicsJobsWithoutRenderThread" } // Best if available (Main thread creating and scheduling jobs, many workers dispatching)
            };
            if (logReportStringSelection.TryGetValue(renderThreadingMode, out string renderThreadingModeString)) {
                string renderLogReport = "DMT: Rendering threading mode was set by base game to: " + renderThreadingModeString;
                Log(renderLogReport);
            }
            else {
                Log("DMT: Could not detect render threading mode");
            }

            string processorThreadLog = "DMT: " + Environment.ProcessorCount + "detected logical threads";
            Log(processorThreadLog);

            base.ModLoaded();
        }

        #endregion

        #region Data

        private readonly MultithreadingScheduler scheduler = new MultithreadingScheduler();

        #endregion

        #region Game object registration

        public MultithreadingScheduler RegisterGameObjectToScheduler(ITaskThreadedGameObject gameObject)
        {
            return scheduler.RegisterGameObjectInternal(gameObject);
        }

        public MultithreadingScheduler RegisterGameObjectToScheduler(IJobThreadedGameObject gameObject)
        {
            return scheduler.RegisterGameObjectInternal(gameObject);
        }

        public void RemoveGameObjectFromScheduler(ITaskThreadedGameObject gameObject)
        {
            scheduler.RemoveGameObjectInternal(gameObject);
        }

        public void RemoveGameObjectFromScheduler(IJobThreadedGameObject gameObject)
        {
            scheduler.RemoveGameObjectInternal(gameObject);
        }

        #endregion

        #region Physics, Frames and Coroutine handling
        // Pass this work on to the scheduler

        //This method is called every frame by Unity. Here you'll probably put most of your code
        void Update()
        {
            scheduler.HandleUpdate();
        }

        //This method is like update but it's framerate independent. This means it gets called at a set time interval instead of every frame. This is useful for physics calculations
        void FixedUpdate()
        {
            scheduler.HandleFixedUpdate();
        }

        // This is called every frame by Unity, except after the frame is completed. 
        void LateUpdate()
        {
            scheduler.HandleLateUpdate();
        }

        #endregion

        #region Scene transitions

        //This function is called every time a scene is loaded. this behaviour is defined in Awake().
        private void SceneLoaded(VTOLScenes scene)
        {
            //If you want something to happen in only one (or more) scenes, this is where you define it.

            //For example, lets say you're making a mod which only does something in the ready room and the loading scene. This is how your code could look:
            switch (scene)
            {
                case VTOLScenes.ReadyRoom:
                    //Add your ready room code here
                    break;
                case VTOLScenes.LoadingScene:
                    //Add your loading scene code here
                    break;
            }
        }

        #endregion
    }
}