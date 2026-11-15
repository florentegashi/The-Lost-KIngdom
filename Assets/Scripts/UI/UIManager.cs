using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
namespace LostKingdom
{
    public class UIManager : MonoBehaviour
    {
        public GameObject menu, hud, pause, complete, settings;
        public TMP_Text fragmentText, crystalText, objectiveText, promptText, notificationText;
        public TMP_Text poisonText;
        TMP_Text deathHeading;
        float notificationUntil;
        GameObject deathPanel;
        GameObject tutorialPanel;
        TMP_Text tutorialTitle, tutorialCaption;
        readonly System.Collections.Generic.List<GameObject> tutorialIllustrations = new System.Collections.Generic.List<GameObject>();
        int tutorialStep;
        void Start() { AudioListener.volume = PlayerPrefs.GetFloat("Volume", .65f); }
        void Update()
        {
            if (notificationText && Time.time > notificationUntil) notificationText.text = "";
            UpdateTutorial();
        }
        public void ShowMenu() { menu.SetActive(true); hud.SetActive(false); pause.SetActive(false); complete.SetActive(false); settings.SetActive(false); }
        public void ShowHUD()
        {
            menu.SetActive(false); hud.SetActive(true); settings.SetActive(false); UpdateCounters(0, 0);
            SetObjective("Recover five light fragments. Climb the ravine ledges and avoid the glowing mushrooms.");
            BuildTutorial(); tutorialStep = 0; PresentTutorial();
        }
        public void ShowPause(bool show) { pause.SetActive(show); }
        public void OpenSettings() { menu.SetActive(false); settings.SetActive(true); }
        public void CloseSettings() { PlayerPrefs.Save(); settings.SetActive(false); menu.SetActive(true); }
        public void ShowComplete() { hud.SetActive(false); complete.SetActive(true); }
        public void ShowDeath()
            => ShowDeath(false);
        public void ShowDeath(bool poisoned)
        {
            hud.SetActive(false); menu.SetActive(false); pause.SetActive(false); complete.SetActive(false); settings.SetActive(false);
            if (!deathPanel)
            {
                deathPanel = new GameObject("Game Over", typeof(RectTransform), typeof(Image)); deathPanel.transform.SetParent(transform, false);
                var rect = deathPanel.GetComponent<RectTransform>(); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
                deathPanel.GetComponent<Image>().color = new Color(.015f, .025f, .035f, .93f);
                deathHeading = MakeText("YOU DIED / GAME OVER", 48, new Vector2(0, 65)); deathHeading.transform.SetParent(deathPanel.transform, false);
                var buttonObject = new GameObject("Restart Level", typeof(RectTransform), typeof(Image), typeof(Button)); buttonObject.transform.SetParent(deathPanel.transform, false);
                var buttonRect = buttonObject.GetComponent<RectTransform>(); buttonRect.sizeDelta = new Vector2(320, 62); buttonRect.anchoredPosition = new Vector2(0, -35);
                buttonObject.GetComponent<Image>().color = new Color(.14f, .34f, .37f);
                buttonObject.GetComponent<Button>().onClick.AddListener(GameManager.Instance.Restart);
                var label = MakeText("Restart Level", 25, Vector2.zero); label.transform.SetParent(buttonObject.transform, false);
            }
            if (!deathHeading) deathHeading = deathPanel.GetComponentInChildren<TMP_Text>();
            deathHeading.text = poisoned ? "POISONED!\nYOU DIED / GAME OVER" : "YOU DIED / GAME OVER";
            deathPanel.SetActive(true);
        }
        public void SetupPoisonCounter()
        {
            if (poisonText) return;
            var panel = hud.transform.Find("Journey");
            var obj = new GameObject("Poison counter", typeof(RectTransform), typeof(TextMeshProUGUI));
            obj.transform.SetParent(panel ? panel : hud.transform, false);
            var rect = obj.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f); rect.sizeDelta = new Vector2(350, 30); rect.anchoredPosition = new Vector2(0, -64);
            poisonText = obj.GetComponent<TMP_Text>(); poisonText.font = fragmentText.font; poisonText.fontSize = 19; poisonText.fontStyle = FontStyles.Bold;
            poisonText.color = SceneManager.GetActiveScene().name == "MysticForest"
                ? new Color(1f, .42f, .68f) : new Color(.83f, .58f, 1f);
            poisonText.alignment = TextAlignmentOptions.Left; poisonText.raycastTarget = false;
        }
        public void SetPoison(int count, int maximum)
        {
            if (!poisonText) SetupPoisonCounter();
            if (poisonText)
            {
                string label = SceneManager.GetActiveScene().name == "MysticForest" ? "POISON MUSHROOMS: " : "CORRUPTION: ";
                poisonText.text = label + count + " / " + maximum;
            }
        }
        TMP_Text MakeText(string value, int size, Vector2 position)
        {
            var obj = new GameObject(value, typeof(RectTransform), typeof(TextMeshProUGUI));
            var rect = obj.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f); rect.sizeDelta = new Vector2(700, 90); rect.anchoredPosition = position;
            var label = obj.GetComponent<TMP_Text>(); label.font = objectiveText ? objectiveText.font : null; label.text = value; label.fontSize = size; label.color = new Color(.91f, .88f, .72f); label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false; return label;
        }
        public void UpdateCounters(int fragments, int crystals) { fragmentText.text = "Light Fragments: " + fragments + "/5"; crystalText.text = "Crystal of Light: " + crystals + "/1"; }
        public void SetObjective(string value) { objectiveText.text = value; }
        public void SetPrompt(string value) { promptText.text = value; }
        public void Notify(string value, float seconds = 5) { notificationText.text = value; notificationUntil = Time.time + seconds; }

        void BuildTutorial()
        {
            if (tutorialPanel) return;
            tutorialPanel = new GameObject("Illustrated forest guide", typeof(RectTransform), typeof(Image));
            tutorialPanel.transform.SetParent(hud.transform, false);
            var rect = tutorialPanel.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(1, 0); rect.pivot = new Vector2(1, 0); rect.sizeDelta = new Vector2(310, 126); rect.anchoredPosition = new Vector2(-28, 105);
            tutorialPanel.GetComponent<Image>().color = new Color(.018f, .055f, .065f, .86f);
            var border = tutorialPanel.AddComponent<Outline>(); border.effectColor = new Color(.24f, .72f, .64f, .75f); border.effectDistance = new Vector2(1, -1);
            tutorialTitle = MakeTutorialText("FOREST FIELD GUIDE", 14, new Vector2(18, -19), new Vector2(276, 24));
            tutorialTitle.transform.SetParent(tutorialPanel.transform, false); tutorialTitle.alignment = TextAlignmentOptions.Left; tutorialTitle.color = new Color(.53f, .87f, .75f);
            var illustrationArea = new GameObject("Control illustrations", typeof(RectTransform));
            illustrationArea.transform.SetParent(tutorialPanel.transform, false);
            var areaRect = illustrationArea.GetComponent<RectTransform>(); areaRect.anchorMin = areaRect.anchorMax = new Vector2(0, 1); areaRect.pivot = new Vector2(0, 1); areaRect.sizeDelta = new Vector2(276, 54); areaRect.anchoredPosition = new Vector2(18, -46);
            tutorialIllustrations.Add(CreateMovementIllustration(illustrationArea.transform));
            tutorialIllustrations.Add(CreateSpaceIllustration(illustrationArea.transform));
            tutorialIllustrations.Add(CreateMouseIllustration(illustrationArea.transform));
            tutorialIllustrations.Add(CreateCrystalIllustration(illustrationArea.transform));
            tutorialCaption = MakeTutorialText("", 13, new Vector2(18, -99), new Vector2(276, 22));
            tutorialCaption.transform.SetParent(tutorialPanel.transform, false); tutorialCaption.alignment = TextAlignmentOptions.Left; tutorialCaption.color = new Color(.78f, .82f, .73f);
        }
        TMP_Text MakeTutorialText(string value, int size, Vector2 position, Vector2 dimensions)
        {
            var obj = new GameObject("Guide illustration", typeof(RectTransform), typeof(TextMeshProUGUI));
            var rect = obj.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1); rect.sizeDelta = dimensions; rect.anchoredPosition = position;
            var label = obj.GetComponent<TMP_Text>(); label.font = objectiveText ? objectiveText.font : null; label.fontSize = size; label.text = value; label.color = new Color(.95f, .9f, .72f); label.raycastTarget = false; label.textWrappingMode = TextWrappingModes.NoWrap;
            return label;
        }
        void PresentTutorial()
        {
            if (!tutorialPanel) return;
            string[] captions = { "Walk the lantern path", "Leap across the ravine stones", "Look around the forest", "Approach a light and interact" };
            for (int i = 0; i < tutorialIllustrations.Count; i++) tutorialIllustrations[i].SetActive(i == tutorialStep);
            tutorialCaption.text = captions[Mathf.Clamp(tutorialStep, 0, 3)];
            tutorialPanel.SetActive(tutorialStep < 4);
        }
        GameObject CreateMovementIllustration(Transform parent)
        {
            var group = CreateIllustrationGroup(parent, "WASD keyboard");
            CreateKeycap(group.transform, "W", new Vector2(78, -1), new Vector2(28, 23));
            CreateKeycap(group.transform, "A", new Vector2(47, -27), new Vector2(28, 23));
            CreateKeycap(group.transform, "S", new Vector2(78, -27), new Vector2(28, 23));
            CreateKeycap(group.transform, "D", new Vector2(109, -27), new Vector2(28, 23));
            CreateIllustrationLabel(group.transform, "MOVE", new Vector2(158, -18), new Vector2(80, 26), 17, new Color(.64f, .9f, .76f));
            return group;
        }
        GameObject CreateSpaceIllustration(Transform parent)
        {
            var group = CreateIllustrationGroup(parent, "Jumping key");
            CreateKeycap(group.transform, "SPACE", new Vector2(35, -12), new Vector2(118, 28));
            CreateIllustrationLabel(group.transform, "JUMP  ↑", new Vector2(166, -15), new Vector2(93, 26), 16, new Color(.64f, .9f, .76f));
            return group;
        }
        GameObject CreateMouseIllustration(Transform parent)
        {
            var group = CreateIllustrationGroup(parent, "Mouse camera diagram");
            var mouse = new GameObject("Mouse body", typeof(RectTransform), typeof(Image)); mouse.transform.SetParent(group.transform, false);
            var rect = mouse.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1); rect.sizeDelta = new Vector2(35, 46); rect.anchoredPosition = new Vector2(72, -1);
            mouse.GetComponent<Image>().color = new Color(.18f, .31f, .32f, 1);
            CreateKeycap(mouse.transform, "L", new Vector2(3, -3), new Vector2(13, 17));
            CreateKeycap(mouse.transform, "R", new Vector2(19, -3), new Vector2(13, 17));
            CreateIllustrationLabel(group.transform, "LOOK  ↕ ↔", new Vector2(126, -14), new Vector2(135, 28), 16, new Color(.64f, .9f, .76f));
            return group;
        }
        GameObject CreateCrystalIllustration(Transform parent)
        {
            var group = CreateIllustrationGroup(parent, "Crystal and interact key");
            var gem = new GameObject("Illustrated crystal", typeof(RectTransform), typeof(Image)); gem.transform.SetParent(group.transform, false);
            var gemRect = gem.GetComponent<RectTransform>(); gemRect.anchorMin = gemRect.anchorMax = new Vector2(0, 1); gemRect.pivot = new Vector2(.5f, .5f); gemRect.sizeDelta = new Vector2(24, 34); gemRect.anchoredPosition = new Vector2(55, -23); gemRect.localRotation = Quaternion.Euler(0, 0, 12);
            gem.GetComponent<Image>().color = new Color(.3f, .84f, 1f, 1);
            CreateIllustrationLabel(group.transform, "LIGHT", new Vector2(77, -14), new Vector2(90, 26), 16, new Color(.7f, .88f, 1f));
            CreateKeycap(group.transform, "E", new Vector2(162, -12), new Vector2(34, 28));
            CreateIllustrationLabel(group.transform, "INTERACT", new Vector2(202, -15), new Vector2(70, 26), 12, new Color(.64f, .9f, .76f));
            return group;
        }
        GameObject CreateIllustrationGroup(Transform parent, string name)
        {
            var group = new GameObject(name, typeof(RectTransform)); group.transform.SetParent(parent, false);
            var rect = group.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1); rect.sizeDelta = new Vector2(276, 54);
            group.SetActive(false); return group;
        }
        void CreateKeycap(Transform parent, string value, Vector2 position, Vector2 size)
        {
            var key = new GameObject(value + " key", typeof(RectTransform), typeof(Image)); key.transform.SetParent(parent, false);
            var rect = key.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1); rect.sizeDelta = size; rect.anchoredPosition = position;
            key.GetComponent<Image>().color = new Color(.16f, .3f, .31f, 1);
            CreateIllustrationLabel(key.transform, value, Vector2.zero, size, value.Length > 2 ? 12 : 14, new Color(.95f, .91f, .75f)).alignment = TextAlignmentOptions.Center;
        }
        TMP_Text CreateIllustrationLabel(Transform parent, string value, Vector2 position, Vector2 size, int fontSize, Color color)
        {
            var obj = new GameObject(value, typeof(RectTransform), typeof(TextMeshProUGUI)); obj.transform.SetParent(parent, false);
            var rect = obj.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1); rect.sizeDelta = size; rect.anchoredPosition = position;
            var label = obj.GetComponent<TMP_Text>(); label.font = objectiveText ? objectiveText.font : null; label.text = value; label.fontSize = fontSize; label.color = color; label.raycastTarget = false; label.textWrappingMode = TextWrappingModes.NoWrap; label.alignment = TextAlignmentOptions.Left;
            return label;
        }
        void UpdateTutorial()
        {
            if (!tutorialPanel || !tutorialPanel.activeSelf || GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;
            bool progressed = tutorialStep switch
            {
                0 => DemoInput.Move.sqrMagnitude > .1f,
                1 => Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame,
                2 => DemoInput.Look.sqrMagnitude > 4,
                _ => Keyboard.current != null && (Keyboard.current.eKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame)
            };
            if (progressed) { tutorialStep++; PresentTutorial(); }
        }
    }
}
