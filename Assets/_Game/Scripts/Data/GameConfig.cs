using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "House Cut/Game Config")]
    public class GameConfig : ScriptableObject
    {
        [Header("Ekonomi")]
        [Min(0)] public int startMoney = 1000;
        [Min(0)] public int minBet = 700;
        [Min(0)] public int betStep = 50;
        [Min(0)] public int betRamp = 300;

        [Header("Zamanlama")]
        [Min(0)] public float decisionTime = 15f;

        [Header("Ödeme")]
        [Min(1f)] public float blackjackPayout = 1.5f;
    }
}
