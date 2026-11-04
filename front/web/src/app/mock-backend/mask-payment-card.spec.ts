import { maskPaymentCard } from './mask-payment-card';

describe('maskPaymentCard', () => {
  it('masks a card number written with spaces and keeps its last four digits', () => {
    expect(maskPaymentCard('My new card is 4111 1111 1111 1111, exp 09/29.')).toBe(
      'My new card is [card ending 1111], exp 09/29.',
    );
  });

  it('masks a card number written with dashes or without separators', () => {
    expect(maskPaymentCard('5500-0000-0000-0004 and 4012888888881881')).toBe(
      '[card ending 0004] and [card ending 1881]',
    );
  });

  it('removes a labelled security code', () => {
    expect(maskPaymentCard('CVV 123, cvc2: 9876')).toBe('CVV [removed], cvc2 [removed]');
  });

  it('leaves digit sequences that are not card numbers alone', () => {
    const text = 'Order #88231, call 1234 5678 9012 3 before 9:00.';

    expect(maskPaymentCard(text)).toBe(text);
  });
});
