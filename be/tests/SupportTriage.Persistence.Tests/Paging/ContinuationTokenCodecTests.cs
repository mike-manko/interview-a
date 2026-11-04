using SupportTriage.Persistence.Paging;

namespace SupportTriage.Persistence.Tests.Paging;

[TestFixture]
public sealed class ContinuationTokenCodecTests
{
    [Test]
    public void TryDecode_EncodedToken_ReturnsTheOriginal()
    {
        const string token = """[{"token":"+RID:~06QLAMV1AVULAAAAAAAAAA==#RT:1#TRC:4","range":{"min":"","max":"FF"}}]""";

        var encoded = ContinuationTokenCodec.Encode(token);

        Assert.Multiple(() =>
        {
            Assert.That(ContinuationTokenCodec.TryDecode(encoded, out var decoded), Is.True);
            Assert.That(decoded, Is.EqualTo(token));
        });
    }

    [Test]
    public void Encode_TokenWithUrlSensitiveCharacters_ProducesUrlSafeText()
    {
        var encoded = ContinuationTokenCodec.Encode("""{"a":"+/=?&"}""")!;

        Assert.That(encoded, Does.Match("^[A-Za-z0-9_-]+$"));
    }

    [Test]
    public void Encode_Null_ReturnsNull()
    {
        Assert.That(ContinuationTokenCodec.Encode(null), Is.Null);
    }

    [TestCase("not base64 !")]
    [TestCase("")]
    [TestCase(null)]
    public void TryDecode_InvalidText_ReturnsFalse(string? encoded)
    {
        Assert.That(ContinuationTokenCodec.TryDecode(encoded, out _), Is.False);
    }
}
