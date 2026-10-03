import { I, G, css, page, presCss, presTop } from './lib.mjs';
import { fly, head, band, profiles, tile, tiles } from './flyout.mjs';

const P = ['Desktop', 'Night', 'Gaming', 'Movie'];
const teaser = fly(`${head()}${band(45)}${profiles(P, 'Night')}${tiles([
  tile({ icon: G.palette, label: 'Scenery', kind: 'pick' }),
  tile({ hdr: true, label: 'HDR', kind: 'split' }),
  tile({ icon: G.moon, label: 'Blue light', kind: 'split', on: true }),
  tile({ icon: G.shadow, label: 'Shadow boost', kind: 'split' }),
  tile({ icon: G.target, label: 'Crosshair', kind: 'split' }),
  tile({ icon: G.focus, label: 'Target mode' }),
])}`);

const idxCss = `
.ihero{display:grid;grid-template-columns:1fr 520px;gap:56px;align-items:center;margin-top:8px}
.ihero .pic{height:560px;border-radius:12px;display:flex;align-items:flex-end;justify-content:flex-end;padding:0 24px 24px 0;box-shadow:0 0 0 1px rgba(0,0,0,.08),0 12px 32px rgba(0,0,0,.12);background:
  radial-gradient(420px 320px at 15% 95%,rgba(64,128,220,.55),transparent 70%),
  radial-gradient(380px 360px at 60% 35%,rgba(166,196,242,.75),transparent 70%),
  linear-gradient(165deg,#E7EDF8,#B9CDEE 70%,#9DB9E8)}
.ihero h1{font-family:"Segoe UI Variable Display","Segoe UI Variable","Segoe UI",sans-serif;font-size:46px;line-height:56px;font-weight:600;letter-spacing:-.01em;max-width:560px}
.ihero p{font-size:16px;line-height:26px;color:rgba(0,0,0,.62);margin-top:18px;max-width:520px}
.links{display:grid;grid-template-columns:repeat(4,1fr);gap:16px;margin-top:64px}
.links a{display:block;padding:20px;border-radius:8px;background:rgba(255,255,255,.7);border:1px solid rgba(0,0,0,.06);color:inherit;text-decoration:none}
.links a:hover{background:#fff}
.links a .ico{font-size:20px;width:20px;height:20px;color:#005FB8;margin-bottom:14px}
.links a .t{font-weight:600}
.links a .d{font-size:12px;line-height:18px;color:rgba(0,0,0,.62);margin-top:4px}
.princ{display:grid;grid-template-columns:repeat(3,1fr);gap:32px;margin-top:64px;padding-top:32px;border-top:1px solid rgba(0,0,0,.08)}
.princ h3{font-size:14px;font-weight:600}
.princ p{font-size:14px;color:rgba(0,0,0,.62);margin-top:6px}
`;

export default () => page({
  title: 'Display Toolkit Design',
  desc: 'Index of the Display Toolkit design mockups and spec.',
  cssText: css('base.css') + css('flyout.css') + presCss + idxCss,
  body: `<div class="pres light">${presTop('index.html')}
<main class="pwrap">
<div class="ihero">
  <div><h1>Your monitor’s settings, one click from the taskbar.</h1>
  <p>Display Toolkit replaces ASUS DisplayWidget Center with a small, native Windows 11 app. Brightness, profiles, picture mode and HDR live in a tray flyout; everything else is in a settings window that looks like it shipped with Windows.</p>
  <p style="font-size:14px;margin-top:22px">Mockups need Windows 11 for Segoe UI Variable and Segoe Fluent Icons. View at 100% zoom.</p></div>
  <div class="pic light">${teaser}</div>
</div>
<div class="links">
  <a href="tray-flyout.html">${I(G.monitor)}<div class="t">Tray flyout</div><div class="d">Default, edit mode, sub-pages, states, hotkey indicator. Light and dark.</div></a>
  <a href="main-window.html">${I(G.sliders)}<div class="t">Main window</div><div class="d">Display, six-axis color, OLED care, GamePlus, Settings.</div></a>
  <a href="profiles.html">${I(G.bolt)}<div class="t">Profiles &amp; automation</div><div class="d">Profile editor, day strip, rules and the add-rule dialog.</div></a>
  <a href="../design-spec.md">${I(G.edit)}<div class="t">Design spec</div><div class="d">Principles, structure, states, tokens, motion and WPF notes.</div></a>
</div>
<div class="princ">
  <div><h3>The flyout is the product</h3><p>Ninety percent of use is a brightness drag, a profile tap or an HDR flip. Those take one click and no scrolling.</p></div>
  <div><h3>Only what this monitor can do</h3><p>Every control comes from the monitor’s own capability report. Unsupported features are hidden, never greyed out.</p></div>
  <div><h3>Instant, then confirmed</h3><p>Controls move immediately and the monitor catches up. Problems show up on the control that failed, in words.</p></div>
</div>
</main></div>`,
});
