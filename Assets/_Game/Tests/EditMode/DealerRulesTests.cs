using Game.Cards;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public class DealerRulesTests
    {
        [Test]
        public void Total16_ShouldHit()
        {
            var hand = new Hand();
            hand.AddCard(new Card(Suit.Spades, Rank.Ten));
            hand.AddCard(new Card(Suit.Hearts, Rank.Six));

            Assert.AreEqual(16, hand.Total);
            Assert.IsTrue(DealerRules.ShouldHit(hand));
        }

        [Test]
        public void HardSeventeen_Stands()
        {
            var hand = new Hand();
            hand.AddCard(new Card(Suit.Spades, Rank.Ten));
            hand.AddCard(new Card(Suit.Hearts, Rank.Seven));

            Assert.AreEqual(17, hand.Total);
            Assert.IsFalse(DealerRules.ShouldHit(hand));
        }

        [Test]
        public void SoftSeventeen_AcePlusSix_Stands()
        {
            var hand = new Hand();
            hand.AddCard(new Card(Suit.Spades, Rank.Ace));
            hand.AddCard(new Card(Suit.Hearts, Rank.Six));

            Assert.AreEqual(17, hand.Total);
            Assert.IsFalse(DealerRules.ShouldHit(hand));
        }
    }
}
