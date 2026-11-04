// Same rules as the API's PaymentCardMasker: a 13-19 digit sequence (single spaces or dashes allowed between digits)
// that passes the Luhn check becomes "[card ending 1234]", and a labelled security code becomes "[removed]".
const cardNumberCandidate = /(?<!\d)\d(?:[ -]?\d){12,18}(?!\d)/g;
const securityCode = /\b(CVV2?|CVC2?|CID|security code)\b\s*[:=]?\s*\d{3,4}\b/gi;
const visibleDigits = 4;

export function maskPaymentCard(text: string): string {
  const withoutCardNumbers = text.replace(cardNumberCandidate, (candidate) =>
    maskCardNumber(candidate),
  );
  return withoutCardNumbers.replace(securityCode, (_match, label: string) => `${label} [removed]`);
}

function maskCardNumber(candidate: string): string {
  const digits = candidate.replace(/\D/g, '');
  if (!passesLuhnCheck(digits)) {
    return candidate;
  }
  return `[card ending ${digits.slice(-visibleDigits)}]`;
}

function passesLuhnCheck(digits: string): boolean {
  let sum = 0;
  let doubleIt = false;
  for (let index = digits.length - 1; index >= 0; index--) {
    let digit = Number(digits[index]);
    if (doubleIt) {
      digit *= 2;
      if (digit > 9) {
        digit -= 9;
      }
    }
    sum += digit;
    doubleIt = !doubleIt;
  }
  return sum % 10 === 0;
}
