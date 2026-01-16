using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Ratings
{
    internal class UI
    {
        private GameObject _ratingsMenu;
        private GameObject _ratingsBG;
        private readonly Plugin _ratings;
        private readonly ExtensionButton _extensionBtn = new ExtensionButton();
        private List<TMP_InputField> _values = new List<TMP_InputField>();

        public UI(Plugin ratings)
        {
            this._ratings = ratings;

            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Ratings.Icon.png");
            byte[] data = new byte[stream.Length];
            stream.Read(data, 0, (int)stream.Length);

            Texture2D texture2D = new Texture2D(256, 256);
            texture2D.LoadImage(data);

            _extensionBtn.Icon = Sprite.Create(texture2D, new Rect(0, 0, texture2D.width, texture2D.height), new Vector2(0, 0), 100.0f);
            _extensionBtn.Tooltip = "Ratings";
            ExtensionButtons.AddButton(_extensionBtn);
        }

        public void AddMenu(MapEditorUI mapEditorUI)
        {
            CanvasGroup parent = mapEditorUI.MainUIGroup[5]; // right panel
            _ratingsMenu = new GameObject("Ratings Menu");
            _ratingsMenu.transform.parent = parent.transform;

            AttachTransform(_ratingsMenu, 300, 140, 1, 1, 0, 0, 1, 1);

            Image image = _ratingsMenu.AddComponent<Image>();
            image.sprite = PersistentUI.Instance.Sprites.Background;
            image.type = Image.Type.Sliced;
            image.color = new Color(0.24f, 0.24f, 0.24f);

            var songTimeText = _ratings._songTimeLineController.transform.Find("Song Time").GetComponent<TextMeshProUGUI>();
            

            // Column 1
            AddLabel(_ratingsMenu.transform, "Ratings", "Save then press", new Vector2(-90, -25));
            AddButton(_ratingsMenu.transform, "Reload", "Reload Map", new Vector2(-90, -55), () =>
            {
                _ratings.Reload();
            });
            AddTextInput(_ratingsMenu.transform, "PredictedAcc", "Pred. %", new Vector2(-110, -85), System.Math.Round(_ratings.PredictedAcc * 100, 3).ToString(), (value) =>
            {
            }, false, true, "AI predicted accuracy %");
            AddTextInput(_ratingsMenu.transform, "Acc", "Acc", new Vector2(-110, -115), System.Math.Round(_ratings.Acc, 3).ToString(), (value) =>
            {
            }, false, true);

            // Column 2
            AddLabel(_ratingsMenu.transform, "Enable", "Enable Plugin", new Vector2(-10, -25));
            AddCheckbox(_ratingsMenu.transform, "Enable", "Enabled", new Vector2(15, -60), _ratings.Config.Enabled, (check) =>
            {
                _ratings.Config.Enabled = check;
                _ratingsBG.SetActive(check);
                _ratings.SaveConfigFile();
            });
            AddTextInput(_ratingsMenu.transform, "StarRating", "Star", new Vector2(-20, -85), System.Math.Round(_ratings.Star, 3).ToString(), (value) =>
            {
            }, false, true);
            AddTextInput(_ratingsMenu.transform, "Tech", "Tech", new Vector2(-20, -115), System.Math.Round(_ratings.Tech, 3).ToString(), (value) =>
            {
            }, false, true);

            // Column 3
            AddTextInput(_ratingsMenu.transform, "Timescale", "Timescale", new Vector2(80, -25), _ratings.Config.Timescale.ToString(), (value) =>
            {
                float res;
                if (float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture.NumberFormat, out res))
                {
                    _ratings.Config.Timescale = res;
                }
                _ratings.SaveConfigFile();
            }, true, false, "SS: 0.85, FS: 1.2, SFS: 1.5");
            AddTextInput(_ratingsMenu.transform, "NoteCount", "Note Count", new Vector2(80, -55), _ratings.Config.NotesCount.ToString(), (value) =>
            {
                int res;
                if (int.TryParse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture.NumberFormat, out res))
                {
                    _ratings.Config.NotesCount = res;
                }
                _ratings.SaveConfigFile();
            }, true, false, "How many notes to consider for the average");
            AddTextInput(_ratingsMenu.transform, "StarAccuracy", "Star Calc", new Vector2(80, -85), _ratings.Config.StarAccuracy.ToString(), (value) =>
            {
                float res;
                if (float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture.NumberFormat, out res))
                {
                    _ratings.Config.StarAccuracy = res;
                }
                _ratings.SaveConfigFile();
            }, true, false, "Accuracy used to calculate Star Rating");
            AddTextInput(_ratingsMenu.transform, "Pass", "Pass", new Vector2(80, -115), System.Math.Round(_ratings.Pass, 3).ToString(), (value) =>
            {
            }, false, true);

            _extensionBtn.Click = () =>
            {
                _ratingsMenu.SetActive(!_ratingsMenu.activeSelf);
            };

            _ratingsMenu.SetActive(false);
            if (!_ratings.Config.Enabled) _ratingsBG.SetActive(false);
        }

        private void AddButton(Transform parent, string title, string text, Vector2 pos, UnityAction onClick)
        {
            var button = Object.Instantiate(PersistentUI.Instance.ButtonPrefab, parent);
            MoveTransform(button.transform, 60, 25, 0.5f, 1, pos.x, pos.y);

            button.name = title;
            button.Button.onClick.AddListener(onClick);

            button.SetText(text);
            button.Text.enableAutoSizing = false;
            button.Text.fontSize = 12;
        }

        private void AddLabel(Transform parent, string title, string text, Vector2 pos, Vector2? size = null)
        {
            var entryLabel = new GameObject(title + " Label", typeof(TextMeshProUGUI));
            var rectTransform = ((RectTransform)entryLabel.transform);
            rectTransform.SetParent(parent);

            MoveTransform(rectTransform, 110, 24, 0.5f, 1, pos.x, pos.y);
            var textComponent = entryLabel.GetComponent<TextMeshProUGUI>();

            textComponent.name = title;
            textComponent.font = PersistentUI.Instance.ButtonPrefab.Text.font;
            textComponent.alignment = TextAlignmentOptions.Center;
            textComponent.fontSize = 14;
            textComponent.text = text;
        }

        private void AddTextInput(Transform parent, string title, string text, Vector2 pos, string value, UnityAction<string> onChange, bool interactable = true, bool store = false, string tooltip = "")
        {
            var entryLabel = new GameObject(title + " Label", typeof(TextMeshProUGUI));
            var rectTransform = ((RectTransform)entryLabel.transform);
            rectTransform.SetParent(parent);

            MoveTransform(rectTransform, 50, 16, 0.5f, 1, pos.x - 27.5f, pos.y);
            var textComponent = entryLabel.GetComponent<TextMeshProUGUI>();

            textComponent.name = title;
            textComponent.font = PersistentUI.Instance.ButtonPrefab.Text.font;
            textComponent.alignment = TextAlignmentOptions.Right;
            textComponent.fontSize = 12;
            textComponent.text = text;

            var textInput = Object.Instantiate(PersistentUI.Instance.TextInputPrefab, parent);
            MoveTransform(textInput.transform, 55, 20, 0.5f, 1, pos.x + 27.5f, pos.y);
            textInput.GetComponent<Image>().pixelsPerUnitMultiplier = 3;
            textInput.InputField.text = value;
            textInput.InputField.onFocusSelectAll = false;
            textInput.InputField.textComponent.alignment = TextAlignmentOptions.Left;
            textInput.InputField.textComponent.fontSize = 10;

            if (store)
                _values.Add(textInput.InputField);

            textInput.InputField.onValueChanged.AddListener(onChange);
            if (!interactable)
                textInput.InputField.interactable = false;

            if (!string.IsNullOrWhiteSpace(tooltip))
            {
                var tt = textInput.InputField.gameObject.AddComponent<Tooltip>();
                tt.TooltipOverride = tooltip;
            }
        }

        private void AddCheckbox(Transform parent, string title, string text, Vector2 pos, bool value, UnityAction<bool> onClick)
        {
            var entryLabel = new GameObject(title + " Label", typeof(TextMeshProUGUI));
            var rectTransform = ((RectTransform)entryLabel.transform);
            rectTransform.SetParent(parent);
            MoveTransform(rectTransform, 50, 16, 0.45f, 1, pos.x + 10, pos.y + 5);
            var textComponent = entryLabel.GetComponent<TextMeshProUGUI>();

            textComponent.name = title;
            textComponent.font = PersistentUI.Instance.ButtonPrefab.Text.font;
            textComponent.alignment = TextAlignmentOptions.Left;
            textComponent.fontSize = 12;
            textComponent.text = text;

            var original = GameObject.Find("Strobe Generator").GetComponentInChildren<Toggle>(true);
            var toggleObject = Object.Instantiate(original, parent.transform);
            MoveTransform(toggleObject.transform, 100, 25, 0.5f, 1, pos.x, pos.y);

            var toggleComponent = toggleObject.GetComponent<Toggle>();
            var colorBlock = toggleComponent.colors;
            colorBlock.normalColor = Color.white;
            toggleComponent.colors = colorBlock;
            toggleComponent.isOn = value;

            toggleComponent.onValueChanged.AddListener(onClick);
        }

        private RectTransform AttachTransform(GameObject obj, float sizeX, float sizeY, float anchorX, float anchorY, float anchorPosX, float anchorPosY, float pivotX = 0.5f, float pivotY = 0.5f)
        {
            RectTransform rectTransform = obj.AddComponent<RectTransform>();
            rectTransform.localScale = new Vector3(1, 1, 1);
            rectTransform.sizeDelta = new Vector2(sizeX, sizeY);
            rectTransform.pivot = new Vector2(pivotX, pivotY);
            rectTransform.anchorMin = rectTransform.anchorMax = new Vector2(anchorX, anchorY);
            rectTransform.anchoredPosition = new Vector3(anchorPosX, anchorPosY, 0);

            return rectTransform;
        }

        private void MoveTransform(Transform transform, float sizeX, float sizeY, float anchorX, float anchorY, float anchorPosX, float anchorPosY, float pivotX = 0.5f, float pivotY = 0.5f)
        {
            if (!(transform is RectTransform rectTransform)) return;

            rectTransform.localScale = new Vector3(1, 1, 1);
            rectTransform.sizeDelta = new Vector2(sizeX, sizeY);
            rectTransform.pivot = new Vector2(pivotX, pivotY);
            rectTransform.anchorMin = rectTransform.anchorMax = new Vector2(anchorX, anchorY);
            rectTransform.anchoredPosition = new Vector3(anchorPosX, anchorPosY, 0);
        }

        public void ApplyNewValues()
        {
            for (int i = 0; i < _values.Count; i++)
            {
                switch (i)
                {
                    case 0:
                        _values[i].text = System.Math.Round(_ratings.PredictedAcc * 100, 3).ToString();
                        break;
                    case 1:
                        _values[i].text = System.Math.Round(_ratings.Acc, 3).ToString();
                        break;
                    case 2:
                        _values[i].text = System.Math.Round(_ratings.Star, 3).ToString();
                        break;
                    case 3:
                        _values[i].text = System.Math.Round(_ratings.Tech, 3).ToString();
                        break;
                    case 4:
                        _values[i].text = System.Math.Round(_ratings.Pass, 3).ToString();
                        break;
                }

                _values[i].ForceLabelUpdate();
            }
        }
        
        public TriangleVisualizer AddTriangleVisualizer(MapEditorUI mapEditorUI)
        {
            CanvasGroup parent = mapEditorUI.MainUIGroup[5];
    
            var triangle = new GameObject("TriangleVisualizer");
            triangle.transform.SetParent(parent.transform, false);
            var rectTransform = AttachTransform(triangle, 80, 63, 1, 1, -65, -95);
            
            if (triangle.GetComponent<CanvasRenderer>() == null)
            {
                triangle.AddComponent<CanvasRenderer>();
            }
    
            var t = triangle.AddComponent<TriangleVisualizer>();
            
            t.SetAllDirty();
            return t;
        }
    }
}
