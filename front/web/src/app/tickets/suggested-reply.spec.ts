import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { expectAccessible } from '../../testing/expect-accessible';
import { SuggestedReply } from './suggested-reply';
import { TriageState } from './triage-state';

@Component({
  imports: [SuggestedReply],
  template: `<app-suggested-reply [reply]="reply()" [state]="state()" />`,
})
class Host {
  readonly reply = signal<string | null>('You can change your email under Settings > Profile.');
  readonly state = signal<TriageState>('triaged');
}

describe('SuggestedReply', () => {
  let writeText: ReturnType<typeof vi.fn<(text: string) => Promise<void>>>;

  async function render(reply: string | null, state: TriageState): Promise<HTMLElement> {
    const fixture = TestBed.createComponent(Host);
    fixture.componentInstance.reply.set(reply);
    fixture.componentInstance.state.set(state);
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  function copyButton(element: HTMLElement): HTMLButtonElement {
    const button = Array.from(element.querySelectorAll('button')).find((candidate) =>
      candidate.textContent?.includes('Copy reply'),
    );
    if (!button) {
      throw new Error('Copy reply button missing');
    }
    return button;
  }

  function liveRegionText(element: HTMLElement): string {
    return element.querySelector('[aria-live="polite"]')?.textContent?.trim() ?? '';
  }

  beforeEach(() => {
    writeText = vi.fn<(text: string) => Promise<void>>().mockResolvedValue(undefined);
    vi.stubGlobal('navigator', { ...navigator, clipboard: { writeText } });
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    vi.useRealTimers();
  });

  it('shows the draft as plain text, labelled as an AI draft', async () => {
    const element = await render('<b>Hi</b>\nThanks for writing.', 'triaged');

    expect(element.querySelector('h3')?.textContent).toContain('Suggested reply');
    expect(element.textContent).toContain('AI draft · review before sending');
    expect(element.querySelector('p.whitespace-pre-wrap')?.textContent).toBe(
      '<b>Hi</b>\nThanks for writing.',
    );
    expect(element.querySelector('b')).toBeNull();
    await expectAccessible(element);
  });

  it('copies the reply and says so for two seconds', async () => {
    const element = await render('Thanks for writing.', 'triaged');
    // After the first render: whenStable waits on the change detection timer, which fake timers would hold back.
    vi.useFakeTimers();

    copyButton(element).click();
    await vi.waitFor(() => expect(liveRegionText(element)).toBe('Copied'));
    expect(writeText).toHaveBeenCalledWith('Thanks for writing.');

    await vi.advanceTimersByTimeAsync(2000);
    expect(liveRegionText(element)).toBe('');
  });

  it('tells the agent to select the text when the clipboard refuses', async () => {
    writeText.mockRejectedValue(new DOMException('Denied', 'NotAllowedError'));
    const element = await render('Thanks for writing.', 'triaged');

    copyButton(element).click();

    await vi.waitFor(() =>
      expect(liveRegionText(element)).toBe('Copy failed; select the text instead.'),
    );
    await expectAccessible(element);
  });

  it('explains that the draft is still coming while the ticket is in triage', async () => {
    const element = await render(null, 'classifying');

    expect(element.textContent).toContain('The AI drafts a reply when it classifies this ticket.');
    expect(element.querySelector('button')).toBeNull();
    await expectAccessible(element);
  });

  it('tells the agent to write the reply when triage gave up', async () => {
    const element = await render(null, 'gave-up');

    expect(element.textContent).toContain(
      'Triage gave up on this ticket, so there is no AI draft.',
    );
  });
});
