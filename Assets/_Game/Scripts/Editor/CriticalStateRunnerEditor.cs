using Game.Presentation;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// Play-mode test harness for CriticalStateRunner (GDD §04 Kritik Hâl):
    /// buttons to sell the runner's own organs and nudge its own Bank, so the
    /// pulse/FOV/shake/vignette can be triggered and watched manually without
    /// a real game loop wired up yet.
    /// </summary>
    [CustomEditor(typeof(CriticalStateRunner))]
    public class CriticalStateRunnerEditor : UnityEditor.Editor
    {
        private int withdrawAmount = 200;
        private int depositAmount = 500;
        private bool forceCriticalToggle;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var runner = (CriticalStateRunner)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Kritik Hâl Test Aracı (Play mode)", EditorStyles.boldLabel);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Test butonları için Play mode'a geç.", MessageType.Info);
                return;
            }

            if (runner.OrganManager == null || runner.Bank == null)
            {
                EditorGUILayout.HelpBox("OrganManager/Bank henüz kurulmadı (Awake bekleniyor).", MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField("Para", $"${runner.Bank.Balance}");
            EditorGUILayout.LabelField("Kritik Hâl", runner.IsCritical ? "EVET" : "hayır");

            EditorGUILayout.Space();

            bool newForceToggle = EditorGUILayout.Toggle("Kritik Hâle Zorla", forceCriticalToggle);
            if (newForceToggle != forceCriticalToggle)
            {
                forceCriticalToggle = newForceToggle;
                runner.SetForceCritical(forceCriticalToggle);
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Bir sonraki organı sat"))
            {
                foreach (var organ in runner.OrganManager.Organs)
                {
                    if (organ.id == "heart" || runner.OrganManager.IsSold(organ))
                    {
                        continue;
                    }

                    int gained = runner.OrganManager.Sell(organ);
                    Debug.Log($"[KritikHalTest] Satıldı: {organ.displayName} (+${gained})");
                    break;
                }
            }

            EditorGUILayout.BeginHorizontal();
            withdrawAmount = EditorGUILayout.IntField(withdrawAmount);
            if (GUILayout.Button($"Para çek (-${withdrawAmount})"))
            {
                runner.Bank.Withdraw(withdrawAmount);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            depositAmount = EditorGUILayout.IntField(depositAmount);
            if (GUILayout.Button($"Para yatır (+${depositAmount})"))
            {
                runner.Bank.Deposit(depositAmount);
            }
            EditorGUILayout.EndHorizontal();

            Repaint();
        }
    }
}
