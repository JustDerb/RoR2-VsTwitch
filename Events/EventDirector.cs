using RoR2;
using System;
using System.Collections;
using System.Collections.Concurrent;
using UnityEngine;
using UnityEngine.Networking;

namespace VsTwitch.Events
{
    /// <summary>
    /// The Event Director handles async events and ensures that they fire only during active stage runs.
    /// The director will automatically wrap events and ensure they do not execute during stage transitions.
    /// <br/>
    /// <b>Responsibility:</b> Coordinate events so they execute only during meaningful parts of a run.
    /// </summary>
    class EventDirector : MonoBehaviour
    {
        private BlockingCollection<Func<EventDirector, IEnumerator>> eventQueue;
        private bool previousState;

        /// <summary>
        /// Event that fires when the event director changes states between processing events, to pausing processing events.
        /// The given argument is the current state of the director.
        /// </summary>
        public event EventHandler<bool> OnProcessingEventsChanged;

        public void Awake()
        {
            eventQueue = new BlockingCollection<Func<EventDirector, IEnumerator>>();
            previousState = false;

            //Stage.onServerStageBegin += Stage_onServerStageBegin;
            //Stage.onServerStageComplete += Stage_onServerStageComplete;
            //TeleporterInteraction.onTeleporterFinishGlobal += TeleporterInteraction_onTeleporterFinishGlobal;
        }

        public void OnDestroy()
        {
            //Stage.onServerStageBegin -= Stage_onServerStageBegin;
            //Stage.onServerStageComplete -= Stage_onServerStageComplete;
            //TeleporterInteraction.onTeleporterFinishGlobal -= TeleporterInteraction_onTeleporterFinishGlobal;
        }

        public void Update()
        {
            bool shouldProcess = ShouldProcessEvents();
            if (shouldProcess != previousState)
            {
                previousState = shouldProcess;
                try
                {
                    OnProcessingEventsChanged?.Invoke(this, shouldProcess);
                }
                catch (Exception e)
                {
                    Log.Exception(e);
                }
            }

            if (!shouldProcess)
            {
                return;
            }

            if (eventQueue.TryTake(out Func<EventDirector, IEnumerator> currentEvent))
            {
                try
                {
                    StartCoroutine(Wrap(currentEvent(this)));
                }
                catch (Exception e)
                {
                    Log.Exception(e);
                }
            }
        }

        /// <summary>
        /// Add event to be processed by the director
        /// </summary>
        /// <param name="eventToQueue">Function, that when called, will return an iterable that can be processed.</param>
        public void AddEvent(Func<EventDirector, IEnumerator> eventToQueue)
        {
            eventQueue.Add(eventToQueue);
        }

        public void ClearEvents()
        {
            while (eventQueue.TryTake(out _)) { }
        }

        private IEnumerator Wrap(IEnumerator coroutine)
        {
            while (true)
            {
                // Wait until we can process events
                while (!ShouldProcessEvents())
                {
                    yield return null;
                }

                if (!coroutine.MoveNext())
                {
                    yield break;
                }
                yield return coroutine.Current;
            }
        }

        private bool ShouldProcessEvents()
        {
            if (!enabled)
            {
                return false;
            }

            // We need to actively be the Server!
            if (!NetworkServer.active)
            {
                return false;
            }

            // If there isn't a Run active, or we hit Game Over
            if (!Run.instance || Run.instance.isGameOverServer)
            {
                return false;
            }

            // If we aren't on a Stage, or the Stage is completed
            if (!Stage.instance || Stage.instance.completed)
            {
                return false;
            }

            // If we are on the GameOver screen, don't run any events
            if (GameOverController.instance)
            {
                return false;
            }

            // Ensure the Stage has started and a certain amount of seconds has elapsed
            if (Stage.instance.entryTime.timeSince < 6)
            {
                return false;
            }

            // We are exiting the scene, so don't do events
            if (SceneExitController.isRunning)
            {
                return false;
            }

            // Players are spawned; now check if the teleporter is charged and finished
            if (TeleporterInteraction.instance && TeleporterInteraction.instance.isInFinalSequence)
            {
                return false;
            }

            return true;
        }
    }
}
