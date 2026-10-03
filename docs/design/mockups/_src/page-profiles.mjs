import { I, G, css, page, presCss, presTop } from './lib.mjs';
import { win, slider, combo, seg, toggle, sc } from './win.mjs';

const plist = `<div class="plist">
  ${[['Desktop', G.monitor, 'Sunrise rule'], ['Night', G.moon, 'Sunset rule'], ['Gaming', G.gamepad, 'Full-screen games'], ['Movie', G.film, 'Ctrl + Alt + 4']]
    .map(([n, g, s]) => `<div class="pli ${n === 'Night' ? 'sel' : ''}"><span class="pg">${I(g)}</span><div><div>${n}</div><div class="sub">${s}</div></div></div>`).join('')}
  <div class="pli new"><span class="pg" style="background:transparent">${I(G.add)}</span><span>New profile</span></div>
</div>`;

const irow = (on, name, v) => `<div class="irow ${on ? '' : 'off'}"><span class="check ${on ? 'on' : ''}">${on ? I(G.check) : ''}</span><span class="nm">${name}</span><span class="v">${v}</span></div>`;

const editor = `<div class="ped">
  <div class="top"><span class="bigg">${I(G.moon)}</span>
    <span class="textbox" style="width:220px">Night</span>
    <span style="flex:1"></span>
    <span class="btn">Capture current</span><span class="btn accent">Apply now</span><span class="btn icon subtle">${I(G.more)}</span></div>
  <div class="fields">
    <div class="field"><span>Shortcut</span><span class="kbds"><span class="kbd">Ctrl</span><span class="kbd">Alt</span><span class="kbd">2</span></span></div>
    <div class="field"><span>Show in quick settings</span><span class="toggle on"></span></div>
  </div>
  <div class="incl"><div class="ih"><span class="strong">Settings in this profile</span><span class="caption t2">Unchecked settings stay as they are</span></div>
    ${irow(true, 'Brightness', slider(45, 180))}
    ${irow(true, 'Picture mode', combo('Scenery', 150))}
    ${irow(true, 'Color temperature', combo('5000K', 150))}
    ${irow(true, 'Blue light filter', seg(['Off', '1', '2', '3', '4'], '2'))}
    ${irow(false, 'Contrast', slider(80, 180))}
    ${irow(false, 'HDR', '<span class="tg"><span class="lab">Off</span><span class="toggle"></span></span>')}
    ${irow(false, 'Shadow boost', seg(['Off', '1', '2', '3', '4'], 'Off'))}
    <div class="morel" style="padding-left:12px">${I(G.chevD, 's12')}<span>Show 12 more settings</span></div>
  </div>
</div>`;

// Timeline: 0..24h mapped to %. Sunrise 07:12 (30%), sunset 18:21 (76.46%), now 20:47 (86.6%).
const pct = (h, m) => (((h * 60 + m) / 1440) * 100).toFixed(2);
const timeline = `<div class="timeline">
  <div class="tl-head"><div><div class="strong">Today</div><div class="caption t2">Night is active, set by the sunset rule at 18:21</div></div><span class="sp"></span><span class="btn">${I(G.pause)}<span>Pause automation</span></span></div>
  <div class="tl-track">
    <span class="tl-mark" style="left:${pct(7, 12)}%">${I(G.sun, 's12')}07:12</span>
    <span class="tl-mark" style="left:${pct(18, 21)}%">${I(G.sun, 's12')}18:21</span>
    <div class="tl-seg n past" style="left:0;width:calc(${pct(7, 12)}% - 2px)">${I(G.moon, 's12')}Night</div>
    <div class="tl-seg d" style="left:${pct(7, 12)}%;width:calc(${(pct(18, 21) - pct(7, 12)).toFixed(2)}% - 2px)">${I(G.monitor, 's12')}Desktop</div>
    <div class="tl-seg n" style="left:${pct(18, 21)}%;right:0">${I(G.moon, 's12')}Night</div>
    <div class="tl-now" style="left:${pct(20, 47)}%"><span>Now</span></div>
  </div>
  <div class="tl-axis"><span style="left:0">00:00</span><span style="left:25%">06:00</span><span style="left:50%">12:00</span><span style="left:75%">18:00</span><span style="left:100%">24:00</span></div>
  <div class="tl-over"><span class="hatch"></span>App rules interrupt the day while the app runs, then the schedule takes over again.</div>
</div>`;

const rule = ({ icon, title, desc, prof, pIcon, on = true }) => sc({
  icon: `<span class="grip">${I(G.grip, 's12')}</span>${I(icon, 'lead')}`,
  title, desc, cls: on ? '' : 'disabled',
  act: `<span class="pchip">${I(pIcon, 's12')}<span>${prof}</span></span><span class="toggle ${on ? 'on' : ''}"></span><span class="btn icon subtle">${I(G.more)}</span>`,
});

const rules = `<section class="group rules"><div style="display:flex;align-items:flex-end;justify-content:space-between;margin-bottom:8px"><div><h3 style="font-size:14px;font-weight:600">Rules</h3><div class="caption t2">Higher rules win when two apply at the same time. Drag to reorder.</div></div><span class="btn">${I(G.add)}<span>Add rule</span></span></div>
<div class="stack">
  ${rule({ icon: G.gamepad, title: 'A game is running in full screen', desc: 'Ends when the game closes', prof: 'Gaming', pIcon: G.gamepad })}
  ${rule({ icon: G.apps, title: 'VLC media player is open', desc: 'Ends when VLC closes', prof: 'Movie', pIcon: G.film })}
  ${rule({ icon: G.sun, title: 'At sunset', desc: 'Today at 18:21 in Stockholm', prof: 'Night', pIcon: G.moon })}
  ${rule({ icon: G.sun, title: 'At sunrise', desc: 'Tomorrow at 07:14', prof: 'Desktop', pIcon: G.monitor })}
  ${rule({ icon: G.clock, title: 'Weekdays at 08:00', desc: 'Turned off', prof: 'Desktop', pIcon: G.monitor, on: false })}
</div></section>`;

const profilesPage = `
<div class="page-title title">Profiles &amp; automation</div>
<section class="group" style="margin-top:0"><h3>Profiles</h3>
<div class="pm">${plist}${editor}</div></section>
<section class="group"><h3>Automation</h3>${timeline}</section>
${rules}`;

/* ---------- Add-rule dialog, two steps ---------- */
const trig = (icon, t, d, sel = false) => `<div class="tc ${sel ? 'sel' : ''}">${I(icon)}<div><div>${t}</div><div class="d">${d}</div></div></div>`;
const dlgStage = (theme, inner, h = 640) => win({ theme, sel: 'profiles', content: profilesPage, height: h, overlay: `<div class="smoke">${inner}</div>` });

const step1 = `<div class="dialog" style="width:560px"><div class="dbody">
  <div class="subtitle">Add a rule</div><p class="t2" style="margin-bottom:14px">When should a profile switch on?</p>
  <div class="trig">
    ${trig(G.apps, 'An app is open', 'Pick a running app or browse for one')}
    ${trig(G.gamepad, 'A game is in full screen', 'Any game, detected automatically')}
    ${trig(G.clock, 'At a time', 'Daily or on chosen days')}
    ${trig(G.sun, 'At sunrise or sunset', 'Follows your location, with an offset', true)}
    ${trig(G.battery, 'Power source changes', 'Plugged in or on battery (laptops)')}
    <div class="tc">${'<span class="hdrg" style="margin-top:2px">HDR</span>'}<div><div>HDR turns on or off</div><div class="d">Windows HDR or an HDR signal</div></div></div>
  </div>
  <p class="caption t2" style="margin-top:14px">Want a key combination instead? Set a shortcut on the profile itself.</p>
</div><div class="dfoot"><span class="btn accent">Next</span><span class="btn">Cancel</span></div></div>`;

const step2 = `<div class="dialog" style="width:520px"><div class="dbody">
  <div class="subtitle">At sunrise or sunset</div>
  <div class="flab" style="margin-top:4px">When</div><span class="seg" style="display:flex"><span style="flex:1">${I(G.sun, 's12')}Sunrise</span><span class="on" style="flex:1">${I(G.moon, 's12')}Sunset</span></span>
  <div style="display:flex;gap:16px">
    <div><div class="flab">Offset</div><span class="numbox"><span>−30 min</span><span class="sp"><span>${I(G.chevU, 's10')}</span><span>${I(G.chevD, 's10')}</span></span></span></div>
    <div style="flex:1"><div class="flab">Use profile</div>${combo('Night', 0).replace('min-width:0px', 'width:100%')}</div>
  </div>
  <div class="flab">Days</div>
  <div class="days">${['M', 'T', 'W', 'T', 'F', 'S', 'S'].map((d) => `<span class="on">${d}</span>`).join('')}</div>
  <div class="sunline">${I(G.globe, 's12')}<span>Stockholm, from Windows location. Today this runs at 17:51.</span><span style="flex:1"></span><span class="link" style="font-size:12px">Change</span></div>
</div><div class="dfoot"><span class="btn accent">Save rule</span><span class="btn">Back</span></div></div>`;

export default () => page({
  title: 'Profiles Automation',
  desc: 'Display Toolkit profiles editor and automation rules mockups.',
  cssText: css('base.css') + css('flyout.css') + css('main.css') + presCss,
  body: `<div class="pres light">${presTop('profiles.html')}
<main class="pwrap">
<div class="pintro"><h1 class="title">Profiles &amp; automation</h1>
<p>A profile is a named set of settings. Rules switch profiles for you. The day strip shows what the rules will do today, so nobody has to simulate a schedule in their head.</p></div>
<div class="frames">
  <div><p class="frame-cap"><b>Profiles and rules, light.</b> Master–detail editor; each profile includes only the settings that are checked. Rules read as plain sentences with the profile as a chip.</p>
  ${win({ theme: 'light', sel: 'profiles', content: profilesPage })}</div>
  <div style="display:grid;grid-template-columns:1fr;gap:56px">
  <div><p class="frame-cap"><b>Add rule, step 1, dark.</b> Trigger types as cards. Only triggers that make sense on this PC are shown (power source only on devices with a battery).</p>
  ${dlgStage('dark', step1, 660)}</div>
  <div><p class="frame-cap"><b>Add rule, step 2, light.</b> Sun rules show the resolved time for today so the offset is concrete.</p>
  ${dlgStage('light', step2, 620)}</div>
  </div>
</div>
</main></div>`,
});
