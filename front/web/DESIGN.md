# Design: Support triage console

Mode: **Operate**. The tool disappears into the task; brand lives in precise details.

## World

A calm dispatch console. Light by default, because the use scene is a bright office and long reading. A dark palette exists behind the `.dark` class. One system sans for everything; hierarchy comes from weight and size, never from display fonts. Dense but breathable: rows of 44px or more, generous separation between groups, tight spacing inside a group.

## Color (tokens in `src/styles.css`; templates use only role tokens)

- Surfaces: `bg-surface-raised` is the page; `bg-surface` holds content panels; `bg-surface-sunken` for toolbars, chips and inset areas.
- Text: `text-text`, `text-text-muted`. Never gray on colored surfaces.
- Accent `primary` (indigo) only for primary actions, current selection, focus and the "classifying" state.
- Severity carries priority: `priority-critical`, `-high`, `-medium`, `-low`. Used for the level bars and the group rail, never for large fills. Critical text is `text-danger`.
- State: `success` (triaged), `warning-*` (retrying), `danger` (gave up, errors).

## Signature details

- **PriorityBadge**: four level bars, filled to the level in its severity color, plus the label. Severity reads without color.
- **TriageStateBadge**: dot plus words for queued, classifying (pulsing dot, motion-safe), retrying, triaged, gave up.
- **Group headers** in the queue: a 3px vertical rail in the group's severity color (a rail inside the header, not a border-left on cards), the group name, and a tabular count.
- Times and counts use tabular numerals (`tabular-nums`; `<time>` gets it globally).

## Components

- Controls: `rounded-control` (6px); panels: `rounded-card` (12px) with `shadow-card` **or** a border, never both.
- Buttons: primary = `bg-primary text-primary-contrast`; secondary = `border border-border-strong bg-surface text-text hover:bg-surface-sunken`; quiet = text button. Height 36px (`h-9 px-3 text-sm font-medium`). Every button has hover, focus-visible, active, disabled.
- Filter chips: toggle buttons with `aria-pressed`; pressed = `bg-primary-soft text-primary border-primary/40`.
- Loading: skeleton rows, not spinners in the middle of content. Empty states teach what to do next.
- Focus ring: the global 2px primary outline with 2px offset. Do not remove it.

## Motion

150 to 250ms, `ease-out-expo`, state changes only (detail panel enter, row highlight on update). Respect `prefers-reduced-motion` (`motion-safe:`).

## Bans

No cards-as-layout grids, no hero metrics, no eyebrow labels above headings, no gradient text, no glass, no colored border-left above 1px, no emoji or unicode as icons (use inline SVG with a 1.5px stroke, 16px, `currentColor`), no modals for tasks that work inline.
