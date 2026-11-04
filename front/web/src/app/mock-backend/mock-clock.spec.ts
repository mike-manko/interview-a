import { TestBed } from '@angular/core/testing';
import { mockClock } from './mock-clock';

describe('mockClock', () => {
  it('tells the real time by default', () => {
    const clock = TestBed.inject(mockClock);

    expect(Math.abs(clock() - Date.now())).toBeLessThan(1000);
  });
});
