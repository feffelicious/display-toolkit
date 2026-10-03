import { I, G } from './lib.mjs';

export const mill = (w) => `<div class="mill" style="width:${w}px;height:${Math.round(w * 0.7)}px"><div class="scr"><i></i></div><div class="neck"></div><div class="foot"></div></div>`;

const NAV = [
  ['display', G.monitor, 'Display'],
  ['profiles', G.bolt, 'Profiles & automation'],
  ['oled', G.health, 'OLED care'],
  ['gameplus', G.target, 'GamePlus'],
];

export const nav = (sel) => `<div class="nav">
  <div class="dev">${mill(64)}<div style="min-width:0"><div class="strong">PG32UCWM</div><div class="caption t2">ROG Swift, DisplayPort</div></div></div>
  <div class="search"><span>Find a setting</span>${I(G.search, 's12')}</div>
  ${NAV.map(([k, g, t]) => `<div class="item ${k === sel ? 'sel' : ''}">${I(g)}<span>${t}</span></div>`).join('')}
  <div class="sp"></div>
  <div class="item ${sel === 'settings' ? 'sel' : ''}">${I(G.settings)}<span>Settings</span></div>
</div>`;

export const win = ({ theme, sel, content, height, width = 1200, overlay = '' }) => `<div class="${theme}" style="width:${width}px">
<div class="window" style="width:${width}px;${height ? `height:${height}px` : ''}">
  <div class="titlebar"><div class="appid"><span class="appicon"></span><span>Display Toolkit</span></div>
  <div class="caps"><span>${I(G.min)}</span><span>${I(G.max)}</span><span>${I(G.close)}</span></div></div>
  <div class="shell">${nav(sel)}<div class="content">${content}</div></div>
  ${overlay}
</div></div>`;

export const slider = (v, w = 220, { val = true, suffix = '' } = {}) => `<div class="slider" style="width:${w}px"><div class="rail"></div><div class="fill" style="width:${v}%"></div><div class="thumb" style="left:${v}%"></div></div>${val ? `<span class="sval">${v}${suffix}</span>` : ''}`;
export const combo = (t, w = 200) => `<span class="combo" style="min-width:${w}px"><span>${t}</span>${I(G.chevD)}</span>`;
export const seg = (items, on) => `<span class="seg">${items.map((t) => `<span class="${t === on ? 'on' : ''}">${t}</span>`).join('')}</span>`;
export const toggle = (on, label = true) => `<span class="tg">${label ? `<span class="lab">${on ? 'On' : 'Off'}</span>` : ''}<span class="toggle ${on ? 'on' : ''}"></span></span>`;

/** Settings card. icon may be raw HTML (starting with '<') or a glyph code. */
export const sc = ({ icon, title, desc = '', act = '', cls = '', chev = '' }) => `<div class="sc ${cls}">
  ${icon ? (icon.startsWith('<') ? icon : I(icon, 'lead')) : ''}
  <div class="hd"><div>${title}</div>${desc ? `<div class="d">${desc}</div>` : ''}</div>
  <div class="act">${act}${chev ? I(chev, 'chev') : ''}</div></div>`;

export const group = (title, cards) => `<section class="group">${title ? `<h3>${title}</h3>` : ''}<div class="stack">${cards.join('')}</div></section>`;

export const hdrIcon = '<span class="lead" style="width:20px;margin-right:4px;display:flex;justify-content:center"><span class="hdrg" style="height:15px">HDR</span></span>';
