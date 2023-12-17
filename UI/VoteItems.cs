using UnityEngine;
using RoR2.UI;
using System;
using RoR2;
using System.Reflection;
using UnityEngine.UI;
using System.Collections.Generic;

namespace VsTwitch
{
    /// <summary>
    /// Simple UI element that uses the NotificationPanel2 prefab to display item choices.
    /// <br/>
    /// FIXME: This class is too complicated and has two behaviours (simple or advanced UI) - these should be split into seperate classes.
    /// </summary>
    class VoteItems : MonoBehaviour
    {
        private static readonly int OFFSET_VERTICAL = -128;
        private static readonly int OFFSET_HORIZONTAL = 128;
        private static readonly int TEXT_HEIGHT = 24;

        private GameObject notificationGameObject;
        private GenericNotification notification;
        private float startTime;
        private float duration;

        private int voteIndex;
        private string longTermTitle;

        public void Awake()
        {
            notificationGameObject = Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/NotificationPanel2"));
            SetPosition(new Vector3(Screen.width / 2, Screen.height / 2, 0) + new Vector3(0, OFFSET_VERTICAL, 0));
            if (notificationGameObject != null)
            {
                notification = notificationGameObject.GetComponent<GenericNotification>();
                notification.transform.SetParent(RoR2Application.instance.mainCanvas.transform);
                notification.iconImage.enabled = false;
            }
            else
            {
                Log.Error("Could not load Prefabs/NotificationPanel2, object is null!");
            }

            voteIndex = 0;
            longTermTitle = "";
        }

        public void OnDestroy()
        {
            if (notificationGameObject != null)
            {
                Destroy(notificationGameObject);
            }

            this.notificationGameObject = null;
            this.notification = null;
        }

        private float GetTimeLeft()
        {
            return this.duration - (Run.instance.fixedTime - startTime);
        }

        public void Update()
        {
            float t = (Run.instance.fixedTime - startTime) / duration;
            if (notification == null || t > 1f)
            {
                Destroy(this);
                return;
            }

            notification.SetNotificationT(t);

            if (voteIndex != 0)
            {
                string secondsLeftString = "";
                if (voteIndex == 1)
                {
                    double secondsLeft = Math.Max(0, Math.Round(GetTimeLeft()));
                    secondsLeftString = $"({secondsLeft} sec)";
                }
                FieldInfo resolvedString = typeof(LanguageTextMeshController).GetField("resolvedString", BindingFlags.Instance | BindingFlags.NonPublic);
                resolvedString.SetValue(notification.titleText, $"{voteIndex}: {longTermTitle} {secondsLeftString}");
                MethodInfo UpdateLabel = typeof(LanguageTextMeshController).GetMethod("UpdateLabel", BindingFlags.Instance | BindingFlags.NonPublic);
                UpdateLabel.Invoke(notification.titleText, new object[0]);
            }
            else
            {
                double secondsLeft = Math.Max(0, Math.Round(GetTimeLeft()));
                FieldInfo resolvedString = typeof(LanguageTextMeshController).GetField("resolvedString", BindingFlags.Instance | BindingFlags.NonPublic);
                resolvedString.SetValue(notification.titleText, $"Twitch vote for one item! ({secondsLeft} sec)");
                MethodInfo UpdateLabel = typeof(LanguageTextMeshController).GetMethod("UpdateLabel", BindingFlags.Instance | BindingFlags.NonPublic);
                UpdateLabel.Invoke(notification.titleText, new object[0]);
            }
        }

        public void SetItems(List<PickupIndex> items, float duration, int voteIndex = 0)
        {
            if (notification == null)
            {
                Log.Error("Cannot set items for notification, object is null!");
                return;
            }

            this.startTime = Run.instance.fixedTime;
            this.duration = duration;

            if (voteIndex != 0)
            {
                notification.iconImage.enabled = true;
                var item = items[voteIndex - 1];
                var itemdef = PickupCatalog.GetPickupDef(item);
                if (itemdef.equipmentIndex != EquipmentIndex.None)
                {
                    notification.SetEquipment(EquipmentCatalog.GetEquipmentDef(itemdef.equipmentIndex));
                }
                else if (itemdef.artifactIndex != ArtifactIndex.None)
                {
                    notification.SetArtifact(ArtifactCatalog.GetArtifactDef(itemdef.artifactIndex));
                }
                else if (itemdef.itemIndex != ItemIndex.None)
                {
                    notification.SetItem(ItemCatalog.GetItemDef(itemdef.itemIndex));
                }
                this.voteIndex = voteIndex;

                FieldInfo resolvedString = typeof(LanguageTextMeshController).GetField("resolvedString", BindingFlags.Instance | BindingFlags.NonPublic);
                longTermTitle = (string)resolvedString.GetValue(notification.titleText);
            }
            else
            {
                for (var i = 0; i < items.Count; i++)
                {
                    GameObject icon;
                    var itemdef = PickupCatalog.GetPickupDef(items[i]);

                    if (itemdef.equipmentIndex != EquipmentIndex.None)
                    {
                        icon = CreateIcon(EquipmentCatalog.GetEquipmentDef(itemdef.equipmentIndex).pickupIconSprite);
                    }
                    else
                    {
                        icon = CreateIcon(ItemCatalog.GetItemDef(itemdef.itemIndex).pickupIconSprite);
                    }
                    icon.transform.SetParent(this.notification.transform);
                    icon.transform.position = new Vector3(Screen.width / 2, Screen.height / 2, 0) +
                        new Vector3((i * OFFSET_HORIZONTAL) - OFFSET_HORIZONTAL, OFFSET_VERTICAL + TEXT_HEIGHT, 0);
                }
                longTermTitle = "";
                this.voteIndex = 0;
            }
        }

        public void SetPosition(Vector3 position)
        {
            notificationGameObject.transform.position = position;
        }

        private static GameObject CreateIcon(Sprite pickupIcon)
        {
            GameObject gameObject = new GameObject("VoteItem_Icon");
            gameObject.AddComponent<Image>().sprite = pickupIcon;

            if (gameObject.GetComponent<CanvasRenderer>() == null)
            {
                gameObject.AddComponent<CanvasRenderer>();
            }

            if (gameObject.GetComponent<RectTransform>() == null)
            {
                gameObject.AddComponent<RectTransform>();
            }

            gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(64, 64);

            return gameObject;
        }
    }
}
