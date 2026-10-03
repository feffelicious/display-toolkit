import { I, G } from './lib.mjs';

export const fly = (inner, { foot = footDefault(), style = '' } = {}) =>
  `<div class="fly" style="${style}"><div class="fbody">${inner}</div>${foot}</div>`;

export const head = ({ hdr = false, meta = 'DisplayPort, 240 Hz', multi = false, open = false } = {}) => `
<div class="fhead">
  ${multi ? `<div class="sw ${open ? 'open' : ''}">` : ''}
  <div class="mg">${I(G.monitor)}</div>
  <div>
    <div class="nm">ROG Swift PG32UCWM</div>
    <div class="meta">${meta}${hdr ? ' <span class="hdrb">HDR</span>' : ''}</div>
  </div>
  ${multi ? `${I(G.chevD, 's12')}</div>` : ''}
  <div class="sp"></div>
</div>`;

export const band = (v, { pending = false, focus = false, failed = false } = {}) => `
<div class="band ${focus ? 'focus' : ''}" role="slider" aria-label="Brightness" aria-valuenow="${v}">
  <div class="bf" style="width:${v}%"></div>
  ${I(G.brightness, 'bi')}
  <div class="bv">${pending ? '<span class="pend" title="Sending to monitor"></span>' : ''}<span class="tnum">${v}</span></div>
</div>`;

export const sliderRow = (icon, v, { label = '' } = {}) => `
<div class="srow" aria-label="${label}">${I(icon)}
  <div class="slider"><div class="rail"></div><div class="fill" style="width:${v}%"></div><div class="thumb" style="left:${v}%"></div></div>
  <span class="sval">${v}</span></div>`;

export const profiles = (names, on, { auto = true, extra = '' } = {}) => `
<div class="prof editable" role="radiogroup" aria-label="Profile">${extra}${names
  .map((n) => `<span class="${n === on ? 'on' : ''}">${n}</span>`)
  .join('')}</div>`;

const hdrGlyph = '<span class="hdrg">HDR</span>';

/**
 * kind: 'toggle' | 'split' (toggle + chevron to sub-page) | 'pick' (opens sub-page) | 'action'
 */
export const tile = ({ icon, label, kind = 'toggle', on = false, pending = false, warn = false, edit = false, lift = false, focus = false, hdr = false }) => {
  const glyph = hdr ? hdrGlyph : I(icon);
  const x = kind === 'split' ? `<span class="x">${I(G.chevR)}</span>` : kind === 'pick' ? `<span class="x">${I(G.chevR)}</span>` : '';
  return `<div class="tile ${on ? 'on' : ''} ${lift ? 'lift' : ''}">
  <div class="tb ${kind === 'pick' ? 'pick' : ''} ${focus ? 'focus' : ''}"><span class="m">${glyph}</span>${x}
  ${pending ? '<span class="dot"><span class="pend" style="display:block;background:currentColor"></span></span>' : ''}</div>
  ${warn ? `<span class="warn">${I(G.warn)}</span>` : ''}
  ${edit ? `<span class="unpin" title="Unpin">${I(G.unpin)}</span>` : ''}
  <div class="tl">${label}</div></div>`;
};

export const slot = (label = '') => `<div class="tile ghost"><div class="slot"></div><div class="tl">${label || '&nbsp;'}</div></div>`;

export const tiles = (arr) => `<div class="tiles">${arr.join('')}</div>`;

export function footDefault(status = 'Night since sunset, 18:21', { editHover = false } = {}) {
  return `<div class="ffoot"><div class="st">${I(G.bolt)}<span>${status}</span></div>
  <span class="fb ${editHover ? 'hov' : ''}" title="Edit quick settings">${I(G.edit)}</span>
  <span class="fb" title="Open Display Toolkit">${I(G.settings)}</span></div>`;
}
export const footEdit = () => `<div class="ffoot" style="padding:0 12px"><div class="st" style="color:var(--text-1)">
  <span class="btn subtle" style="margin-left:-6px;padding:0 10px">${I(G.add)}<span>Add</span></span></div>
  <span class="btn accent wide">Done</span></div>`;
export const footPlain = (status = '') => `<div class="ffoot"><div class="st">${status}</div>
  <span class="fb" title="Open Display Toolkit">${I(G.settings)}</span></div>`;

export const subHead = (title) => `<div class="subh"><span class="fb" title="Back">${I(G.back)}</span><span class="strong">${title}</span></div>`;

export const opt = (label, { sel = false, meta = '', focus = false } = {}) => `<div class="opt ${sel ? 'sel' : ''} ${focus ? 'focus' : ''}"><span>${label}</span><span class="sp"></span>${meta ? `<span class="t3">${meta}</span>` : ''}${sel ? I(G.check, 'ck') : ''}</div>`;
