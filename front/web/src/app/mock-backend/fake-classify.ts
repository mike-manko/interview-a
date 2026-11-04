import { TicketCategory, TicketPriority } from '../tickets/ticket';

export interface FakeClassification {
  readonly category: TicketCategory;
  readonly priority: TicketPriority;
  readonly suggestedReply: string;
}

interface CategoryRule {
  readonly category: TicketCategory;
  readonly pattern: RegExp;
}

// Stand-in for the Azure OpenAI call: keyword rules good enough to exercise the UI, nothing more. The first rule
// that matches wins, so Technical comes first: "payment page shows error 500" is a bug, not a billing question.
const categoryRules: readonly CategoryRule[] = [
  {
    category: TicketCategory.TECHNICAL,
    pattern:
      /\b(crash\w*|bug|errors?|export\w*|api|display|dashboard|not working|spins|http \d{3}|rate limits?)\b/i,
  },
  {
    category: TicketCategory.BILLING,
    pattern:
      /\b(charged?|charges|refund|invoice|payment|card|vat|pricing|discount|subscription|renew\w*)\b/i,
  },
  {
    category: TicketCategory.SHIPPING,
    pattern:
      /\b(deliver\w*|package|parcel|paczka|zamówienie|dostarcz\w*|shipped|shipping|tracking|size|order #)/i,
  },
  {
    category: TicketCategory.ACCOUNT,
    pattern:
      /\b(account|email address|password|sso|single sign-on|locked out|log ?in|sign ?in|gdpr|personal data)\b/i,
  },
];

const criticalPattern =
  /\b(entire team|all users|none of our \d+ users|outage|operations are stopped|locked out)\b/i;
const highPattern =
  /\b(crash\w*|charged twice|error \d{3}|not arriving|refund|urgent|gdpr|nadal|cannot book)\b/i;
const lowPattern = /\b(thanks|thank you|how can i|how do i|question|no action needed)\b/i;

const replies: Readonly<Record<TicketCategory, string>> = {
  Billing:
    'Thanks for reaching out about your billing question. We are reviewing your account and will reply within one business day.',
  Shipping:
    'Thanks for letting us know. We are checking the status of your order with the carrier and will update you shortly.',
  Account:
    'Thanks for contacting us about your account. For your security we will verify your identity before making changes.',
  Technical:
    'Sorry for the trouble. Our technical team is looking into it; could you tell us which version and device you are using?',
  Other: 'Thanks for your message. A member of our support team will get back to you soon.',
};

export function fakeClassify(subject: string, body: string): FakeClassification {
  const text = `${subject}\n${body}`;
  const category = guessCategory(text);
  return { category, priority: guessPriority(text), suggestedReply: replies[category] };
}

function guessCategory(text: string): TicketCategory {
  const match = categoryRules.find((rule) => rule.pattern.test(text));
  return match ? match.category : TicketCategory.OTHER;
}

function guessPriority(text: string): TicketPriority {
  if (criticalPattern.test(text)) {
    return TicketPriority.CRITICAL;
  }
  if (highPattern.test(text)) {
    return TicketPriority.HIGH;
  }
  if (lowPattern.test(text)) {
    return TicketPriority.LOW;
  }
  return TicketPriority.MEDIUM;
}
