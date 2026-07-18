using System;
using Game.Cards;
using Game.Core;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class CardPrefabCatalogBuilder
    {
        public const string CatalogPath = "Assets/_Game/Data/CardPrefabCatalog.asset";
        private const string PrefabFolder = "Assets/_Game/Prefabs";

        [MenuItem("House Cut/Cards/Build Prefab Catalog")]
        public static void Build()
        {
            CardPrefabCatalog catalog = AssetDatabase.LoadAssetAtPath<CardPrefabCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<CardPrefabCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            var serialized = new SerializedObject(catalog);
            SerializedProperty prefabs = serialized.FindProperty("prefabs");
            prefabs.arraySize = 52;

            foreach (Suit suit in Enum.GetValues(typeof(Suit)))
            {
                foreach (Rank rank in Enum.GetValues(typeof(Rank)))
                {
                    string cardName = $"{rank} of {suit}";
                    string path = $"{PrefabFolder}/{cardName}.prefab";
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab == null)
                    {
                        throw new InvalidOperationException($"Card prefab missing: {path}");
                    }

                    int index = (int)suit * 13 + (int)rank;
                    prefabs.GetArrayElementAtIndex(index).objectReferenceValue = prefab;
                }
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log("Card prefab catalog built: 52/52 cards.");
        }

        [MenuItem("House Cut/Cards/Deal Preview Hand")]
        public static void DealPreviewHand()
        {
            UnityEngine.Object.FindFirstObjectByType<BlackjackLoopRunner>()?.ConfirmBet();
        }

        [MenuItem("House Cut/Cards/Stand Preview Hand")]
        public static void StandPreviewHand()
        {
            UnityEngine.Object.FindFirstObjectByType<BlackjackLoopRunner>()?.Stand();
        }

    }
}
