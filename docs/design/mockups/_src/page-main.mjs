import { I, G, css, page, presCss, presTop } from './lib.mjs';
import { win, mill, slider, combo, seg, toggle, sc, group, hdrIcon } from './win.mjs';

const dot = (c) => `<span class="dot-c" style="background:${c}"></span>`;

const hero = ({ hdr = false } = {}) => `<div class="hero">
  ${mill(150)}
  <div class="hd">
    <div class="subtitle">ROG Swift PG32UCWM</div>
    <div class="t2">32-inch QD-OLED, 3840 × 2160, 240 Hz</div>
    <div class="facts"><span><i class="ok"></i>Connected</span><span>${I(G.bolt, 's12')}${hdr ? 'Gaming, set by a game rule' : 'Night, set at sunset'}</span>${hdr ? '<span><span class="hdrb">HDR</span>HDR10</span>' : ''}</div>
  </div>
  <div class="hact"><span class="caption t2">Input source</span>${combo('DisplayPort', 180)}</div>
</div>`;

/* ---------- Display, light, full length ---------- */
const displayLight = `
<div class="page-title title">Display</div>
${hero()}
${group('Picture', [
  sc({ icon: G.brightness, title: 'Brightness', act: slider(45, 260) }),
  sc({ icon: G.contrast, title: 'Contrast', act: slider(80, 260) }),
  sc({ icon: G.palette, title: 'Picture mode', desc: 'Each mode keeps its own color settings', act: combo('Scenery') }),
  sc({ icon: hdrIcon, title: 'Use HDR', desc: 'Windows HDR for this display. HDR presets replace picture modes while it’s on.', act: toggle(false) }),
  `<div style="height:6px"></div>`,
  sc({ icon: G.thermo, title: 'Color', desc: 'Temperature, gamma, saturation and sharpness for Scenery', cls: 'exp-top', chev: G.chevU }),
  sc({ title: 'Color temperature', cls: 'sub', act: combo('User') }),
  sc({ icon: dot('#E5484D'), title: 'Red', cls: 'sub', act: slider(100, 220) }),
  sc({ icon: dot('#30A46C'), title: 'Green', cls: 'sub', act: slider(96, 220) }),
  sc({ icon: dot('#3E7BFA'), title: 'Blue', cls: 'sub', act: slider(92, 220) }),
  sc({ title: 'Gamma', cls: 'sub', act: seg(['1.8', '2.0', '2.2', '2.4', '2.6'], '2.2') }),
  sc({ title: 'Saturation', cls: 'sub', act: slider(50, 220) }),
  sc({ title: 'Sharpness', cls: 'sub', act: slider(20, 220) }),
  sc({ title: 'Reset Scenery mode', desc: 'Restores the factory color settings for this mode', cls: 'sub last', act: '<span class="btn">Reset</span>' }),
  `<div style="height:6px"></div>`,
  sc({ icon: G.sliders, title: 'Six-axis color', desc: 'Fine-tune hue and saturation for red, yellow, green, cyan, blue and magenta', chev: G.chevR }),
])}
${group('Gaming and comfort', [
  sc({ icon: G.moon, title: 'Blue light filter', desc: 'Level 4 meets TÜV low blue light', act: seg(['Off', '1', '2', '3', '4'], '2') }),
  sc({ icon: G.shadow, title: 'Shadow boost', desc: 'Lifts dark areas without washing out highlights', act: seg(['Off', '1', '2', '3', '4'], 'Off') }),
  sc({ icon: G.vrr, title: 'Variable refresh rate', desc: 'Adaptive-Sync and G-SYNC Compatible', act: toggle(true) }),
  sc({ icon: G.bolt, title: 'Frame-rate boost', act: toggle(false) }),
])}
${group('Sound', [
  sc({ icon: G.volume, title: 'Volume', desc: 'Headphone jack and DisplayPort audio', act: `<span class="btn icon subtle">${I(G.volume)}</span>${slider(40, 220)}` }),
])}
<section class="group related"><h3>Related</h3><div class="stack">
  ${sc({ title: 'Windows display settings', desc: 'Resolution, scale, refresh rate', chev: G.open })}
  ${sc({ title: 'Monitor information', desc: 'Capabilities, firmware and DDC/CI details', chev: G.chevR })}
</div></section>`;

/* ---------- Display, dark, HDR on (first screen) ---------- */
const displayDark = `
<div class="page-title title">Display</div>
${hero({ hdr: true })}
${group('Picture', [
  sc({ icon: G.brightness, title: 'Brightness', act: slider(80, 260) }),
  sc({ icon: hdrIcon, title: 'Use HDR', desc: 'Windows HDR for this display', act: toggle(true), cls: 'exp-top' }),
  sc({ title: 'HDR mode', desc: 'Dolby Vision presets appear while Dolby Vision content is playing', cls: 'sub last', act: combo('Gaming HDR') }),
])}
<div class="infobar info" style="margin-top:14px">${I(G.info, 'lead')}<div>Picture mode and color settings are managed by the monitor while HDR is on. They come back when you turn HDR off.</div></div>
${group('Gaming and comfort', [
  sc({ icon: G.shadow, title: 'Shadow boost', desc: 'Lifts dark areas without washing out highlights', act: seg(['Off', '1', '2', '3', '4'], '1') }),
  sc({ icon: G.vrr, title: 'Variable refresh rate', desc: 'Adaptive-Sync and G-SYNC Compatible', act: toggle(true) }),
])}`;

/* ---------- Six-axis color (sketch) ---------- */
const axisRows = [['Red', '#E5484D', 50, 55], ['Yellow', '#F5C518', 50, 50], ['Green', '#30A46C', 46, 50], ['Cyan', '#2EC4D6', 50, 50], ['Blue', '#3E7BFA', 52, 60], ['Magenta', '#D84FD8', 50, 50]];
const sixAxis = `
<div class="page-title crumb"><span class="title t2">Display</span><span class="sep">${I(G.chevR, 's12')}</span><span class="title">Six-axis color</span></div>
<p class="t2" style="max-width:640px;margin:-8px 0 22px">Adjusts how each color is reproduced in Scenery mode. Changes are saved in the monitor.</p>
<div class="sc" style="display:block;padding:16px 20px">
  <div class="axis"><div class="h">Color</div><div class="h">Hue</div><div class="h">Saturation</div>
  ${axisRows.map(([n, c, h, s]) => `<div class="r"><div class="nm">${dot(c)}<span>${n}</span></div><div class="cell">${slider(h, 200)}</div><div class="cell">${slider(s, 200)}</div></div>`).join('')}
  </div>
</div>
<div style="display:flex;justify-content:flex-end;margin-top:16px"><span class="btn">Reset six-axis color</span></div>`;

/* ---------- OLED care + pixel cleaning dialog ---------- */
const oled = `
<div class="page-title title">OLED care</div>
<div class="clean"><div class="gl">${I(G.sparkle)}</div>
  <div style="flex:1"><div class="strong">Pixel cleaning</div><div class="caption t2" style="margin-top:2px;max-width:440px">Refreshes the panel to reduce temporary image retention. The screen goes dark for about 6 minutes.</div></div>
  <span class="btn">Run pixel cleaning</span></div>
${sc({ title: 'Remind me after', desc: 'The monitor shows a reminder after this much use', cls: 'sub last', act: combo('4 hours', 160) })}
${group('Panel protection', [
  sc({ icon: G.health, title: 'Screen dimming control', desc: 'Lowers brightness when the picture stays still', act: toggle(true) }),
  sc({ title: 'Logo detection', desc: 'Dims static logos and HUD elements', act: toggle(true), cls: 'compact', icon: G.eye }),
  sc({ title: 'Taskbar detection', desc: 'Dims the Windows taskbar area', act: toggle(true), cls: 'compact', icon: G.apps }),
  sc({ title: 'Outer dimming', act: toggle(false), cls: 'compact', icon: G.focus }),
  sc({ title: 'Global dimming', act: toggle(false), cls: 'compact', icon: G.brightness }),
  sc({ title: 'Uniform brightness', desc: 'Keeps brightness steady when the picture changes', act: toggle(false), cls: 'compact', icon: G.contrast }),
  sc({ title: 'OLED anti-flicker', act: toggle(true), cls: 'compact', icon: G.eye }),
  sc({ icon: G.move, title: 'Screen move', desc: 'Shifts the picture slightly over time', act: seg(['Off', 'Light', 'Middle', 'Strong'], 'Light') }),
])}
${group('Proximity sensor', [
  sc({ icon: G.radar, title: 'Turn the screen off when I leave', act: combo('After 10 minutes', 180) }),
  sc({ title: 'Detection distance', cls: 'compact', icon: '<span style="width:24px"></span>', act: seg(['Near', 'Medium', 'Far'], 'Medium') }),
])}`;

const cleanDialog = `<div class="smoke"><div class="dialog">
  <div class="dbody"><div class="subtitle">Run pixel cleaning now?</div>
  <p>The screen turns off for about 6 minutes while the panel refreshes. Leave the monitor plugged in and don't turn it off.</p>
  <p class="t2" style="margin-top:12px">Display Toolkit can't change any settings until cleaning finishes.</p></div>
  <div class="dfoot"><span class="btn accent">Run pixel cleaning</span><span class="btn">Cancel</span></div>
</div></div>`;

/* ---------- GamePlus ---------- */
const xh = [
  '',
  '<path d="M14 3v7M14 18v7M3 14h7M18 14h7"/>',
  '<circle cx="14" cy="14" r="2.6" fill="currentColor" stroke="none"/>',
  '<path d="M14 4v6M14 18v6M4 14h6M18 14h6"/><circle cx="14" cy="14" r="1.6" fill="currentColor" stroke="none"/>',
  '<circle cx="14" cy="14" r="8"/><circle cx="14" cy="14" r="1.6" fill="currentColor" stroke="none"/>',
  '<circle cx="14" cy="14" r="9"/><path d="M14 2v6M14 20v6M2 14h6M20 14h6"/>',
  '<path d="M4 14h7M17 14h7M14 17v7"/>',
  '<path d="M14 9v10M9 14h10"/>',
  '<path d="M8 18l6-6 6 6"/>',
  '<path d="M6 10V6h4M18 6h4v4M22 18v4h-4M10 22H6v-4"/>',
];
const xgrid = `<div class="xgrid">${xh.map((p, i) => `<div class="xc ${i === 3 ? 'sel' : ''}">${i === 0 ? 'Off' : `<svg viewBox="0 0 28 28" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round">${p}</svg>`}</div>`).join('')}</div>`;

const gameplus = `
<div class="page-title title">GamePlus</div>
<p class="t2" style="max-width:640px;margin:-8px 0 4px">Overlays drawn by the monitor. They work with any input and don't appear in screenshots or recordings.</p>
${group('', [
  sc({ icon: G.target, title: 'Crosshair', desc: 'Shortcut: Ctrl + Alt + C', cls: 'exp-top', act: toggle(true), chev: G.chevU }),
  `<div class="sc sub last" style="padding:16px 16px 18px 56px">${xgrid}</div>`,
])}
${group('', [
  sc({ icon: G.fps, title: 'FPS counter', act: seg(['Off', 'Number', 'Bar'], 'Number') }),
  sc({ icon: G.timer, title: 'Timer', desc: 'Counts down on screen, then hides', act: combo('Off', 160) }),
  sc({ icon: G.align, title: 'Display alignment', desc: 'Shows grid lines for lining up multiple monitors', act: toggle(false) }),
  sc({ icon: G.move, title: 'Overlay position', desc: 'Where the FPS counter and timer appear', act: `<div class="posgrid"><span class="sel"></span><span></span><span></span><span></span><span class="c">${I(G.monitor, 's12')}</span><span></span><span></span><span></span><span></span></div>` }),
])}`;

/* ---------- Settings ---------- */
const kb = (...k) => `<span class="kbds">${k.map((x) => `<span class="kbd">${x}</span>`).join('')}</span>`;
const settings = `
<div class="page-title title">Settings</div>
${group('General', [
  sc({ icon: G.power, title: 'Start with Windows', desc: 'Runs quietly in the notification area', act: toggle(true) }),
  sc({ icon: G.palette, title: 'App theme', act: combo('Use system setting') }),
  sc({ icon: G.brightness, title: 'Show changes from shortcuts', desc: 'A small indicator above the taskbar', act: toggle(true) }),
])}
<section class="group keys"><h3>Keyboard shortcuts</h3><div class="stack">
  ${sc({ title: 'Brightness up', act: `${kb('Ctrl', 'Alt', 'Up')}<span class="btn icon subtle">${I(G.edit)}</span>` })}
  ${sc({ title: 'Brightness down', act: `${kb('Ctrl', 'Alt', 'Down')}<span class="btn icon subtle">${I(G.edit)}</span>` })}
  ${sc({ title: 'Brightness step', act: combo('5', 100) })}
  ${sc({ title: 'Next profile', act: `${kb('Ctrl', 'Alt', 'P')}<span class="btn icon subtle">${I(G.edit)}</span>` })}
  ${sc({ title: 'Crosshair on or off', act: `${kb('Ctrl', 'Alt', 'C')}<span class="btn icon subtle">${I(G.edit)}</span>` })}
  ${sc({ title: 'Target mode on or off', act: `${kb('Ctrl', 'Alt', 'T')}<span class="btn icon subtle">${I(G.edit)}</span>` })}
  ${sc({ title: 'Add a shortcut', desc: 'Picture mode, input source, HDR, any profile', act: `<span class="btn">${I(G.add)}<span>Add</span></span>` })}
</div></section>
${group('Target mode', [
  sc({ icon: G.focus, title: 'Dim other windows', desc: 'Everything except the active window is darkened by the app, not the monitor', act: slider(70, 200, { suffix: '%' }) }),
])}
${group('Monitor communication', [
  sc({ icon: G.history, title: 'Delay between commands', desc: 'Raise this if settings sometimes don’t stick', act: `<span class="numbox" style="width:120px"><span>20 ms</span><span class="sp"><span>${I(G.chevU, 's10')}</span><span>${I(G.chevD, 's10')}</span></span></span>` }),
  sc({ icon: G.refresh, title: 'Read settings from the monitor', desc: 'Use after changing settings with the monitor’s own buttons', act: '<span class="btn">Refresh</span>' }),
  sc({ icon: G.layers, title: 'Back up settings', desc: 'Profiles, rules, shortcuts and quick settings layout', act: '<span class="btn">Export</span><span class="btn">Import</span>' }),
])}
${group('About', [
  sc({ icon: '<span class="appicon" style="width:20px;height:20px;margin-right:4px"></span>', title: 'Display Toolkit 1.0', desc: 'Open source, MIT license. Not affiliated with ASUS.', act: '<span class="link">View on GitHub</span>' }),
])}`;

export default () => page({
  title: 'Main Window',
  desc: 'Display Toolkit main window mockups: Display, Six-axis color, OLED care, GamePlus and Settings.',
  cssText: css('base.css') + css('flyout.css') + css('main.css') + presCss,
  body: `<div class="pres light">${presTop('main-window.html')}
<main class="pwrap">
<div class="pintro"><h1 class="title">Main window</h1>
<p>Where the 10% lives: color tuning, OLED care, GamePlus, profiles and app settings. A standard Windows 11 settings layout with Mica, a left navigation pane and settings cards, so it feels native on day one.</p></div>
<div class="frames">
  <div><p class="frame-cap"><b>Display, light, full page.</b> Common controls first; color tuning collapses into one expander; six-axis is a page of its own. Every row is hidden if the monitor doesn’t report the feature.</p>
  ${win({ theme: 'light', sel: 'display', content: displayLight })}</div>
  <div><p class="frame-cap"><b>Display, dark, HDR signal.</b> Picture mode becomes HDR mode, and the color group disappears instead of being greyed out. The info bar explains where it went.</p>
  ${win({ theme: 'dark', sel: 'display', content: displayDark, height: 780 })}</div>
  <div><p class="frame-cap"><b>Six-axis color.</b> Second-level page with a breadcrumb title, the Windows 11 Settings pattern.</p>
  ${win({ theme: 'light', sel: 'display', content: sixAxis, height: 640 })}</div>
  <div><p class="frame-cap"><b>OLED care, with the pixel cleaning confirmation.</b> The only destructive-feeling action in the app gets a modal dialog with plain consequences and a specific button label.</p>
  ${win({ theme: 'light', sel: 'oled', content: oled, height: 1120, overlay: cleanDialog })}</div>
  <div><p class="frame-cap"><b>GamePlus, dark.</b> Visual picker for crosshair styles; position as a miniature screen.</p>
  ${win({ theme: 'dark', sel: 'gameplus', content: gameplus, height: 760 })}</div>
  <div><p class="frame-cap"><b>Settings, dark.</b> Shortcuts are shown as keycaps and edited in place.</p>
  ${win({ theme: 'dark', sel: 'settings', content: settings })}</div>
</div>
</main></div>`,
});
