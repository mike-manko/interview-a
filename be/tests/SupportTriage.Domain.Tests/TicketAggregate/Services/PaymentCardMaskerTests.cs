using SupportTriage.Domain.TicketAggregate.Services;

namespace SupportTriage.Domain.Tests.TicketAggregate.Services;

[TestFixture]
public sealed class PaymentCardMaskerTests
{
    // Seed ticket t-009.
    [Test]
    public void Mask_SeedTicketWithCardAndCvv_MasksBoth()
    {
        var masked = PaymentCardMasker.Mask("My new card is 4111 1111 1111 1111, exp 09/29, CVV 123. Please put it on my account.");

        Assert.That(masked, Is.EqualTo("My new card is [card ending 1111], exp 09/29, CVV [removed]. Please put it on my account."));
    }

    [TestCase("4111111111111111", "[card ending 1111]")]
    [TestCase("5555-5555-5555-4444", "[card ending 4444]")]
    [TestCase("3782 822463 10005", "[card ending 0005]")]
    [TestCase("4222222222222", "[card ending 2222]")]
    public void Mask_LuhnValidCardNumber_KeepsOnlyTheLastFourDigits(string cardNumber, string expected)
    {
        Assert.That(PaymentCardMasker.Mask("card " + cardNumber + " please"), Is.EqualTo("card " + expected + " please"));
    }

    // Long numbers that fail the Luhn check (order numbers, phone numbers, tracking ids) are left alone.
    [TestCase("4111 1111 1111 1112")]
    [TestCase("Order #88231")]
    [TestCase("None of our 250 users can log in since 9:00.")]
    [TestCase("Tracking 1234567890123")]
    [TestCase("Invoice 2026-10 is wrong")]
    public void Mask_TextWithoutCardNumber_ReturnsTextUnchanged(string text)
    {
        Assert.That(PaymentCardMasker.Mask(text), Is.EqualTo(text));
    }

    [TestCase("CVV 123", "CVV [removed]")]
    [TestCase("cvc: 9876", "cvc [removed]")]
    [TestCase("security code=321", "security code [removed]")]
    [TestCase("CVV2 456", "CVV2 [removed]")]
    public void Mask_LabelledSecurityCode_RemovesTheDigits(string text, string expected)
    {
        Assert.That(PaymentCardMasker.Mask(text), Is.EqualTo(expected));
    }

    [Test]
    public void Mask_TextWithoutDigits_ReturnsSameInstance()
    {
        const string text = "Just wanted to say your support team was great last week.";

        Assert.That(PaymentCardMasker.Mask(text), Is.SameAs(text));
    }

    [Test]
    public void Mask_NumberLongerThanACardNumber_ReturnsTextUnchanged()
    {
        const string text = "Reference 41111111111111111111111";

        Assert.That(PaymentCardMasker.Mask(text), Is.EqualTo(text));
    }
}
