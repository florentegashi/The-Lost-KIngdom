using TMPro;
using UnityEngine;
using UnityEngine.UI;
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
        void Start() { AudioListener.volume = PlayerPrefs.GetFloat("Volume", .65f); }
        void Update()
        {
            if (Time.time > notificationUntil) notificationText.text = "";
        }
        public void ShowMenu() { menu.SetActive(true); hud.SetActive(false); pause.SetActive(false); complete.SetActive(false); settings.SetActive(false); }
        public void ShowHUD() { menu.SetActive(false); hud.SetActive(true); settings.SetActive(false); UpdateCounters(0, 0); SetObjective("Recover five light fragments. Climb the ravine ledges and avoid the glowing mushrooms."); }
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
            poisonText.color = new Color(.83f, .58f, 1f); poisonText.alignment = TextAlignmentOptions.Left; poisonText.raycastTarget = false;
        }
        public void SetPoison(int count, int maximum)
        {
            if (!poisonText) SetupPoisonCounter();
            if (poisonText) poisonText.text = "CORRUPTION: " + count + " / " + maximum;
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
    }
}
