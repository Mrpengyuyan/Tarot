using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TarotUnity.Network;

namespace TarotUnity.Tests.EditMode
{
    public sealed class Phase65GuestSessionTests
    {
        [Test]
        public void GuestSessionRouteMatchesBackendContract()
        {
            var field = typeof(ApiRoutes).GetField("GuestSession", BindingFlags.Public | BindingFlags.Static);

            Assert.That(field, Is.Not.Null);
            Assert.That(field.GetValue(null), Is.EqualTo("/guest-session"));
        }

        [Test]
        public void ApiClientExposesGuestSessionCoroutine()
        {
            var method = typeof(ApiClient).GetMethod("CreateGuestSession", BindingFlags.Public | BindingFlags.Instance);

            Assert.That(method, Is.Not.Null);
            Assert.That(method.ReturnType, Is.EqualTo(typeof(IEnumerator)));
        }
    }
}
