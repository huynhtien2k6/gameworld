'use strict';
(() => {
  const catalog = window.GameWorldAppearances;
  const authDialog = $('#auth-dialog'), journeyDialog = $('#journey-dialog');
  const favoriteKey = 'gw.art.favorites', selectionKey = 'gw.art.selected';
  let favorites = new Set(), selected = catalog[5], filter = 'all', account = null, owned = [];
  let register = false, pendingJourney = false;
  const readLocal = key => { try { return localStorage.getItem(key); } catch { return null; } };
  const saveLocal = (key, data) => { try { localStorage.setItem(key, data); } catch { /* Private browsing can disable persistence. */ } };
  try {
    const stored = JSON.parse(readLocal(favoriteKey) || '[]');
    if (Array.isArray(stored)) favorites = new Set(stored.filter(id => catalog.some(p => p.id === id)));
  } catch { /* Ignore an outdated or invalid preference. */ }
  selected = catalog.find(p => p.id === readLocal(selectionKey)) || selected;
  function filtered() {
    return catalog.filter(p => filter === 'all' || (filter === 'favorites' ? favorites.has(p.id) : String(p.classId) === filter));
  }
  function showError(id, message = '') {
    $(id).textContent = message;
    $(id).classList.toggle('hidden', !message);
  }
  function select(profile, announce = true) {
    selected = profile;
    saveLocal(selectionKey, profile.id);
    document.documentElement.style.setProperty('--accent', profile.color);
    $('#character-title').textContent = profile.name;
    $('#character-subtitle').textContent = profile.subtitle;
    $('#character-category').textContent = profile.category;
    $('#character-symbol').textContent = profile.symbol;
    $('#character-description').textContent = profile.description;
    $('#character-quote').textContent = '“' + profile.quote + '”';
    $('#art-world').textContent = profile.world;
    $('#art-number').textContent = String(profile.index + 1).padStart(2, '0');
    $('#hero-art').className = 'hero-art portrait-art portrait-' + profile.index;
    $('#hero-art').setAttribute('aria-label', profile.alt);
    $('#design-traits').replaceChildren(...profile.traits.map(text => el('span', {className:'trait', text})));
    updateFavorite();
    renderRoster();
    if (announce) $('#selection-announcement').textContent = profile.name + '. ' + profile.subtitle;
  }
  function updateFavorite() {
    const active = favorites.has(selected.id), control = $('#favorite-button');
    control.textContent = active ? '♥' : '♡';
    control.setAttribute('aria-pressed', String(active));
    control.setAttribute('aria-label', (active ? 'Bỏ ' : 'Thêm ') + selected.name + (active ? ' khỏi yêu thích' : ' vào yêu thích'));
  }
  function renderRoster() {
    const entries = filtered();
    $('#roster-count').textContent = String(entries.length).padStart(2, '0');
    $('#roster-empty').classList.toggle('hidden', entries.length !== 0);
    // Keep the buttons stable during selection to preserve keyboard focus and scroll.
    const list = $('#character-roster');
    if (list.dataset.ids !== entries.map(p => p.id).join(',')) {
      list.dataset.ids = entries.map(p => p.id).join(',');
      list.replaceChildren(...entries.map(p => {
        const card = el('button', {type:'button', className:'portrait-card', 'data-profile':p.id, 'aria-label':p.name + ' — ' + p.category},
          el('span', {className:'card-portrait portrait-art portrait-' + p.index, 'aria-hidden':'true'}),
          el('span', {className:'card-symbol', text:p.symbol, 'aria-hidden':'true'}),
          el('span', {className:'card-index', text:String(p.index+1).padStart(2,'0'), 'aria-hidden':'true'}),
          el('span', {className:'card-caption'}, el('strong', {text:p.name}), el('small', {text:p.world})),
          el('span', {className:'card-check', text:'◆', 'aria-hidden':'true'}));
        card.addEventListener('click', () => select(p));
        return card;
      }));
    }
    $$('.portrait-card').forEach(card => card.setAttribute('aria-pressed', String(card.dataset.profile === selected.id)));
  }
  function step(direction, focus = false) {
    const entries = filtered();
    if (!entries.length) return;
    let index = entries.findIndex(p => p.id === selected.id);
    index = index < 0 ? (direction > 0 ? 0 : entries.length - 1) : (index + direction + entries.length) % entries.length;
    select(entries[index]);
    if (focus) {
      const card = $(`[data-profile="${selected.id}"]`);
      card?.focus({preventScroll:true});
      card?.scrollIntoView({block:'nearest', inline:'nearest', behavior:'auto'});
    }
  }
  $$('.roster-filters button').forEach(control => control.addEventListener('click', () => {
    filter = control.dataset.filter;
    $$('.roster-filters button').forEach(other => {
      other.classList.toggle('active', other === control);
      other.setAttribute('aria-pressed', String(other === control));
    });
    const entries = filtered();
    if (entries.length && !entries.includes(selected)) select(entries[0]); else renderRoster();
  }));
  $('#favorite-button').addEventListener('click', () => {
    if (favorites.has(selected.id)) favorites.delete(selected.id); else favorites.add(selected.id);
    saveLocal(favoriteKey, JSON.stringify([...favorites]));
    updateFavorite();
    renderRoster();
  });
  $('#previous-character').addEventListener('click', () => step(-1));
  $('#next-character').addEventListener('click', () => step(1));
  document.addEventListener('keydown', event => {
    if (event.altKey || event.ctrlKey || event.metaKey || authDialog.open || journeyDialog.open || event.target.closest('input,select,textarea,[contenteditable="true"]')) return;
    if (event.key === 'ArrowLeft' || event.key === 'ArrowRight') { event.preventDefault(); step(event.key === 'ArrowRight' ? 1 : -1, true); }
  });
  $$('[data-close]').forEach(control => control.addEventListener('click', () => $('#' + control.dataset.close).close()));
  authDialog.addEventListener('close', () => { if (!account) pendingJourney = false; });
  function authMode(isRegister) {
    register = isRegister;
    $('#login-tab').classList.toggle('active', !register); $('#login-tab').setAttribute('aria-pressed', String(!register));
    $('#register-tab').classList.toggle('active', register); $('#register-tab').setAttribute('aria-pressed', String(register));
    $('#registration-email').classList.toggle('hidden', !register);
    const form = $('#collection-auth-form');
    form.elements.email.disabled = !register; form.elements.email.required = register;
    form.elements.password.autocomplete = register ? 'new-password' : 'current-password';
    $('#auth-submit').replaceChildren(register ? 'Tạo tài khoản & tiếp tục' : 'Đăng nhập', el('span', {text:'→', 'aria-hidden':'true'}));
    $('#auth-title').textContent = register ? 'Một câu chuyện mới' : 'Trở về thế giới';
    showError('#auth-error');
  }
  $('#login-tab').addEventListener('click', () => authMode(false));
  $('#register-tab').addEventListener('click', () => authMode(true));
  async function loadAccount() {
    if (!sessionStorage.getItem(tokenKey)) { account = null; owned = []; $('#account-label').textContent = 'Đăng nhập'; return; }
    account = await api('/api/auth/me');
    owned = await api('/api/character/me');
    $('#account-label').textContent = account.username;
  }
  function rememberAppearance(character) {
    // Artwork preference is local to this browser. No server stats or 3D model are changed.
    saveLocal(`gw.art.account.${account.accountId}.character.${character.id}`, selected.id);
    sessionStorage.setItem('gw.character', String(character.id));
  }
  function enterExisting(character) {
    rememberAppearance(character);
    location.assign('/play.html');
  }
  async function openJourney() {
    try {
      await loadAccount();
      if (!account) { pendingJourney = true; showError('#auth-error'); authDialog.showModal(); return; }
      $('#journey-description').textContent = selected.name + ' · ' + selected.subtitle;
      const container = $('#owned-characters');
      container.replaceChildren();
      if (owned.length) {
        container.append(el('p', {className:'form-note', text:'Tiếp tục với nhân vật đã có:'}));
        owned.forEach(character => {
          const control = el('button', {type:'button', className:'owned-character'},
            el('div', {}, el('strong', {text:character.name}), el('small', {text:`Cấp ${character.level} · Chọn để vào thế giới`})),
            el('span', {text:'→', 'aria-hidden':'true'}));
          control.addEventListener('click', () => enterExisting(character));
          container.append(control);
        });
      }
      const form = $('#collection-create-form');
      form.classList.toggle('hidden', owned.length >= 3);
      form.elements.classId.value = String(selected.classId);
      if (owned.length >= 3) container.append(el('p', {className:'form-note', text:'Bạn đã có đủ 3 nhân vật. Chọn một nhân vật để tiếp tục.'}));
      showError('#journey-error');
      if (!journeyDialog.open) journeyDialog.showModal();
    } catch (error) {
      if (!sessionStorage.getItem(tokenKey)) { account = null; pendingJourney = true; authDialog.showModal(); }
      else toast(error.message, true);
    }
  }
  $('#begin-journey').addEventListener('click', openJourney);
  $('#account-button').addEventListener('click', () => {
    pendingJourney = false;
    if (account) openJourney(); else { showError('#auth-error'); authDialog.showModal(); }
  });
  $('#collection-auth-form').addEventListener('submit', async event => {
    event.preventDefault();
    const form = event.currentTarget, control = $('#auth-submit');
    control.disabled = true; showError('#auth-error');
    try {
      const credentials = {username:value(form, 'username'), password:form.elements.password.value};
      if (register) await api('/api/auth/register', {...credentials, email:value(form, 'email')}, 'POST', true);
      const response = await api('/api/auth/login', credentials, 'POST', true);
      sessionStorage.setItem(tokenKey, response.token);
      if (response.refreshToken) sessionStorage.setItem('gw.player.refresh', response.refreshToken);
      await loadAccount();
      const continueJourney = pendingJourney;
      pendingJourney = false;
      authDialog.close(); form.reset();
      if (continueJourney) await openJourney(); else toast('Chào mừng ' + account.username + '.');
    } catch (error) { showError('#auth-error', error.message); }
    finally { control.disabled = false; }
  });
  $('#collection-create-form').addEventListener('submit', async event => {
    event.preventDefault();
    const form = event.currentTarget, control = $('button[type="submit"]', form);
    control.disabled = true; showError('#journey-error');
    try {
      const response = await api('/api/character/create', {name:value(form, 'name'), classId:num(form, 'classId'), raceId:num(form, 'raceId')});
      const created = {id:response.characterId};
      if (!created.id) throw new Error('Không xác định được nhân vật vừa tạo. Vui lòng tải lại trang.');
      rememberAppearance(created);
      location.assign('/play.html');
    } catch (error) {
      showError('#journey-error', error.message);
      if (!sessionStorage.getItem(tokenKey)) {
        account = null; $('#account-label').textContent = 'Đăng nhập';
        journeyDialog.close(); pendingJourney = true;
        showError('#auth-error', 'Phiên đăng nhập đã hết hạn. Hãy đăng nhập để tiếp tục.'); authDialog.showModal();
      }
    } finally { control.disabled = false; }
  });
  $('#sign-out').addEventListener('click', async event => {
    event.currentTarget.disabled = true;
    try { await api('/api/auth/logout', {}); }
    catch (error) { if (sessionStorage.getItem(tokenKey)) { toast(error.message, true); return; } }
    finally { event.currentTarget.disabled = false; }
    sessionStorage.removeItem(tokenKey); sessionStorage.removeItem('gw.player.refresh'); sessionStorage.removeItem('gw.character');
    account = null; owned = []; $('#account-label').textContent = 'Đăng nhập'; journeyDialog.close(); toast('Đã đăng xuất.');
  });
  select(selected, false);
  loadAccount().catch(error => { account = null; $('#account-label').textContent = 'Đăng nhập'; toast(error.message, true); });
})();
