using RoR2;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;

namespace VsTwitch
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
        private int forceChargingCount;

        /// <summary>
        /// Event that fires when the event director changes states between processing events, to pausing processing events.
        /// The given argument is the current state of the director.
        /// </summary>
        public event EventHandler<bool> OnProcessingEventsChanged;

        public void Awake()
        {
            eventQueue = new BlockingCollection<Func<EventDirector, IEnumerator>>();
            previousState = false;
            forceChargingCount = 0;

            //Stage.onServerStageBegin += Stage_onServerStageBegin;
            //Stage.onServerStageComplete += Stage_onServerStageComplete;
            //TeleporterInteraction.onTeleporterFinishGlobal += TeleporterInteraction_onTeleporterFinishGlobal;
        }

        public void OnDestroy()
        {
            //Stage.onServerStageBegin -= Stage_onServerStageBegin;
            //Stage.onServerStageComplete -= Stage_onServerStageComplete;
            //TeleporterInteraction.onTeleporterFinishGlobal -= TeleporterInteraction_onTeleporterFinishGlobal;

            if (Interlocked.Exchange(ref forceChargingCount, 0) > 0)
            {
                On.RoR2.TeleporterInteraction.UpdateMonstersClear -= TeleporterInteraction_UpdateMonstersClear;
            }
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
                    Debug.LogException(e);
                }
            }

            if (!shouldProcess)
            {
                return;
            }

            if (eventQueue.TryTake(out Func<EventDirector, IEnumerator> currentEvent)) {
                try
                {
                    StartCoroutine(Wrap(currentEvent(this)));
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        /// <summary>
        /// Add event to be processed by the director
        /// </summary>
        /// <param name="eventToQueue">Function, that when called, will return an iterable that can be processed.</param>
        public void AddEvent(Func<EventDirector, IEnumerator> eventToQueue) {
            eventQueue.Add(eventToQueue);
        }

        public void ClearEvents()
        {
            while(eventQueue.TryTake(out _)) {}
        }

        private void SetTeleporterCrystals(bool enabled)
        {
            if (TeleporterInteraction.instance)
            {
                ChildLocator component = TeleporterInteraction.instance.GetComponent<ModelLocator>().modelTransform.GetComponent<ChildLocator>();
                if (component)
                {
                    if (enabled)
                    {
                        // Only enable, never disable
                        component.FindChild("TimeCrystalProps").gameObject.SetActive(true);
                    }

                    Transform transform = component.FindChild("TimeCrystalBeaconBlocker");
                    EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/TimeCrystalDeath"), new EffectData
                    {
                        origin = transform.transform.position
                    }, true);
                    transform.gameObject.SetActive(enabled);
                }
            }
        }

        /// <summary>
        /// Force the current stages teleporter to not fully charge until the returned handle is disposed.
        /// <br/>
        /// <b>Important:</b> You must ensure the returned <c>IDisposable</c> is invoked to not risk locking the player into a stage,
        /// because the teleporter is set to never fully charge.
        /// </summary>
        /// <returns>A handle to the state. Call the <c>IDisposable.Dispose()</c> when you are done.</returns>
        public IDisposable CreateOpenTeleporterObjectiveHandle()
        {
            int newValue = Interlocked.Increment(ref forceChargingCount);
                
            Debug.LogError($"EventDirector::ForceChargingState::Count = {newValue}");
            if (newValue == 1)
            {
                Debug.LogError("EventDirector::ForceChargingState::Enabled = true");
                On.RoR2.TeleporterInteraction.UpdateMonstersClear += TeleporterInteraction_UpdateMonstersClear;
                SetTeleporterCrystals(true);
            }

            return new ForceChargingHandle(this);
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

        private void TeleporterInteraction_UpdateMonstersClear(On.RoR2.TeleporterInteraction.orig_UpdateMonstersClear orig, TeleporterInteraction self)
        {
            // We don't call the original to keep monstersCleared from flapping between True and False
            // orig(self);

            FieldInfo monstersCleared = typeof(TeleporterInteraction).GetField("monstersCleared", BindingFlags.NonPublic | BindingFlags.Instance);
            monstersCleared.SetValue(self, false);
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

        private class ForceChargingHandle : IDisposable
        {
            private readonly EventDirector eventDirector;
            private int disposed;

            public ForceChargingHandle(EventDirector eventDirector)
            {
                this.eventDirector = eventDirector;
                this.disposed = 0;
            }

            public void Dispose()
            {
                int previouslyDisposed = Interlocked.Exchange(ref disposed, 1);
                if (previouslyDisposed == 1)
                {
                    return;
                }

                int newValue = Interlocked.Decrement(ref eventDirector.forceChargingCount);
                if (newValue < 0)
                {
                    Debug.LogError("Something didn't correctly ref count ForceChargingState!");
                    Debug.LogException(new Exception());
                    Interlocked.Exchange(ref eventDirector.forceChargingCount, 0);
                    newValue = 0;
                }

                Debug.LogError($"EventDirector::ForceChargingState::Count = {newValue}");
                if (newValue == 0)
                {
                    Debug.LogError("EventDirector::ForceChargingState::Enabled = false");
                    On.RoR2.TeleporterInteraction.UpdateMonstersClear -= eventDirector.TeleporterInteraction_UpdateMonstersClear;
                    eventDirector.SetTeleporterCrystals(false);
                }
            }
        }
    }
}
