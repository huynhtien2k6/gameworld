'use strict';
const $ = (s, parent = document) => parent.querySelector(s);
const $$ = (s, parent = document) => [...parent.querySelectorAll(s)];
const adminMode = document.body.classList.contains('admin');
const tokenKey = adminMode ? 'gw.admin.token' : 'gw.player.token';
const fmt = n => new Intl.NumberFormat('vi-VN', { maximumFractionDigits: 2 }).format(n ?? 0);
const dateFmt = s => s ? new Date(s.endsWith('Z') || /[+-]\d\d:\d\d$/.test(s) ? s : s + 'Z').toLocaleString('vi-VN') : '—';
function el(tag, props = {}, ...children) {
  const item = document.createElement(tag);
  for (const [key, value] of Object.entries(props)) {
    if (key === 'className') item.className = value;
    else if (key === 'text') item.textContent = value;
    else if (key.startsWith('on') && typeof value === 'function') item.addEventListener(key.slice(2), value);
    else if (value !== null && value !== undefined) item.setAttribute(key, value);
  }
  for (const child of children.flat()) if (child !== null && child !== undefined) item.append(child instanceof Node ? child : document.createTextNode(String(child)));
  return item;
}
function toast(message, failed = false) {
  $('.toast')?.remove();
  const node = el('div', { className: 'toast' + (failed ? ' error' : ''), role: failed ? 'alert' : 'status', text: message });
  document.body.append(node);
  setTimeout(() => node.remove(), failed ? 8000 : 5000);
}
let refreshRequest = null;
async function api(path, data, method = data === undefined ? 'GET' : 'POST', noAuth = false, retry = true) {
  const headers = {};
  if (data !== undefined) headers['Content-Type'] = 'application/json';
  const token = sessionStorage.getItem(tokenKey);
  if (token && !noAuth) headers.Authorization = 'Bearer ' + token;
  const response = await fetch(path, { method, headers, body: data === undefined ? undefined : JSON.stringify(data), cache: 'no-store' });
  const raw = await response.text();
  let result; try { result = raw ? JSON.parse(raw) : null; } catch { result = raw; }
  if (!response.ok) {
    if(response.status === 401 && !noAuth && !adminMode && retry && sessionStorage.getItem('gw.player.refresh')) {
      let refreshed = false;
      try {
        if(!refreshRequest) refreshRequest = api('/api/auth/refresh',{refreshToken:sessionStorage.getItem('gw.player.refresh')},'POST',true,false).then(r => {
          sessionStorage.setItem(tokenKey,r.token); sessionStorage.setItem('gw.player.refresh',r.refreshToken); return r;
        }).finally(() => { refreshRequest = null; });
        await refreshRequest;
        refreshed = true;
      } catch { sessionStorage.removeItem('gw.player.refresh'); }
      if(refreshed) return api(path,data,method,noAuth,false);
    }
    const detail = typeof result === 'string' ? result : result?.message || result?.detail || (result?.errors ? Object.values(result.errors).flat().join('\n') : result?.title);
    if (response.status === 401 && !noAuth) { sessionStorage.removeItem(tokenKey); sessionStorage.removeItem('gw.player.refresh'); $('#auth-overlay')?.classList.remove('hidden'); }
    throw new Error(detail || (response.status === 403 ? 'Bạn không có quyền thực hiện thao tác này.' : 'Không thể thực hiện yêu cầu (' + response.status + ').'));
  }
  return result;
}
async function run(button, action) {
  const text = button?.textContent;
  if (button) { button.disabled = true; button.textContent = 'Đang xử lý…'; }
  try { return await action(); } catch (error) { toast(error.message || 'Không thể kết nối máy chủ.', true); return null; }
  finally { if (button) { button.disabled = false; button.textContent = text; } }
}
function empty(text = 'Chưa có dữ liệu.') { return el('div', { className: 'empty', text }); }
function pill(text, type = '') { return el('span', { className: 'pill ' + type, text }); }
function row(icon, title, subtitle, end) {
  return el('div', { className: 'row' }, el('div', { className: 'row-main' }, el('div', { className: 'row-icon', text: icon }), el('div', {}, el('div', { className: 'row-title', text: title }), el('div', { className: 'row-sub', text: subtitle }))), end);
}
function button(text, action, cls = 'small') { const b = el('button', { className: cls, type: 'button', text }); b.addEventListener('click', () => run(b, action)); return b; }
function table(headers, records) {
  if (!records.length) return empty();
  return el('div', { className: 'table-wrap' }, el('table', {}, el('thead', {}, el('tr', {}, headers.map(x => el('th', { text: x })))), el('tbody', {}, records.map(values => el('tr', {}, values.map(x => el('td', {}, x)))))));
}
function options(select, values, value = 'id', label = 'name') {
  const prior = select.value; select.replaceChildren(...values.map(x => el('option', { value: x[value], text: typeof label === 'function' ? label(x) : x[label] })));
  if ([...select.options].some(x => x.value === prior)) select.value = prior;
}
function value(form, name) { return form.elements.namedItem(name)?.value.trim() ?? ''; }
function num(form, name) { return Number(value(form, name)); }
function setupNav(onView) {
  $$('.nav button[data-view]').forEach(b => b.addEventListener('click', () => {
    $$('.nav button').forEach(x => x.classList.toggle('active', x === b));
    $$('.view-content').forEach(x => x.classList.toggle('hidden', x.id !== 'view-' + b.dataset.view));
    $('#breadcrumb-current').textContent = b.dataset.title || b.textContent.trim();
    $('.sidebar').classList.remove('open');
    onView?.(b.dataset.view);
  }));
  $('.mobile-menu')?.addEventListener('click', () => $('.sidebar').classList.toggle('open'));
}
function formHandler(selector, action) {
  const form = $(selector);
  form?.addEventListener('submit', e => { e.preventDefault(); run($('button[type=submit]', form), () => action(form)); });
}
