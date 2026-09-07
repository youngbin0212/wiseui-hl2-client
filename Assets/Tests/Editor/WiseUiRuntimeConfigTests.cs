using System;
using NUnit.Framework;
using UnityEngine;

public class WiseUiRuntimeConfigTests
{
    [Serializable]
    class Payload
    {
        public string initServerUrl;
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void NormalizeBaseUrl_UsesLoopbackFallbackForMissingValue(string value)
    {
        Assert.AreEqual("http://127.0.0.1:8002", WiseUiRuntimeConfig.NormalizeBaseUrl(value));
    }

    [Test]
    public void NormalizeBaseUrl_TrimsWhitespaceAndTrailingSlash()
    {
        Assert.AreEqual(
            "http://10.0.0.5:8002",
            WiseUiRuntimeConfig.NormalizeBaseUrl("  http://10.0.0.5:8002///  "));
    }

    [TestCase("ftp://10.0.0.5:8002")]
    [TestCase("file:///tmp/init")]
    [TestCase("10.0.0.5:8002")]
    public void NormalizeBaseUrl_RejectsNonHttpOrRelativeValues(string value)
    {
        Assert.Throws<ArgumentException>(() => WiseUiRuntimeConfig.NormalizeBaseUrl(value));
    }

    [Test]
    public void BuildJson_StoresTheInjectedNormalizedValue()
    {
        string json = WiseUiRuntimeConfig.BuildJson("http://10.0.0.8:9000/");
        var payload = JsonUtility.FromJson<Payload>(json);
        Assert.AreEqual("http://10.0.0.8:9000", payload.initServerUrl);
    }
}
