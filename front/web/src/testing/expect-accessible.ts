import axe from 'axe-core';
import { expect } from 'vitest';

// jsdom has no layout engine, so colour contrast is covered by the token pairs in styling.md, not here.
const options: axe.RunOptions = { rules: { 'color-contrast': { enabled: false } } };

export async function expectAccessible(element: Element): Promise<void> {
  const { violations } = await axe.run(element, options);
  const report = violations
    .map((violation) => {
      const nodes = violation.nodes.map((node) => node.html).join('\n  ');
      return `${violation.id} (${violation.impact}): ${violation.help}\n  ${nodes}`;
    })
    .join('\n');
  expect(violations, report).toHaveLength(0);
}
