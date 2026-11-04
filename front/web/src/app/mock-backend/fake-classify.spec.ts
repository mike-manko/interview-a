import { fakeClassify } from './fake-classify';

describe('fakeClassify', () => {
  it('classifies a double charge as high-priority billing with a billing reply', () => {
    const classification = fakeClassify('Charged twice', 'I was charged twice for October.');

    expect(classification).toMatchObject({ category: 'Billing', priority: 'High' });
    expect(classification.suggestedReply).toContain('billing');
  });

  it('classifies a crash as high-priority technical', () => {
    expect(fakeClassify('App crashes on login', 'It closes after my password.')).toMatchObject({
      category: 'Technical',
      priority: 'High',
    });
  });

  it('classifies a late parcel as shipping', () => {
    expect(fakeClassify('Paczka nie dotarła', 'Zamówienie #88231').category).toBe('Shipping');
  });

  it('classifies a team lock-out as critical account trouble', () => {
    expect(fakeClassify('Entire team locked out', 'Nobody can log in.')).toMatchObject({
      category: 'Account',
      priority: 'Critical',
    });
  });

  it('gives a how-to question low priority', () => {
    expect(fakeClassify('Change email address', 'How can I change it?').priority).toBe('Low');
  });

  it('falls back to Other with medium priority', () => {
    expect(fakeClassify('Hello', 'Just saying hi.')).toMatchObject({
      category: 'Other',
      priority: 'Medium',
    });
  });
});
