using UnityEngine;

namespace Game.Core
{
    public static class CardPlacement
    {
        private const float CardWidth = 1.12f;
        private const float CardHeight = 1.76f;
        private const float GizmoThickness = 0.02f;

        public static Vector3 GetPosition(Transform anchor, int cardIndex, float spacing)
        {
            return anchor.position + anchor.right * (cardIndex * spacing);
        }

        public static Vector3 GetGizmoSize(float scale)
        {
            return new Vector3(CardWidth * scale, CardHeight * scale, GizmoThickness);
        }
    }
}
