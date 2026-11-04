// A stable hue (0-359) per tenant id, so each tenant always gets the same colour cue without a colour table.
// FNV-1a spreads similar ids (tenant-1, tenant-2) across the colour wheel.
export function tenantHue(tenantId: string): number {
  let hash = 0x811c9dc5;
  for (const character of tenantId) {
    hash ^= character.codePointAt(0) ?? 0;
    hash = Math.imul(hash, 0x01000193) >>> 0;
  }
  return hash % 360;
}
