using UnityEngine;

namespace Game.Core
{
    public static class CardPlacement
    {
        public static Vector3 GetPosition(Transform anchor, int cardIndex, float spacing)
        {
            return anchor.position + anchor.right * (cardIndex * spacing);
        }
    }
}
