using Game.Organs;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.Editor
{
    /// <summary>
    /// One-time scene setup for the organ market. Keeps the runtime scripts
    /// reference-driven: no hierarchy lookups are used during play.
    /// </summary>
    public static class MarketUiSetup
    {
        private const string TableScenePath = "Assets/_Game/Scenes/Table.unity";

        [MenuItem("House Cut/Rebuild Organ Market Panel")]
        public static void Apply()
        {
            Scene scene = EditorSceneManager.OpenScene(TableScenePath, OpenSceneMode.Single);
            OrganSaleRunner runner = Object.FindAnyObjectByType<OrganSaleRunner>(FindObjectsInactive.Include);
            if (runner == null)
            {
                Debug.LogError("OrganSaleRunner Table sahnesinde bulunamadı.");
                return;
            }

            OrganTileView tileTemplate = runner.GetComponentInChildren<OrganTileView>(true);
            Transform oldPanel = FindTransform(runner.transform, "TooltipPanel");
            if (oldPanel == null || tileTemplate == null)
            {
                Debug.LogError("Market UI kurulamadı: TooltipPanel veya OrganTile bulunamadı.");
                return;
            }

            CanvasGroup visualGroup = tileTemplate.GetComponent<CanvasGroup>();
            if (visualGroup == null)
            {
                visualGroup = tileTemplate.gameObject.AddComponent<CanvasGroup>();
            }

            SerializedObject tileData = new SerializedObject(tileTemplate);
            tileData.FindProperty("soldVisualGroup").objectReferenceValue = visualGroup;
            tileData.ApplyModifiedPropertiesWithoutUndo();

            RebuildPanel(oldPanel, out TMP_Text titleText, out TMP_Text descriptionText, out Button sellButton, out TMP_Text sellButtonText);
            WireRunner(runner, titleText, descriptionText, sellButton, sellButtonText);
            SetActive(runner.transform, "Footer", false);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Organ Market UI yeniden kuruldu: başlık, açıklama ve fiyatlı SAT butonu hazır.");
        }

        private static void RebuildPanel(Transform panel, out TMP_Text titleText, out TMP_Text descriptionText, out Button sellButton, out TMP_Text sellButtonText)
        {
            TMP_Text titleSource = FindText(panel, "TooltipNameText");
            TMP_Text descriptionSource = FindText(panel, "TooltipDebuffText");
            TMP_Text buttonSource = FindText(panel.root, "SatButtonText");
            if (titleSource == null || descriptionSource == null || buttonSource == null)
            {
                throw new System.InvalidOperationException("Market panel için gerekli TMP şablonları bulunamadı.");
            }

            // Keep temporary clones while the old hierarchy is removed. The
            // clones preserve the scene's chosen TMP fonts and materials.
            TMP_Text titleTemplate = Object.Instantiate(titleSource);
            TMP_Text descriptionTemplate = Object.Instantiate(descriptionSource);
            TMP_Text buttonTemplate = Object.Instantiate(buttonSource);

            foreach (Transform child in panel)
            {
                Object.DestroyImmediate(child.gameObject);
            }

            RemoveLayoutComponents(panel.gameObject);
            VerticalLayoutGroup layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 12, 12);
            layout.spacing = 14;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            titleText = CreateTextBox(panel, "SelectedOrganName", titleTemplate, "BİR ORGAN SEÇ", 82, TextAlignmentOptions.Center, 30);
            descriptionText = CreateTextBox(panel, "SelectedOrganDescription", descriptionTemplate,
                "Bir organ seç. Bedeli burada görünecek; satış fiyatı SAT butonunda yazacak.", 250, TextAlignmentOptions.TopLeft, 22);
            sellButton = CreateSellButton(panel, buttonTemplate, out sellButtonText);

            Object.DestroyImmediate(titleTemplate.gameObject);
            Object.DestroyImmediate(descriptionTemplate.gameObject);
            Object.DestroyImmediate(buttonTemplate.gameObject);
        }

        private static TMP_Text CreateTextBox(Transform parent, string objectName, TMP_Text template, string text, float height, TextAlignmentOptions alignment, float fontSize)
        {
            GameObject box = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            box.transform.SetParent(parent, false);
            box.GetComponent<Image>().color = new Color(0.07f, 0.06f, 0.055f, 0.96f);
            box.GetComponent<LayoutElement>().preferredHeight = height;

            TMP_Text label = Object.Instantiate(template, box.transform);
            label.name = "Label";
            label.text = text;
            label.alignment = alignment;
            label.fontSize = fontSize;
            label.enableWordWrapping = true;
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(18, 12);
            labelRect.offsetMax = new Vector2(-18, -12);
            return label;
        }

        private static Button CreateSellButton(Transform parent, TMP_Text template, out TMP_Text label)
        {
            GameObject buttonObject = new GameObject("SellSelectedOrgansButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonObject.transform.SetParent(parent, false);
            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.45f, 0.11f, 0.08f, 1f);
            LayoutElement element = buttonObject.GetComponent<LayoutElement>();
            element.preferredHeight = 78;

            Button button = buttonObject.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.84f, 0.72f, 1f);
            colors.pressedColor = new Color(0.75f, 0.55f, 0.48f, 1f);
            colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.55f);
            button.colors = colors;

            label = Object.Instantiate(template, buttonObject.transform);
            label.name = "Label";
            label.text = "SAT";
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 28;
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            return button;
        }

        private static void WireRunner(OrganSaleRunner runner, TMP_Text titleText, TMP_Text descriptionText, Button sellButton, TMP_Text sellButtonText)
        {
            SerializedObject runnerData = new SerializedObject(runner);
            runnerData.FindProperty("satButton").objectReferenceValue = sellButton;
            runnerData.FindProperty("satButtonLabel").objectReferenceValue = sellButtonText;
            runnerData.FindProperty("tooltipEmptyState").objectReferenceValue = null;
            runnerData.FindProperty("tooltipFilledState").objectReferenceValue = null;
            runnerData.FindProperty("tooltipNameText").objectReferenceValue = titleText;
            runnerData.FindProperty("tooltipValueText").objectReferenceValue = null;
            runnerData.FindProperty("tooltipDebuffText").objectReferenceValue = descriptionText;
            runnerData.FindProperty("tooltipBuffText").objectReferenceValue = null;
            runnerData.ApplyModifiedPropertiesWithoutUndo();
        }

        private static TMP_Text FindText(Transform root, string objectName)
        {
            Transform transform = FindTransform(root, objectName);
            return transform != null ? transform.GetComponent<TMP_Text>() : null;
        }

        private static Transform FindTransform(Transform root, string objectName)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == objectName)
                {
                    return child;
                }
            }

            return null;
        }

        private static void RemoveLayoutComponents(GameObject target)
        {
            foreach (Component component in target.GetComponents<Component>())
            {
                if (component is LayoutGroup || component is ContentSizeFitter)
                {
                    Object.DestroyImmediate(component);
                }
            }
        }
    }
}
