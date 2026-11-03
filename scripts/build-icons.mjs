// Optional regeneration of app.ico and tray.ico from the SVG sources (ADR 0031).
// Needs Node with Playwright/Chromium and ImageMagick `convert`; the .ico files are committed,
// so normal builds never run this. Usage: node scripts/build-icons.mjs [chromium-executable]
import { execFileSync } from 'node:child_process';
import { mkdtempSync, readFileSync } from 'node:fs';
import { createRequire } from 'node:module';
import { tmpdir } from 'node:os';
import { join } from 'node:path';

const root = new URL('../src/SchoolTimetableWidget.Desktop/Assets/Icons/', import.meta.url).pathname;
const globalRoot = execFileSync('npm', ['root', '-g'], { encoding: 'utf8' }).trim();
const { chromium } = createRequire(globalRoot + '/')('playwright');
const svg = name => readFileSync(join(root, name), 'utf8');
const out = mkdtempSync(join(tmpdir(), 'stw-icons-'));
const browser = await chromium.launch(process.argv[2] ? { executablePath: process.argv[2] } : {});
const page = await browser.newPage();
async function render(source, size) {
  const file = join(out, `${size}-${Math.random().toString(36).slice(2)}.png`);
  await page.setViewportSize({ width: size, height: size });
  await page.setContent(`<style>html,body{margin:0;background:transparent}svg{display:block;width:${size}px;height:${size}px}</style>${source}`);
  await page.screenshot({ path: file, omitBackground: true, clip: { x: 0, y: 0, width: size, height: size } });
  return file;
}
// Small sizes use the 2x2 variant; Explorer and shortcut sizes use the full 3x3 design.
const small = [16, 20, 24], large = [32, 40, 48, 64, 96, 128, 256];
const app = [];
for (const s of small) app.push(await render(svg('tray.svg'), s));
for (const s of large) app.push(await render(svg('app.svg'), s));
const tray = [];
for (const s of [16, 20, 24, 32, 40, 48, 64]) tray.push(await render(svg('tray.svg'), s));
await browser.close();
execFileSync('convert', [...app, join(root, 'app.ico')]);
execFileSync('convert', [...tray, join(root, 'tray.ico')]);
console.log('Wrote app.ico and tray.ico');
