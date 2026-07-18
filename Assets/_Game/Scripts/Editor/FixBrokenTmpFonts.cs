using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Game.EditorTools
{
    // BebasNeue SDF ve SpecialElite SDF asset'leri atlas/material olmadan (bozuk) commitlenmişti.
    // Bu araç, TMP'nin kendi "Create Font Asset" akışıyla aynı şekilde temiz bir font asset üretip
    // içeriğini var olan (GUID'i sahnelerden referanslanan) asset'in üstüne kopyalar.
    internal static class FixBrokenTmpFonts
    {
        private static readonly (string fontAssetPath, string sourceFontPath)[] Targets =
        {
            ("Assets/_Game/Art/Fonts/BebasNeue SDF.asset", "Assets/_Game/Art/Fonts/BebasNeue-Regular.ttf"),
            ("Assets/_Game/Art/Fonts/SpecialElite SDF.asset", "Assets/_Game/Art/Fonts/SpecialElite-Regular.ttf"),
        };

        [MenuItem("Tools/House Cut/Fix Broken TMP Fonts")]
        private static void Run()
        {
            FontEngine.InitializeFontEngine();

            foreach (var (fontAssetPath, sourceFontPath) in Targets)
            {
                Fix(fontAssetPath, sourceFontPath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("FixBrokenTmpFonts: tamamlandı.");
        }

        private static void Fix(string fontAssetPath, string sourceFontPath)
        {
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontAssetPath);
            var sourceFont = AssetDatabase.LoadAssetAtPath<Font>(sourceFontPath);

            if (existing == null || sourceFont == null)
            {
                Debug.LogError($"FixBrokenTmpFonts: asset bulunamadı ({fontAssetPath} / {sourceFontPath})");
                return;
            }

            TMP_FontAsset rebuilt = TMP_FontAsset.CreateFontAsset(
                sourceFont, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024,
                AtlasPopulationMode.Dynamic, true);

            if (rebuilt == null)
            {
                Debug.LogError($"FixBrokenTmpFonts: font face yüklenemedi ({sourceFontPath}) — Import Settings'te 'Include Font Data' açık mı kontrol et.");
                return;
            }

            EditorUtility.CopySerialized(rebuilt, existing);

            if (existing.material != null)
                AssetDatabase.AddObjectToAsset(existing.material, existing);

            if (existing.atlasTextures != null && existing.atlasTextures.Length > 0 && existing.atlasTextures[0] != null)
                AssetDatabase.AddObjectToAsset(existing.atlasTextures[0], existing);

            EditorUtility.SetDirty(existing);
            Object.DestroyImmediate(rebuilt);

            Debug.Log($"FixBrokenTmpFonts: atlas yeniden oluşturuldu -> {fontAssetPath}");
        }
    }
}
