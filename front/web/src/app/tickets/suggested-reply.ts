import { Component, computed, DestroyRef, inject, input, signal } from '@angular/core';
import { TriageState } from './triage-state';

type CopyStatus = 'idle' | 'copied' | 'failed';

const copiedFeedbackMs = 2000;

const copyStatusMessages: Readonly<Record<CopyStatus, string>> = {
  idle: '',
  copied: 'Copied',
  failed: 'Copy failed; select the text instead.',
};

// Why there is no draft yet, so the agent knows whether to wait or to write the reply by hand.
const missingReplyReasons: Readonly<Record<TriageState, string>> = {
  queued: 'The AI drafts a reply when it classifies this ticket. It appears here by itself.',
  classifying: 'The AI drafts a reply when it classifies this ticket. It appears here by itself.',
  retrying: 'Triage failed and is being retried. The draft appears here if a retry succeeds.',
  triaged: 'The AI did not suggest a reply for this ticket. Write the reply yourself.',
  'gave-up': 'Triage gave up on this ticket, so there is no AI draft. Write the reply yourself.',
};

@Component({
  selector: 'app-suggested-reply',
  host: { class: 'block rounded-card bg-surface-sunken p-4' },
  templateUrl: './suggested-reply.html',
})
export class SuggestedReply {
  private readonly destroyRef = inject(DestroyRef);

  readonly reply = input.required<string | null>();
  readonly state = input.required<TriageState>();

  private readonly copyStatus = signal<CopyStatus>('idle');
  protected readonly copyStatusMessage = computed(() => copyStatusMessages[this.copyStatus()]);
  protected readonly missingReason = computed(() => missingReplyReasons[this.state()]);

  private resetTimer: ReturnType<typeof setTimeout> | undefined;

  constructor() {
    this.destroyRef.onDestroy(() => clearTimeout(this.resetTimer));
  }

  protected async copyReply(reply: string): Promise<void> {
    clearTimeout(this.resetTimer);
    this.copyStatus.set('idle');
    try {
      // Missing outside secure contexts (plain http on a LAN address); that lands in catch like a denied write.
      await navigator.clipboard.writeText(reply);
    } catch {
      this.copyStatus.set('failed');
      return;
    }
    this.copyStatus.set('copied');
    this.resetTimer = setTimeout(() => this.copyStatus.set('idle'), copiedFeedbackMs);
  }
}
