using Game.Cards;
using Game.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public class CardPresentationTests
    {
        [Test]
        public void ProjectCatalog_ContainsAll52NamedPrefabs()
        {
            CardPrefabCatalog catalog = AssetDatabase.LoadAssetAtPath<CardPrefabCatalog>(
                "Assets/_Game/Data/CardPrefabCatalog.asset");

            Assert.That(catalog, Is.Not.Null);
            foreach (Suit suit in System.Enum.GetValues(typeof(Suit)))
            {
                foreach (Rank rank in System.Enum.GetValues(typeof(Rank)))
                {
                    GameObject prefab = catalog.GetPrefab(new Card(suit, rank));
                    Assert.That(prefab, Is.Not.Null, $"Missing prefab for {rank} of {suit}");
                    Assert.That(prefab.name, Is.EqualTo($"{rank} of {suit}"));
                }
            }
        }

        [Test]
        public void Catalog_MapsEverySuitAndRankToItsPrefab()
        {
            var catalog = ScriptableObject.CreateInstance<CardPrefabCatalog>();
            var prefabs = new GameObject[52];

            try
            {
                for (int i = 0; i < prefabs.Length; i++)
                    prefabs[i] = new GameObject($"Card_{i}");

                var serialized = new SerializedObject(catalog);
                SerializedProperty entries = serialized.FindProperty("prefabs");
                entries.arraySize = prefabs.Length;
                for (int i = 0; i < prefabs.Length; i++)
                    entries.GetArrayElementAtIndex(i).objectReferenceValue = prefabs[i];
                serialized.ApplyModifiedPropertiesWithoutUndo();

                Assert.That(
                    catalog.GetPrefab(new Card(Suit.Clubs, Rank.Ace)),
                    Is.SameAs(prefabs[0]));
                Assert.That(
                    catalog.GetPrefab(new Card(Suit.Hearts, Rank.Eight)),
                    Is.SameAs(prefabs[(int)Suit.Hearts * 13 + (int)Rank.Eight]));
                Assert.That(
                    catalog.GetPrefab(new Card(Suit.Spades, Rank.King)),
                    Is.SameAs(prefabs[51]));
            }
            finally
            {
                foreach (GameObject prefab in prefabs)
                    Object.DestroyImmediate(prefab);
                Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void Placement_UsesOneAnchorAndOffsetsAdditionalCards()
        {
            var anchorObject = new GameObject("PlayerCardAnchor");
            try
            {
                anchorObject.transform.position = new Vector3(4f, 2f, -1f);
                anchorObject.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

                Vector3 first = CardPlacement.GetPosition(anchorObject.transform, 0, 0.4f);
                Vector3 third = CardPlacement.GetPosition(anchorObject.transform, 2, 0.4f);

                Assert.That(first, Is.EqualTo(anchorObject.transform.position));
                Assert.That(third, Is.EqualTo(anchorObject.transform.position + anchorObject.transform.right * 0.8f));
            }
            finally
            {
                Object.DestroyImmediate(anchorObject);
            }
        }

        [Test]
        public void GizmoSize_ScalesLikeTheRuntimeCardModel()
        {
            Vector3 size = CardPlacement.GetGizmoSize(0.65f);

            Assert.That(size.x, Is.EqualTo(0.728f).Within(0.001f));
            Assert.That(size.y, Is.EqualTo(1.144f).Within(0.001f));
            Assert.That(size.z, Is.EqualTo(0.02f).Within(0.001f));
        }

        [Test]
        public void CardVisual_FaceDownThenReveal_RestoresModelRotation()
        {
            var root = new GameObject("CardView");
            var model = new GameObject("Model");
            try
            {
                model.transform.SetParent(root.transform, false);
                model.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                Quaternion faceUpRotation = model.transform.localRotation;

                CardVisual visual = root.AddComponent<CardVisual>();
                visual.Initialize(model.transform);
                visual.SetCard(new Card(Suit.Diamonds, Rank.Queen), false);

                Assert.That(Quaternion.Angle(model.transform.localRotation, faceUpRotation), Is.EqualTo(180f).Within(0.1f));

                visual.SetCard(new Card(Suit.Diamonds, Rank.Queen), true);
                Assert.That(Quaternion.Angle(model.transform.localRotation, faceUpRotation), Is.LessThan(0.1f));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
