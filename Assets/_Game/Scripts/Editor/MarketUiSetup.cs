using Game.Organs;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    /// <summary>
    /// One-time scene setup for the organ market. Keeps the runtime scripts
    /// reference-driven: no hierarchy lookups are used during play.
    /// </summary>
    public static class MarketUiSetup
    {
        private const string TableScenePath = "Assets/_Game/Scenes/Table.unity";

        [MenuItem("House Cut/Setup Organ Market UI")]
        public static void Apply()
        {
            Scene scene = EditorSceneManager.OpenScene(TableScenePath, OpenSceneMode.Single);
            OrganSaleRunner runner = Object.FindAnyObjectByType<OrganSaleRunner>(FindObjectsInactive.Include);
            if (runner == null)
            {
                Debug.LogError("OrganSaleRunner Table sahnesinde bulunamadı.");
                return;
            }

            TMP_Text buttonLabel = FindText(runner.transform, "SatButtonText");
            OrganTileView tileTemplate = runner.GetComponentInChildren<OrganTileView>(true);
            if (buttonLabel == null || tileTemplate == null)
            {
                Debug.LogError("Market UI kurulamadı: SatButtonText veya OrganTile bulunamadı.");
                return;
            }

            CanvasGroup visualGroup = tileTemplate.GetComponent<CanvasGroup>();
            if (visualGroup == null)
            {
                visualGroup = tileTemplate.gameObject.AddComponent<CanvasGroup>();
            }

            SerializedObject runnerData = new SerializedObject(runner);
            runnerData.FindProperty("satButtonLabel").objectReferenceValue = buttonLabel;
            runnerData.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject tileData = new SerializedObject(tileTemplate);
            tileData.FindProperty("soldVisualGroup").objectReferenceValue = visualGroup;
            tileData.ApplyModifiedPropertiesWithoutUndo();

            SetActive(runner.transform, "TooltipValueText", false);
            SetActive(runner.transform, "TooltipBuffText", false);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Organ Market UI hazır: fiyat SAT butonunda, detay seçilen organda, satılmış organlar soluk.");
        }

        private static TMP_Text FindText(Transform root, string objectName)
        {
            foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.name == objectName)
                {
                    return text;
                }
            }

            return null;
        }

        private static void SetActive(Transform root, string objectName, bool active)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == objectName)
                {
                    child.gameObject.SetActive(active);
                    return;
                }
            }
        }
    }
}
