using Game.Cards;
using UnityEngine;

namespace Game.Core
{
    [CreateAssetMenu(fileName = "CardPrefabCatalog", menuName = "House Cut/Card Prefab Catalog")]
    public sealed class CardPrefabCatalog : ScriptableObject
    {
        private const int RankCount = 13;
        private const int CardCount = 52;

        [SerializeField] private GameObject[] prefabs = new GameObject[CardCount];

        public GameObject GetPrefab(Card card)
        {
            int index = (int)card.Suit * RankCount + (int)card.Rank;
            return prefabs != null && index >= 0 && index < prefabs.Length
                ? prefabs[index]
                : null;
        }
    }
}
