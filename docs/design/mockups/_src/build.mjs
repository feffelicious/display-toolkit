import { writeFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
const out = join(dirname(fileURLToPath(import.meta.url)), '..');
const pages = { 'tray-flyout.html': './page-flyout.mjs', 'main-window.html': './page-main.mjs', 'profiles.html': './page-profiles.mjs', 'index.html': './page-index.mjs' };
for (const [f, m] of Object.entries(pages)) {
  const mod = await import(m);
  writeFileSync(join(out, f), mod.default());
  console.log('wrote', f);
}
