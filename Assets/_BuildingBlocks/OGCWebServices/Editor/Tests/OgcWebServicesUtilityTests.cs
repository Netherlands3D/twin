using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Netherlands3D.OgcWebServices.Shared
{
    public class OgcWebServicesUtilityTests
    {
        
        [Test]
        public void NormalizeUrl_LowercasesAllQueryKeysButPreservesValues()
        {
            var source = "https://example.org/wms?SERVICE=WMS&REQUEST=GetCapabilities&VERSION=1.3.0";

            Uri normalized = OgcWebServicesUtility.NormalizeUrl(source);

            // Keys should be lowercase, values should keep original case (e.g., GetCapabilities)
            StringAssert.Contains("?service=WMS&request=GetCapabilities&version=1.3.0", normalized.Query);
        }

        [Test]
        public void NormalizeUrl_KeepsPathAndHostIntact()
        {
            var source = "https://tiles.example.com/ows/endpoint?SERVICE=WFS&Request=GetCapabilities";

            var normalized = OgcWebServicesUtility.NormalizeUrl(source);

            Assert.AreEqual("https", normalized.Scheme);
            Assert.AreEqual("tiles.example.com", normalized.Host);
            Assert.AreEqual("/ows/endpoint", normalized.AbsolutePath);
            StringAssert.StartsWith("?service=WFS&request=GetCapabilities", normalized.Query);
        }

        [Test]
        [UnityPlatform(RuntimePlatform.WebGLPlayer)]
        public void NormalizeUrl_PreservesUrlEncodingAndDecodingRoundtrip()
        {
            // Value contains a space and a slash which are typically encoded, represents "Bag Gebouwen/3D"
            var source = "https://example.com/wms?LaYeRs=Bag+Gebouwen%2f3D";

            var normalized = OgcWebServicesUtility.NormalizeUrl(source);

            // Key should be lowercased; value should still represent "Bag Gebouwen/3D"
            StringAssert.Contains("layers=", normalized.Query);
            
            // The raw encoded representation should still decode to "Bag Gebouwen/3D"
            var normalizedStr = normalized.ToString();
            Debug.Log(normalizedStr);
            Assert.IsTrue(
                normalizedStr.Contains("layers=Bag+Gebouwen%2f3D"),
                "Value should remain 'Bag+Gebouwen%2f3D'"
            );
        }

        [Test]
        public void NormalizeUrl_NoQueryRemainsNoQuery()
        {
            var source = "https://example.com/ows";

            var normalized = OgcWebServicesUtility.NormalizeUrl(source);

            Assert.IsTrue(string.IsNullOrEmpty(normalized.Query), "Query should remain empty");
        }

        [Test]
        public void NormalizeUrl_HandlesDuplicateKeysDifferingOnlyByCaseByCollapsingToLowercase()
        {
            // When both "SERVICE" and "service" are present, the method will:
            //  - detect "SERVICE" as to-be-replaced
            //  - add "service" with its value
            //  - 'service' might already exist; desired behavior depends on QueryString implementation:
            //      * If disallowed, this test ensures no exceptions and that resulting key is lowercase.
            //      * If allowed, the last write may win.
            // We assert no exception and that the final query contains a lowercase key.
            var source = "https://example.com/wms?SERVICE=WMS&service=wcs";

            Uri normalized = null;
            Assert.DoesNotThrow(() => normalized = OgcWebServicesUtility.NormalizeUrl(source));

            // At minimum, only lowercase key should exist.
            // Depending on the QueryString dictionary semantics, the value may be "wcs" (existing)
            // or "WMS" (replaced). We accept either but enforce lowercase key presence.
            StringAssert.Contains("service=", normalized.Query);
            StringAssert.DoesNotContain("SERVICE=", normalized.Query);
        }

        [Test]
        public void NormalizeUrl_LeavesFragmentIntact()
        {
            var source = "https://example.com/wms?SERVICE=WMS#section-2";

            var normalized = OgcWebServicesUtility.NormalizeUrl(source);

            Assert.AreEqual("section-2", normalized.Fragment.TrimStart('#'));
            StringAssert.Contains("?service=WMS", normalized.Query);
        }
    }
}