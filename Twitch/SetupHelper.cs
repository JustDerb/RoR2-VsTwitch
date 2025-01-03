using RoR2;
using RoR2.UI;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using VsTwitch.Twitch.Auth;

namespace VsTwitch.Twitch
{
    internal class SetupHelper : MonoBehaviour
    {
        private static readonly string COMMON_YES = "DIALOG_OPTION_YES";
        private static readonly string COMMON_NO = "DIALOG_OPTION_NO";
        private static readonly string HEADER_DIALOG_TWITCH_SETUP = "VSTWITCH_HEADER_DIALOG_TWITCH_SETUP";
        private static readonly string DESCRIPTION_DIALOG_TWITCH_SETUP = "VSTWITCH_DESCRIPTION_DIALOG_TWITCH_SETUP";
        private static readonly string DESCRIPTION_DIALOG_TWITCH_SETUP_WAITING = "VSTWITCH_DESCRIPTION_DIALOG_TWITCH_SETUP_WAITING";
        private static readonly string DESCRIPTION_DIALOG_TWITCH_SETUP_FINISHED = "VSTWITCH_DESCRIPTION_DIALOG_TWITCH_SETUP_FINISHED";
        private static readonly string DESCRIPTION_DIALOG_WALKTHROUGH_CONFIG_ASK = "VSTWITCH_DESCRIPTION_DIALOG_WALKTHROUGH_CONFIG_ASK";
        private static readonly string DESCRIPTION_DIALOG_WALKTHROUGH_ENABLE_ITEM_VOTING = "VSTWITCH_DESCRIPTION_DIALOG_WALKTHROUGH_CONFIG_ENABLE_ITEM_VOTING";
        private static readonly string DESCRIPTION_DIALOG_WALKTHROUGH_ENABLE_BIT_EVENTS = "VSTWITCH_DESCRIPTION_DIALOG_WALKTHROUGH_CONFIG_ENABLE_BIT_EVENTS";
        private static readonly string DESCRIPTION_DIALOG_WALKTHROUGH_ENABLE_BIT_EVENTS_YES = "VSTWITCH_DESCRIPTION_DIALOG_WALKTHROUGH_CONFIG_ENABLE_BIT_EVENTS_YES";
        private static readonly string DESCRIPTION_DIALOG_WALKTHROUGH_ENABLE_CHANNEL_POINTS = "VSTWITCH_DESCRIPTION_DIALOG_WALKTHROUGH_CONFIG_ENABLE_CHANNEL_POINTS";
        private static readonly string DESCRIPTION_DIALOG_WALKTHROUGH_ENABLE_CHANNEL_POINTS_YES = "VSTWITCH_DESCRIPTION_DIALOG_WALKTHROUGH_CONFIG_ENABLE_CHANNEL_POINTS_YES";
        private static readonly string DESCRIPTION_DIALOG_WALKTHROUGH_ENABLE_CHANNEL_POINTS_NO = "VSTWITCH_DESCRIPTION_DIALOG_WALKTHROUGH_CONFIG_ENABLE_CHANNEL_POINTS_NO";

        /// <summary>
        /// Event Queue that essentially maintains a queue that creates a progressive series of dialog boxes
        /// </summary>
        private BlockingCollection<Func<IEnumerator>> eventQueue;

        public void Awake()
        {
            eventQueue = new BlockingCollection<Func<IEnumerator>>();
        }

        public void Update()
        {
            // We only want to run events when we know we can show a Dialog Box
            if (SimpleDialogBox.instancesList.Count > 0)
            {
                return;
            }

            if (eventQueue.TryTake(out Func<IEnumerator> currentEvent))
            {
                try
                {
                    StartCoroutine(currentEvent());
                }
                catch (Exception e)
                {
                    Log.Exception(e);
                }
            }
        }

        public Task GuideThroughTwitchIntegration(AuthManager authManager, Configuration config)
        {
            CountdownEvent countdown = new CountdownEvent(1);
            eventQueue.Add(() => GuideThroughTwitchIntegration(countdown, authManager, config));
            return Task.Factory.StartNew(delegate
            {
                // This countdown objects acts a latch to allow us to wait for the whole process to finish
                countdown.Wait();
                countdown.Dispose();

                // Double check things were okay
                if (!authManager.IsAuthed())
                {
                    throw new InvalidOperationException("Finished Twitch setup but not authed");
                }
            });
        }

        private IEnumerator GuideThroughTwitchIntegration(CountdownEvent countdown, AuthManager authManager, Configuration config)
        {
            DialogBoxManager.DialogBox(
                HEADER_DIALOG_TWITCH_SETUP,
                DESCRIPTION_DIALOG_TWITCH_SETUP,
                delegate
                {
                    eventQueue.Add(() => TwitchIntegrationTryAuth(countdown, authManager, config));
                },
                CommonLanguageTokens.ok);
            yield break;
        }

        private IEnumerator TwitchIntegrationTryAuth(CountdownEvent countdown, AuthManager authManager, Configuration config)
        {
            // TODO: Add a cancel button to this - right now, they must complete the flow
            //using var cancel = new CancellationTokenSource();
            SimpleDialogBox waitingDialog = SimpleDialogBox.Create();
            waitingDialog.headerToken = new SimpleDialogBox.TokenParamsPair(HEADER_DIALOG_TWITCH_SETUP);
            waitingDialog.descriptionToken = new SimpleDialogBox.TokenParamsPair(DESCRIPTION_DIALOG_TWITCH_SETUP_WAITING);
            waitingDialog.rootObject.transform.SetParent(RoR2Application.instance.mainCanvas.transform);

            authManager.MaybeAuthUser().ContinueWith((t) =>
            {
                Destroy(waitingDialog.rootObject);
                if (t.Exception != null)
                {
                    eventQueue.Add(() => TwitchIntegrationFinish(countdown, $"Error setting up Twitch: {t.Exception.GetBaseException().Message}"));
                    Log.Exception(t.Exception);
                    return;
                }

                eventQueue.Add(() => TwitchIntegrationAskForConfigSetup(countdown, config));
            });
            yield break;
        }

        private IEnumerator TwitchIntegrationAskForConfigSetup(CountdownEvent countdown, Configuration config)
        {
            DialogBoxManager.DialogBox(
                HEADER_DIALOG_TWITCH_SETUP,
                DESCRIPTION_DIALOG_WALKTHROUGH_CONFIG_ASK,
                delegate
                {
                    eventQueue.Add(() => TwitchIntegrationConfigSetupEnableItemVoting(countdown, config));
                },
                COMMON_YES
            ).AddActionButton(
                delegate
                {
                    eventQueue.Add(() => TwitchIntegrationFinish(countdown, DESCRIPTION_DIALOG_TWITCH_SETUP_FINISHED));
                },
                COMMON_NO
            );
            yield break;
        }

        private IEnumerator TwitchIntegrationConfigSetupEnableItemVoting(CountdownEvent countdown, Configuration config)
        {
            DialogBoxManager.DialogBox(
                HEADER_DIALOG_TWITCH_SETUP,
                DESCRIPTION_DIALOG_WALKTHROUGH_ENABLE_ITEM_VOTING,
                delegate
                {
                    config.EnableItemVoting.Value = true;
                    eventQueue.Add(() => TwitchIntegrationConfigSetupEnableBitEvents(countdown, config));
                },
                COMMON_YES
            ).AddActionButton(
                delegate
                {
                    config.EnableItemVoting.Value = false;
                    eventQueue.Add(() => TwitchIntegrationConfigSetupEnableBitEvents(countdown, config));
                },
                COMMON_NO
            );
            yield break;
        }

        private IEnumerator TwitchIntegrationConfigSetupEnableBitEvents(CountdownEvent countdown, Configuration config)
        {
            DialogBoxManager.DialogBox(
                HEADER_DIALOG_TWITCH_SETUP,
                DESCRIPTION_DIALOG_WALKTHROUGH_ENABLE_BIT_EVENTS,
                delegate
                {
                    config.EnableBitEvents.Value = true;
                    eventQueue.Add(() => TwitchIntegrationConfigSetupEnableBitEventsYes(countdown, config));
                },
                COMMON_YES
            ).AddActionButton(
                delegate
                {
                    config.EnableBitEvents.Value = false;
                    eventQueue.Add(() => TwitchIntegrationConfigSetupEnableChannelPoints(countdown));
                },
                COMMON_NO
            );
            yield break;
        }

        private IEnumerator TwitchIntegrationConfigSetupEnableBitEventsYes(CountdownEvent countdown, Configuration config)
        {
            DialogBoxManager.DialogBox(
                new SimpleDialogBox.TokenParamsPair(HEADER_DIALOG_TWITCH_SETUP),
                new SimpleDialogBox.TokenParamsPair(DESCRIPTION_DIALOG_WALKTHROUGH_ENABLE_BIT_EVENTS_YES, config.BitsThreshold),
                delegate
                {
                    eventQueue.Add(() => TwitchIntegrationConfigSetupEnableChannelPoints(countdown));
                },
                CommonLanguageTokens.ok
            );
            yield break;
        }

        private IEnumerator TwitchIntegrationConfigSetupEnableChannelPoints(CountdownEvent countdown)
        {
            DialogBoxManager.DialogBox(
                HEADER_DIALOG_TWITCH_SETUP,
                DESCRIPTION_DIALOG_WALKTHROUGH_ENABLE_CHANNEL_POINTS,
                delegate
                {
                    eventQueue.Add(() => TwitchIntegrationConfigSetupEnableChannelPointsYesNo(countdown, DESCRIPTION_DIALOG_WALKTHROUGH_ENABLE_CHANNEL_POINTS_YES));
                },
                COMMON_YES
            ).AddActionButton(
                delegate
                {
                    eventQueue.Add(() => TwitchIntegrationConfigSetupEnableChannelPointsYesNo(countdown, DESCRIPTION_DIALOG_WALKTHROUGH_ENABLE_CHANNEL_POINTS_NO));
                },
                COMMON_NO
            );
            yield break;
        }

        private IEnumerator TwitchIntegrationConfigSetupEnableChannelPointsYesNo(CountdownEvent countdown, string descriptionToken)
        {
            DialogBoxManager.DialogBox(
                HEADER_DIALOG_TWITCH_SETUP,
                descriptionToken,
                delegate
                {
                    eventQueue.Add(() => TwitchIntegrationFinish(countdown, DESCRIPTION_DIALOG_TWITCH_SETUP_FINISHED));
                },
                CommonLanguageTokens.ok
            );
            yield break;
        }

        private IEnumerator TwitchIntegrationFinish(CountdownEvent countdown, string descriptionToken)
        {
            DialogBoxManager.DialogBox(
                HEADER_DIALOG_TWITCH_SETUP,
                descriptionToken,
                delegate
                {
                    countdown?.Signal();
                },
                CommonLanguageTokens.ok
            );
            yield break;
        }
    }
}
