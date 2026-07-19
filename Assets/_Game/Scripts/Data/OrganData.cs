using UnityEngine;

namespace Game.Data
{
    public enum OrganBuffType
    {
        None,
        DealerCardHint,
        LowerCriticalThreshold,
        BetterSaleRate,
        SlowerBetRamp,
        ShowHandDistance,
        PeekHoleCard,
        ExtraDecisionTime,
        CashRegenOnWin
    }

    public enum OrganDebuffType
    {
        None,
        MonoAudio,
        WorseSaleRate,
        MinorVignette,
        CashBleedPerHand,
        NauseaOnHit,
        LeftEyeMask,
        ReducedDecisionTime,
        ColorShiftBleed,
        HeartEnd
    }

    [CreateAssetMenu(fileName = "OrganData", menuName = "House Cut/Organ Data")]
    public class OrganData : ScriptableObject
    {
        [Header("Identity")]
        public string id;
        public string displayName;
        public int dollarValue;
        public int quantity = 1;

        [Header("Effects")]
        public OrganDebuffType debuffType;
        public float debuffStrength;
        public OrganBuffType buffType;
        public float buffStrength;
        [TextArea] public string debuffDescription;
        [TextArea] public string buffDescription;

        [Header("Presentation")]
        public Sprite icon;
        public AudioClip extractionSfx;
        public ScriptableObject postProcessProfile;

        [Tooltip("Organ satılınca sahnede spawn olan 3B prefab. Üzerinde Rigidbody (Is Kinematic AÇIK) + bir Collider bulunmalı; çıkarma animasyonu bittiğinde kinematik kapatılıp tezgaha düşer.")]
        public GameObject extractionPrefab;
    }
}
