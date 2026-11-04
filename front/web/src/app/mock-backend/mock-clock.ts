import { InjectionToken } from '@angular/core';

// The mock worker derives every ticket's triage state from timestamps at read time, so tests move this clock
// instead of faking timers. Returns milliseconds since the epoch, like Date.now.
export const mockClock = new InjectionToken<() => number>('mockClock', {
  providedIn: 'root',
  factory: () => Date.now,
});
