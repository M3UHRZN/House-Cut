using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public class OrganDataTests
    {
        [Test]
        public void OrganData_ExposesTheGddSchema()
        {
            Type organDataType = AppDomain.CurrentDomain
                .GetAssemblies()
                .Select(assembly => assembly.GetType("Game.Data.OrganData"))
                .FirstOrDefault(type => type != null);

            Assert.That(organDataType, Is.Not.Null);
            Assert.That(typeof(ScriptableObject).IsAssignableFrom(organDataType), Is.True);

            string[] expectedFields =
            {
                "id", "displayName", "dollarValue", "quantity",
                "debuffType", "debuffStrength", "buffType", "buffStrength",
                "mesh", "icon", "extractionSfx", "postProcessProfile"
            };

            CollectionAssert.IsSubsetOf(
                expectedFields,
                organDataType.GetFields().Select(field => field.Name).ToArray());
        }
    }
}
