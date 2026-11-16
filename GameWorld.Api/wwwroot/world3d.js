import * as THREE from './vendor/three/three.module.js';

const $ = id => document.getElementById(id);
const fmt = n => new Intl.NumberFormat('vi-VN', { maximumFractionDigits: 2 }).format(n ?? 0);
const weatherNames = { Sunny: 'Trời nắng', Clear: 'Trời quang', Cloudy: 'Nhiều mây', Rain: 'Mưa', Fog: 'Sương mù', Snow: 'Tuyết rơi', Sandstorm: 'Bão cát' };
const npcPlaces = { 1: [6, 6], 4: [-8, 4], 5: [-10, -8], 2: [7, -5], 3: [0, -10] };
let state, world, characterId = Number(sessionStorage.getItem('gw.character')), renderer, scene, camera, terrain, avatar;
let sun, ambient, fireLight, flames, precipitation, skyColor, frameTime = 0, toastTimer, attackUntil = 0;
let yaw = .65, pitch = .62, distance = 18, nearest = null, actionBusy = false, panelMode = '', currentMap = 0;
let dragging = null, refreshPromise, lastPresence = 0, lastNearby = 0, presenceBusy = false, nearbyBusy = false, refreshBusy = false;
let chatMessages = [], presenceAvailable = true, shuttingDown = false, lastWeather = '';
const keys = new Set(), targets = [], obstacles = [], animated = [], peers = new Map();
const position = new THREE.Vector3(), cameraTarget = new THREE.Vector3(), projected = new THREE.Vector3();
const materials = new Map();

function node(tag, text = '', className = '') {
  const element = document.createElement(tag); element.textContent = text; if (className) element.className = className; return element;
}
function message(text, error = false) {
  $('toast').textContent = text; $('toast').classList.toggle('error', error); $('toast').classList.remove('hidden');
  clearTimeout(toastTimer); toastTimer = setTimeout(() => $('toast').classList.add('hidden'), error ? 6500 : 4500);
}
function fail(title, text) {
  $('loading-title').textContent = title; $('loading-message').textContent = text;
  $('loading-link').classList.remove('hidden'); $('loading').classList.remove('hidden');
}
async function api(path, data, retry = true) {
  const token = sessionStorage.getItem('gw.player.token');
  const response = await fetch(path, { method: data === undefined ? 'GET' : 'POST',
    headers: { Authorization: `Bearer ${token || ''}`, ...(data === undefined ? {} : { 'Content-Type': 'application/json' }) },
    body: data === undefined ? undefined : JSON.stringify(data), cache: 'no-store' });
  if (response.status === 401 && retry && sessionStorage.getItem('gw.player.refresh')) {
    if (!refreshPromise) refreshPromise = (async () => {
      const renewed = await fetch('/api/auth/refresh', { method: 'POST', headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ refreshToken: sessionStorage.getItem('gw.player.refresh') }) });
      if (!renewed.ok) throw new Error('Phiên đăng nhập đã kết thúc. Hãy đăng nhập lại tại trang chính.');
      const result = await renewed.json(); sessionStorage.setItem('gw.player.token', result.token); sessionStorage.setItem('gw.player.refresh', result.refreshToken);
    })().finally(() => { refreshPromise = null; });
    await refreshPromise; return api(path, data, false);
  }
  const result = await response.json().catch(() => ({}));
  if (!response.ok) {
    if (response.status === 401) { shuttingDown = true; fail('Hành trình tạm dừng.', 'Phiên đăng nhập đã kết thúc. Hãy quay lại trang chính để đăng nhập.'); }
    throw new Error(result.message || (result.errors ? Object.values(result.errors).flat().join(' ') : result.title) || `Máy chủ trả về lỗi ${response.status}.`);
  }
  return result;
}
async function act(action, data = {}) {
  if (actionBusy || !state) return;
  actionBusy = true; document.body.classList.add('working');
  try {
    const result = await api(`/api/gameplay/${characterId}/${action}`, data);
    message(result.message || 'Đã hoàn tất.');
    if (action === 'fight') { attackUntil = performance.now() + 900; if (nearest?.kind === 'monster') nearest.mesh.scale.setScalar(.75); }
    await refreshState(); return result;
  } catch (error) { message(error.message, true); }
  finally { actionBusy = false; document.body.classList.remove('working'); }
}
function actionButton(text, action, disabled = false) {
  const button = node('button', text, 'small'); button.type = 'button'; button.disabled = disabled;
  button.addEventListener('click', async () => { button.disabled = true; try { await action(); } catch (e) { message(e.message, true); } finally { if (button.isConnected) button.disabled = disabled; } });
  return button;
}
function row(title, detail, ...buttons) {
  const item = node('div', '', 'panel-row'), copy = node('div'), actions = node('div', '', 'panel-actions');
  copy.append(node('strong', title), node('small', detail)); actions.append(...buttons); item.append(copy, actions); return item;
}
function showPanel(title, mode) {
  keys.clear(); panelMode = mode; $('panel-title').textContent = title; $('panel-content').replaceChildren(); $('panel').classList.remove('hidden');
  $('close-panel').focus(); return $('panel-content');
}
function closePanel() { panelMode = ''; $('panel').classList.add('hidden'); keys.clear(); }

function mat(color, extra = {}) {
  const key = color + JSON.stringify(extra);
  if (!materials.has(key)) materials.set(key, new THREE.MeshToonMaterial({ color, ...extra }));
  return materials.get(key);
}
function mesh(geometry, material, x = 0, y = 0, z = 0, parent = terrain) {
  const object = new THREE.Mesh(geometry, material); object.position.set(x, y, z); object.castShadow = true; object.receiveShadow = true; parent.add(object); return object;
}
function box(w, h, d, color, x, y, z, parent) { return mesh(new THREE.BoxGeometry(w, h, d), mat(color), x, y, z, parent); }
function tree(x, z, scale = 1, pine = false) {
  const group = new THREE.Group(); terrain.add(group); group.position.set(x, 0, z); group.scale.setScalar(scale);
  mesh(new THREE.CylinderGeometry(.2, .33, 2.8, 5), mat('#796047'), 0, 1.4, 0, group);
  if (pine) {
    for (let i = 0; i < 3; i++) mesh(new THREE.ConeGeometry(1.7 - i * .35, 2.7, 6), mat(i % 2 ? '#4a7254' : '#61805c'), 0, 2.2 + i * 1.15, 0, group);
  } else {
    mesh(new THREE.IcosahedronGeometry(1.8, 0), mat('#76965a'), -.45, 3.7, .1, group);
    mesh(new THREE.IcosahedronGeometry(1.45, 0), mat('#95aa67'), .8, 3.4, .3, group);
  }
  obstacles.push({ x, z, radius: .65 * scale });
}
function house(x, z, rotation = 0, color = '#e4d4ab', city = false) {
  const g = new THREE.Group(); g.position.set(x, 0, z); g.rotation.y = rotation; terrain.add(g);
  box(5, 3.5, 4.2, color, 0, 1.75, 0, g);
  const roof = mesh(new THREE.ConeGeometry(4.3, 2.6, 4), mat(city ? '#657e77' : '#aa8050'), 0, 4.65, 0, g); roof.rotation.y = Math.PI / 4; roof.scale.z = .9;
  box(.9, 2.1, .13, '#765d41', 0, 1.05, 2.15, g);
  for (const side of [-1, 1]) { box(.85, .85, .14, '#90b3aa', side * 1.55, 2.1, 2.16, g); box(.08, .9, .17, '#a58a63', side * 1.55, 2.1, 2.2, g); }
  box(.65, 2, .7, '#acaa92', 1.4, 4.1, -.7, g);
  obstacles.push({ x, z, radius: 3.5 });
}
function humanoid(color = '#597453', npc = false) {
  const g = new THREE.Group();
  mesh(new THREE.CylinderGeometry(.43, .55, 1.1, 6), mat(color), 0, 1.12, 0, g);
  mesh(new THREE.IcosahedronGeometry(.4, 1), mat('#e6bc93'), 0, 1.98, 0, g);
  mesh(new THREE.CylinderGeometry(.45, .45, .24, 7), mat('#6e5941'), 0, 2.25, 0, g);
  const legs = [], arms = [];
  for (const side of [-1, 1]) {
    const leg = box(.28, .7, .32, '#5c6256', side * .22, .4, 0, g); legs.push(leg);
    const arm = box(.24, .85, .25, color, side * .57, 1.17, 0, g); arms.push(arm);
  }
  if (!npc) { box(.65, .7, .32, '#9c7a4c', 0, 1.2, .43, g); box(.13, 1.35, .15, '#d2d4be', .65, .95, -.1, g); }
  g.userData.legs = legs; g.userData.arms = arms; return g;
}
function target(kind, id, name, object, x, z) {
  object.position.x = x; object.position.z = z; terrain.add(object);
  const label = node('button', name, 'world-label' + (kind === 'monster' ? ' monster' : '')); label.type = 'button';
  label.addEventListener('click', () => {
    const d = Math.hypot(position.x - x, position.z - z);
    if (d > 4.5) { message(`Hãy đến gần ${name} để tương tác.`); return; }
    interactWith(item);
  });
  $('world-labels').append(label);
  const item = { kind, id, name, mesh: object, label, x, z }; targets.push(item); return item;
}
function monsterModel(id) {
  const g = new THREE.Group();
  if (id === 1) {
    const body = mesh(new THREE.IcosahedronGeometry(1.15, 1), mat('#98bd65'), 0, .8, 0, g); body.scale.y = .7;
    for (const side of [-1, 1]) mesh(new THREE.SphereGeometry(.13, 6, 5), mat('#253c30'), side * .3, 1, -.85, g);
  } else {
    box(1.1, .9, 1.9, id === 2 ? '#8c9697' : '#b79863', 0, 1, 0, g);
    mesh(new THREE.IcosahedronGeometry(.65, 0), mat(id === 2 ? '#9eaaa7' : '#b09b70'), 0, 1.5, -1, g);
    for (const x of [-.4, .4]) for (const z of [-.65, .65]) box(.25, .8, .25, '#6e7974', x, .4, z, g);
  }
  animated.push(g); return g;
}
function seeded(seed) { let s = seed; return () => { s = (Math.imul(s, 1664525) + 1013904223) >>> 0; return s / 4294967296; }; }
function clearTerrain() {
  targets.splice(0).forEach(t => t.label.remove()); obstacles.length = 0; animated.length = 0;
  if (!terrain) return;
  const geometries = new Set(); terrain.traverse(o => { if (o.geometry) geometries.add(o.geometry); }); geometries.forEach(g => g.dispose());
  scene.remove(terrain);
  for (const peer of peers.values()) { scene.remove(peer.mesh); peer.label.remove(); } peers.clear();
}
function buildTerrain() {
  clearTerrain(); terrain = new THREE.Group(); scene.add(terrain); currentMap = state.map.id; position.set(0, 0, 0); avatar.position.copy(position);
  const desert = currentMap === 4, mountain = currentMap === 3, city = currentMap === 2, forest = currentMap === 5;
  const random = seeded(currentMap * 53), groundColor = desert ? '#d5bb80' : mountain ? '#a3ada2' : '#91a570';
  mesh(new THREE.BoxGeometry(100, 1, 100), mat(groundColor), 0, -.55, 0);
  box(5.5, .08, 85, desert ? '#e4c997' : '#c6b48c', 0, .01, 0);
  box(75, .06, 4.5, desert ? '#dec28f' : '#c6b48c', 0, .02, 5);
  for (let i = 0; i < 45; i++) {
    const angle = i / 45 * Math.PI * 2, radius = 42 + random() * 14;
    const hill = mesh(new THREE.IcosahedronGeometry(5 + random() * 10, 0), mat(desert ? '#bda474' : mountain ? '#d5dbce' : '#6e8d60'), Math.cos(angle) * radius, 0, Math.sin(angle) * radius);
    hill.scale.y = mountain ? 2.6 : desert ? .6 : .9;
  }
  if (currentMap === 1) {
    house(-13, 15, .12); house(13, 15, -.2); house(-15, -14, .2); house(14, -14, -.15);
    for (let i = 0; i < 16; i++) { const x = (i % 4) * 1.25 - 13, z = Math.floor(i / 4) * 1.2 - 7; box(.8, .09, .85, '#896d47', x, .06, z); for (let j = 0; j < 3; j++) mesh(new THREE.ConeGeometry(.12, .8, 4), mat('#ccb16b'), x + (j - 1) * .2, .47, z); }
    const field = new THREE.Group(); target('farm', 0, 'Ruộng lúa · gieo trồng', field, -10, -5);
    for (let i = 0; i < 7; i++) box(.12, 1.1, .12, '#ac946a', -15 + i * 1.5, .55, -9);
    const stall = new THREE.Group(); box(3, .9, 1.2, '#aa8059', 0, .7, 0, stall); const roof = mesh(new THREE.ConeGeometry(2.4, 1.1, 4), mat('#b29c65'), 0, 2.8, 0, stall); roof.rotation.y = Math.PI / 4; stall.position.set(7, 0, 9); terrain.add(stall);
  } else if (city) {
    for (const x of [-18, -10, 10, 18]) { house(x, 18, 0, '#d8d7c3', true); house(x, -18, Math.PI, '#d8d7c3', true); }
    box(53, 5, 2, '#9fa995', 0, 2.5, -30);
    for (const x of [-27, 0, 27]) {
      mesh(new THREE.CylinderGeometry(2.6, 2.8, 10, 7), mat('#adb7a3'), x, 5, -30);
      mesh(new THREE.ConeGeometry(3.3, 4, 7), mat('#607c75'), x, 12, -30);
      box(.15, 4, .15, '#7c7359', x, 15, -30); box(2, 1, .1, '#bf9d55', x + .95, 16, -30);
    }
    const fountain = mesh(new THREE.CylinderGeometry(2.2, 2.5, .5, 12), mat('#b6bdab'), -7, .25, 0); obstacles.push({ x: -7, z: 0, radius: 2.5 });
    mesh(new THREE.CylinderGeometry(1.8, 1.8, .12, 12), mat('#8fb8b3'), -7, .55, 0); fountain.userData.decor = true;
  } else if (desert) {
    for (let i = 0; i < 24; i++) {
      const x = (random() - .5) * 70, z = (random() - .5) * 70; if (Math.abs(x) < 6 || Math.abs(z) < 7) continue;
      const g = new THREE.Group(); box(.5, 3, .5, '#7e976e', 0, 1.5, 0, g); box(1.5, .4, .4, '#7e976e', .4, 1.5, 0, g); box(.4, 1.2, .4, '#7e976e', 1, 1.9, 0, g); g.position.set(x, 0, z); terrain.add(g); obstacles.push({ x, z, radius: .7 });
    }
    house(-13, 15, 0, '#d2b17a'); house(14, -16, .3, '#d2b17a');
  }
  if (!desert) {
    for (let i = 0; i < (forest ? 68 : 34); i++) {
      const x = (random() - .5) * 79, z = (random() - .5) * 79;
      if (Math.abs(x) < 7 || Math.abs(z - 5) < 5 || (currentMap === 1 && Math.abs(x) < 21 && Math.abs(z) < 21) || (city && Math.abs(x) < 30 && Math.abs(z) < 32)) continue;
      tree(x, z, .8 + random() * .7, mountain || forest && i % 2 === 0);
    }
  }
  for (const npc of state.npcs) {
    const [x, z] = npcPlaces[npc.id] || [5 + npc.id % 3 * 3, -5];
    target('npc', npc.id, npc.name, humanoid(npc.type === 'Chef' ? '#d9d1b6' : npc.type === 'Farmer' ? '#9b8053' : '#6e8e82', true), x, z);
  }
  for (const [index, monster] of state.monsters.entries()) target('monster', monster.id, `${monster.name} · Cấp ${monster.level}`, monsterModel(monster.id), 8 + index * 6, -9 - index * 5);
  const camp = new THREE.Group();
  for (let i = 0; i < 7; i++) mesh(new THREE.DodecahedronGeometry(.35, 0), mat('#989b87'), Math.cos(i) * .9, .2, Math.sin(i) * .9, camp);
  for (const offset of [-.2, .25]) { const log = box(1.5, .25, .3, '#785a3b', 0, .25, offset, camp); log.rotation.y = offset * 2; }
  flames = mesh(new THREE.ConeGeometry(.5, 1.4, 5), new THREE.MeshBasicMaterial({ color: '#eeb965' }), 0, .9, 0, camp);
  const rest = target('camp', 0, state.map.isSafeZone ? 'Feu trại · nghỉ ngơi'.replace('Feu', 'Lửa') : 'Lửa trại', camp, -5, 9);
  fireLight.position.set(rest.x, 2, rest.z);
  lastWeather = ''; updateAtmosphere();
}
function updateAtmosphere() {
  const w = state.weather, hour = world?.gameHour ?? (new Date().getUTCHours() + 7) % 24;
  const night = hour < 6 || hour >= 19, dusk = hour >= 17 && hour < 19;
  skyColor = new THREE.Color(night ? '#24394b' : dusk ? '#d5b28b' : '#a9c7be'); scene.background = skyColor;
  const fogDensity = w.type === 'Fog' ? .037 : w.type === 'Sandstorm' ? .036 : night ? .024 : .013;
  scene.fog = new THREE.FogExp2(skyColor, fogDensity); ambient.intensity = night ? .7 : 2; sun.intensity = night ? .35 : dusk ? 1.7 : 2.7;
  sun.color.set(dusk ? '#ffe0ab' : night ? '#b2c9de' : '#fff1d2');
  if (lastWeather === w.type) return; lastWeather = w.type;
  if (precipitation) { scene.remove(precipitation); precipitation.geometry.dispose(); precipitation.material.dispose(); precipitation = null; }
  if (!['Rain', 'Snow', 'Sandstorm'].includes(w.type)) return;
  const data = new Float32Array(450 * 3), random = seeded(state.map.id * 121);
  for (let i = 0; i < data.length; i += 3) { data[i] = (random() - .5) * 62; data[i + 1] = random() * 24; data[i + 2] = (random() - .5) * 62; }
  const geometry = new THREE.BufferGeometry(); geometry.setAttribute('position', new THREE.BufferAttribute(data, 3));
  precipitation = new THREE.Points(geometry, new THREE.PointsMaterial({ color: w.type === 'Sandstorm' ? '#d6ba82' : '#e3edf0', size: w.type === 'Snow' ? .12 : .065, transparent: true, opacity: .65 }));
  scene.add(precipitation);
}
function updateHud() {
  const c = state.character, w = state.weather;
  $('location').textContent = state.map.name; $('zone').textContent = state.map.isSafeZone ? 'Miền quê yên bình · Khu vực an toàn' : 'Vùng khám phá · Hãy chuẩn bị hành trang';
  $('player-name').textContent = c.name; $('level').textContent = `Cấp ${c.level}`; $('health').textContent = `${c.currentHP} / ${c.maxHP} HP`; $('gold').textContent = `${fmt(c.gold)} vàng`;
  $('health-fill').style.width = `${Math.min(100, Math.max(0, c.currentHP / c.maxHP * 100))}%`;
  $('weather').textContent = weatherNames[w.type] || w.type; $('season').textContent = `Mùa ${w.season} · Cây trồng ×${fmt(w.cropModifier)}`;
  $('weather-icon').textContent = w.type === 'Rain' ? '☂' : w.type === 'Snow' ? '❄' : ['Fog', 'Sandstorm'].includes(w.type) ? '≋' : '☀';
}
async function refreshState() {
  if (refreshBusy) return; refreshBusy = true;
  try {
    const fresh = await api(`/api/gameplay/${characterId}/state`); state = fresh; updateHud();
    if (state.map.id !== currentMap) { closePanel(); buildTerrain(); }
    else updateAtmosphere();
    if (panelMode === 'inventory') openInventory();
    if (panelMode === 'farm') openFarm();
    if (panelMode.startsWith('npc:')) { const npc = state.npcs.find(n => `npc:${n.id}` === panelMode); if (npc) openNpc(npc.id); }
  } finally { refreshBusy = false; }
}
function openTravel() {
  const content = showPanel('Những miền đất mới', 'travel'); content.append(node('p', 'Trở lại Làng Tân Thủ luôn miễn phí. Chi phí tại vùng khác còn chịu ảnh hưởng thời tiết và kỹ năng.'));
  for (const map of state.maps) {
    const here = map.id === state.map.id, locked = state.character.level < map.minLevel;
    content.append(row(map.name, `Cấp ${map.minLevel} · Giá gốc ${fmt(map.travelCost)} vàng`, actionButton(here ? 'Đang ở đây' : locked ? `Cần cấp ${map.minLevel}` : 'Lên đường', () => act('travel', { mapId: map.id }), here || locked)));
  }
}
function openInventory() {
  const content = showPanel('Hành trang lữ hành', 'inventory');
  content.append(node('p', `${fmt(state.character.gold)} vàng · ${state.inventory.length} / 80 ô hành trang. Thức ăn giúp hồi sinh lực trên hành trình.`));
  if (!state.inventory.length) content.append(node('p', 'Hành trang đang trống.'));
  for (const item of state.inventory) {
    const actions = [];
    if (item.category === 'Food' || item.itemId === 7) actions.push(actionButton('Dùng', () => act('use', { inventoryId: item.id })));
    if (item.category === 'Equipment') actions.push(actionButton(item.isEquipped ? 'Tháo' : 'Trang bị', () => act('equip', { inventoryId: item.id })));
    content.append(row(item.name, `×${item.quantity} · ${item.isEquipped ? 'Đang trang bị' : item.rarity}`, ...actions));
  }
  const link = node('a', 'Mở chợ & thương đoàn →', 'primary'); link.href = '/'; content.append(link);
}
function openFarm() {
  const content = showPanel('Ruộng vườn của bạn', 'farm');
  content.append(node('p', `Mùa ${state.weather.season} ảnh hưởng tốc độ lớn và năng suất. Bạn có tối đa 3 luống đang trồng.`));
  for (const crop of state.crops) content.append(row(crop.name, `Thời gian cơ sở ${crop.growthTimeMinutes} phút · Năng suất ${crop.baseYield}`, actionButton('Gieo trồng', () => act('plant', { cropId: crop.id }), state.plots.length >= 3)));
  for (const plot of state.plots) {
    const remaining = Math.max(0, Math.ceil((new Date(plot.readyAt).getTime() - Date.now()) / 1000));
    content.append(row(plot.name, remaining === 0 ? `Đã chín · ${plot.expectedYield} nông sản` : `Khoảng ${remaining} giây · ${plot.expectedYield} nông sản`, actionButton('Thu hoạch', () => act('harvest', { plotId: plot.id }), remaining > 0)));
  }
  content.append(actionButton('Cập nhật ruộng', refreshState));
}
function openNpc(id) {
  const npc = state.npcs.find(n => n.id === id); if (!npc) return;
  const content = showPanel(npc.name, `npc:${id}`);
  const greetings = { Merchant: 'Hàng hóa có thể đổi giá theo mùa. Cứ xem thử, người lữ hành!', Chef: 'Bếp đã đỏ lửa. Mang lúa mì đến, ta cùng nướng bánh.', Farmer: 'Đất tốt và một chút kiên nhẫn, mùa gặt sẽ đến.', Trainer: 'Kinh nghiệm trên đường đời là người thầy tốt nhất.', Blacksmith: 'Một thanh kiếm tốt sẽ bảo vệ chuyến hàng của bạn.' };
  content.append(node('p', greetings[npc.type] || 'Chào mừng người lữ hành.'));
  for (const item of npc.shopItems) {
    const quantity = node('input'); quantity.type = 'number'; quantity.min = '1'; quantity.max = '99'; quantity.value = '1'; quantity.setAttribute('aria-label', `Số lượng ${item.name}`);
    const trade = buy => act('trade', { npcId: id, itemId: item.itemId, quantity: Number(quantity.value), buy });
    content.append(row(item.name, `Mua ${fmt(item.buyPrice)} · Bán ${fmt(item.sellPrice)} vàng · Kho ${item.stock}`, quantity,
      actionButton('Mua', () => trade(true), !item.isSell), actionButton('Bán', () => trade(false), !item.isBuy)));
  }
  if (npc.type === 'Chef') for (const recipe of state.recipes) content.append(row(recipe.name, 'Cần 2 lúa mì · Nhận bánh nướng', actionButton('Nấu ăn', () => act('cook', { recipeId: recipe.id }))));
  if (npc.type === 'Farmer') content.append(actionButton('Chăm sóc ruộng', openFarm));
  for (const skill of state.skills.filter(s => s.trainerNpcId === id)) {
    const progress = skill.requirements.map(r => `${r.actionType} ${r.currentCount}/${r.requiredCount}`).join(' · ');
    const ready = skill.requirements.every(r => r.currentCount >= r.requiredCount);
    content.append(row(skill.name, skill.isUnlocked ? 'Đã lĩnh hội kỹ năng' : progress, actionButton(skill.isUnlocked ? 'Đã học' : 'Học kỹ năng', () => act('learn', { npcId: id, skillId: skill.id }), skill.isUnlocked || !ready)));
  }
}
function openCamp() {
  const content = showPanel('Bên ánh lửa trại', 'camp');
  content.append(node('p', state.map.isSafeZone ? 'Ngồi lại một chút. Một giấc nghỉ yên lành giúp bạn hồi đầy sinh lực.' : 'Lửa trại soi sáng đường đi. Hãy dùng thức ăn hoặc trở lại làng để nghỉ an toàn.'));
  content.append(actionButton('Nghỉ ngơi · 5 vàng', () => act('rest'), !state.map.isSafeZone), actionButton('Mở hành trang', openInventory));
}
function interactWith(item) {
  if (item.kind === 'npc') openNpc(item.id);
  else if (item.kind === 'farm') openFarm();
  else if (item.kind === 'camp') openCamp();
  else if (item.kind === 'monster') fight(item);
}
function fight(item) {
  if (!item || item.kind !== 'monster' || Math.hypot(item.x - position.x, item.z - position.z) > 4.5) { message('Hãy đến gần quái vật trong Rừng Ven Làng để chiến đấu.'); return; }
  closePanel(); act('fight', { monsterId: item.id });
}
function openHelp() {
  const content = showPanel('Khởi đầu một chuyến đi', 'help');
  const help = node('div', '', 'key-list');
  for (const [key, value] of [['W A S D / ↑↓←→', 'Di chuyển theo góc nhìn'], ['Kéo chuột / vuốt', 'Xoay camera'], ['Cuộn / + −', 'Phóng gần, thu xa'], ['E', 'Tương tác khi đến gần NPC, ruộng hoặc lửa trại'], ['F', 'Chiến đấu với quái vật gần bạn'], ['I / Esc', 'Mở hành trang / đóng bảng']]) help.append(node('b', key), node('span', value));
  content.append(help, node('p', 'Mua bán với Bà Lan, nấu ăn tại Chủ Quán, gieo trồng cạnh Bác Năm. Rừng Ven Làng có slime phù hợp cho người mới.'));
  content.append(node('div', 'Bản chơi thử 3D. Vàng, đồ đạc và kết quả hành động được lưu trên máy chủ. Vị trí người chơi và trò chuyện chia sẻ trong phiên máy chủ hiện tại; chiến đấu đang theo lượt.', 'notice'));
}
function renderChat() {
  const list = $('chat-messages'); if (!list) return;
  const atBottom = list.scrollHeight - list.scrollTop - list.clientHeight < 40;
  list.replaceChildren(...chatMessages.map(m => { const line = node('div', '', 'chat-line'); line.append(node('strong', m.name + ': '), node('span', m.text)); return line; }));
  if (!chatMessages.length) list.append(node('p', 'Chưa có lời nhắn. Hãy chào những người lữ hành ở cùng vùng.'));
  if (atBottom) list.scrollTop = list.scrollHeight;
}
function openChat() {
  const content = showPanel('Trò chuyện trong vùng', 'chat'), list = node('div', '', 'chat-messages'); list.id = 'chat-messages';
  const form = node('form', '', 'chat-form'), input = node('input'), send = node('button', 'Gửi', 'small');
  input.maxLength = 200; input.required = true; input.placeholder = 'Lời chào của bạn…'; input.setAttribute('aria-label', 'Nội dung trò chuyện'); send.type = 'submit'; form.append(input, send);
  form.addEventListener('submit', async e => { e.preventDefault(); send.disabled = true; try {
    const sent = await api('/api/presence/chat', { characterId, text: input.value.trim() }); input.value = ''; if (sent.id) chatMessages.push(sent); renderChat();
  } catch (error) { message(error.message, true); } finally { send.disabled = false; input.focus(); } });
  content.append(list, form, node('p', 'Tin nhắn chỉ hiển thị với người chơi ở cùng bản đồ trong phiên máy chủ này.')); renderChat(); input.focus();
}
async function syncPresence() {
  if (presenceBusy || !state || shuttingDown || !presenceAvailable) return; presenceBusy = true;
  const mapAtRequest = state.map.id;
  try {
    const result = await api('/api/presence/update', { characterId, x: position.x, z: position.z });
    if (state.map.id === mapAtRequest && result.mapId === mapAtRequest && Number.isFinite(result.x) && Number.isFinite(result.z) && result.corrected) {
      position.x = result.x; position.z = result.z;
    }
  } catch (error) { if (!shuttingDown && /404/.test(error.message)) presenceAvailable = false; }
  finally { presenceBusy = false; }
}
async function syncNearby() {
  if (nearbyBusy || !state || shuttingDown || !presenceAvailable) return; nearbyBusy = true;
  const mapAtRequest = state.map.id;
  try {
    const nearby = await api(`/api/presence/nearby?characterId=${characterId}`); if (state.map.id !== mapAtRequest) return;
    const seen = new Set();
    for (const other of nearby.players) {
      if (other.characterId === characterId) continue; seen.add(other.characterId);
      let peer = peers.get(other.characterId);
      if (!peer) {
        const model = humanoid('#6b86a0'), label = node('span', `${other.name} · Cấp ${other.level}`, 'world-label player-label'); scene.add(model); $('world-labels').append(label);
        model.position.set(other.x, 0, other.z); peer = { mesh: model, label, x: other.x, z: other.z }; peers.set(other.characterId, peer);
      }
      peer.x = other.x; peer.z = other.z;
    }
    for (const [id, peer] of peers) if (!seen.has(id)) { scene.remove(peer.mesh); peer.mesh.traverse(o => o.geometry?.dispose()); peer.label.remove(); peers.delete(id); }
    chatMessages = nearby.messages; if (panelMode === 'chat') renderChat();
  } catch { /* Network errors should not interrupt local camera controls. Authentication failures display the login handoff. */ }
  finally { nearbyBusy = false; }
}
function canStand(x, z) { return Math.abs(x) < 40 && Math.abs(z) < 40 && !obstacles.some(o => Math.hypot(o.x - x, o.z - z) < o.radius + .38); }
function updateLabels() {
  nearest = null; let best = 4.5;
  for (const item of targets) {
    const d = Math.hypot(item.x - position.x, item.z - position.z); if (d < best) { best = d; nearest = item; }
    positionLabel(item.label, item.mesh.position, 3.1); item.label.classList.toggle('near', d <= 4.5);
  }
  for (const peer of peers.values()) positionLabel(peer.label, peer.mesh.position, 3.1);
  $('context-hint').classList.toggle('hidden', !nearest || !!panelMode);
  if (nearest) $('context-hint').textContent = `${nearest.kind === 'monster' ? 'F · Chiến đấu' : 'E · Tương tác'} — ${nearest.name}`;
}
function positionLabel(label, at, height) {
  projected.copy(at); projected.y += height; projected.project(camera);
  const visible = projected.z < 1 && projected.z > -1 && Math.abs(projected.x) < 1.1 && Math.abs(projected.y) < 1.1;
  label.hidden = !visible;
  if (visible) label.style.transform = `translate(-50%,-100%) translate(${(projected.x * .5 + .5) * innerWidth}px,${(-projected.y * .5 + .5) * innerHeight}px)`;
}
function animate(ms) {
  const dt = Math.min((ms - frameTime) / 1000 || .016, .05); frameTime = ms;
  if (!state || !renderer || shuttingDown) return;
  const moving = !panelMode && !actionBusy && state.character.currentHP > 0;
  let forward = moving ? Number(keys.has('forward')) - Number(keys.has('back')) : 0;
  let side = moving ? Number(keys.has('right')) - Number(keys.has('left')) : 0;
  const length = Math.hypot(forward, side); if (length) { forward /= length; side /= length; }
  const speed = 5 * state.weather.movementModifier, dx = (-Math.sin(yaw) * forward + Math.cos(yaw) * side) * speed * dt, dz = (-Math.cos(yaw) * forward - Math.sin(yaw) * side) * speed * dt;
  if (canStand(position.x + dx, position.z)) position.x += dx;
  if (canStand(position.x, position.z + dz)) position.z += dz;
  if (length) avatar.rotation.y = Math.atan2(-dx, -dz);
  avatar.position.copy(position); avatar.position.y = length ? Math.abs(Math.sin(ms * .01)) * .055 : 0;
  avatar.userData.legs.forEach((leg, i) => { leg.rotation.x = length ? Math.sin(ms * .012 + i * Math.PI) * .45 : 0; });
  avatar.userData.arms[1].rotation.x = attackUntil > ms ? -Math.sin((attackUntil - ms) * .018) * 1.6 : 0;
  cameraTarget.set(position.x + Math.sin(yaw) * Math.cos(pitch) * distance, Math.sin(pitch) * distance + 2, position.z + Math.cos(yaw) * Math.cos(pitch) * distance);
  camera.position.lerp(cameraTarget, 1 - Math.exp(-7 * dt)); camera.lookAt(position.x, 1.2, position.z);
  animated.forEach((m, i) => { m.position.y = Math.abs(Math.sin(ms * .002 + i)) * .28; const scale = m.scale.x + (1 - m.scale.x) * dt * 4; m.scale.setScalar(scale); });
  for (const peer of peers.values()) {
    const ox = peer.mesh.position.x, oz = peer.mesh.position.z; peer.mesh.position.x = THREE.MathUtils.lerp(ox, peer.x, 1 - Math.exp(-5 * dt)); peer.mesh.position.z = THREE.MathUtils.lerp(oz, peer.z, 1 - Math.exp(-5 * dt));
    if (Math.hypot(peer.x - ox, peer.z - oz) > .06) { peer.mesh.rotation.y = Math.atan2(ox - peer.x, oz - peer.z); peer.mesh.userData.legs.forEach((l, i) => { l.rotation.x = Math.sin(ms * .012 + i * Math.PI) * .4; }); }
  }
  if (flames) { flames.scale.set(1 + Math.sin(ms * .013) * .1, .9 + Math.sin(ms * .018) * .18, 1); fireLight.intensity = 7 + Math.sin(ms * .014); }
  if (precipitation) {
    const data = precipitation.geometry.attributes.position, snow = state.weather.type === 'Snow', sand = state.weather.type === 'Sandstorm';
    for (let i = 0; i < data.count; i++) {
      let y = data.getY(i) - dt * (snow ? 1.7 : sand ? 3 : 17), x = data.getX(i) + dt * (sand ? 8 : .6);
      if (y < 0) y = 24; if (x > 31) x = -31; data.setXYZ(i, x, y, data.getZ(i));
    }
    data.needsUpdate = true; precipitation.position.set(position.x, 0, position.z);
  }
  updateLabels(); renderer.render(scene, camera);
  if (ms - lastPresence > 1000 && !document.hidden) { lastPresence = ms; syncPresence(); }
  if (ms - lastNearby > 2200 && !document.hidden) { lastNearby = ms; syncNearby(); }
}
function setupInput() {
  const keyMap = { KeyW: 'forward', ArrowUp: 'forward', KeyS: 'back', ArrowDown: 'back', KeyA: 'left', ArrowLeft: 'left', KeyD: 'right', ArrowRight: 'right' };
  window.addEventListener('keydown', e => {
    if (['INPUT', 'TEXTAREA', 'SELECT'].includes(document.activeElement?.tagName)) { if (e.code === 'Escape') closePanel(); return; }
    if (keyMap[e.code]) { e.preventDefault(); keys.add(keyMap[e.code]); }
    if (e.repeat) return;
    if (e.code === 'Escape') closePanel();
    if (e.code === 'KeyI') openInventory();
    if (e.code === 'KeyE' && nearest) interactWith(nearest);
    if (e.code === 'KeyF') fight(nearest?.kind === 'monster' ? nearest : targets.find(t => t.kind === 'monster' && Math.hypot(t.x - position.x, t.z - position.z) <= 4.5));
  });
  window.addEventListener('keyup', e => keys.delete(keyMap[e.code])); window.addEventListener('blur', () => { keys.clear(); dragging = null; });
  document.addEventListener('visibilitychange', () => { if (document.hidden) keys.clear(); });
  document.querySelectorAll('[data-move]').forEach(button => {
    button.addEventListener('pointerdown', e => { e.preventDefault(); keys.add(button.dataset.move); button.setPointerCapture(e.pointerId); });
    for (const type of ['pointerup', 'pointercancel', 'lostpointercapture']) button.addEventListener(type, () => keys.delete(button.dataset.move));
  });
  const canvas = renderer.domElement;
  canvas.addEventListener('pointerdown', e => { if (panelMode) closePanel(); dragging = { id: e.pointerId, x: e.clientX, y: e.clientY }; canvas.setPointerCapture(e.pointerId); });
  canvas.addEventListener('pointermove', e => { if (!dragging || dragging.id !== e.pointerId) return; yaw -= (e.clientX - dragging.x) * .006; pitch = THREE.MathUtils.clamp(pitch + (e.clientY - dragging.y) * .004, .28, 1.15); dragging.x = e.clientX; dragging.y = e.clientY; });
  for (const type of ['pointerup', 'pointercancel', 'lostpointercapture']) canvas.addEventListener(type, () => { dragging = null; });
  canvas.addEventListener('wheel', e => { e.preventDefault(); distance = THREE.MathUtils.clamp(distance + e.deltaY * .012, 8, 30); }, { passive: false });
  $('zoom-in').onclick = () => { distance = Math.max(8, distance - 2); }; $('zoom-out').onclick = () => { distance = Math.min(30, distance + 2); };
  $('travel').onclick = openTravel; $('help').onclick = openHelp; $('inventory').onclick = openInventory; $('close-panel').onclick = closePanel;
  $('interact').onclick = () => { if (nearest) interactWith(nearest); else message('Hãy đi gần NPC, ruộng hoặc lửa trại.'); };
  $('attack').onclick = () => fight(nearest?.kind === 'monster' ? nearest : null); $('chat').onclick = openChat;
  window.addEventListener('resize', () => { camera.aspect = innerWidth / innerHeight; camera.updateProjectionMatrix(); renderer.setSize(innerWidth, innerHeight); });
}
async function start() {
  if (!sessionStorage.getItem('gw.player.token')) { fail('Chuyến đi bắt đầu tại làng.', 'Hãy đăng nhập và tạo nhân vật tại trang chính trước khi bước vào thế giới 3D.'); return; }
  try {
    const characters = await api('/api/character/me'); if (!characters.length) { fail('Đặt tên cho hành trình.', 'Bạn chưa có nhân vật. Hãy tạo nhân vật tại trang chính.'); return; }
    if (!characters.some(c => c.id === characterId)) characterId = characters[0].id;
    sessionStorage.setItem('gw.character', characterId);
    [state, world] = await Promise.all([api(`/api/gameplay/${characterId}/state`), api('/api/world/state')]);
    try { renderer = new THREE.WebGLRenderer({ antialias: true, powerPreference: 'high-performance' }); }
    catch { fail('Thiết bị chưa mở được cảnh 3D.', 'Trình duyệt cần hỗ trợ WebGL 2 và bật tăng tốc đồ họa. Bạn vẫn có thể chơi bằng giao diện hoạt động tại trang chính.'); return; }
    renderer.setPixelRatio(Math.min(devicePixelRatio || 1, 1.5)); renderer.setSize(innerWidth, innerHeight); renderer.shadowMap.enabled = true; renderer.shadowMap.type = THREE.PCFSoftShadowMap;
    $('viewport').append(renderer.domElement); scene = new THREE.Scene(); camera = new THREE.PerspectiveCamera(48, innerWidth / innerHeight, .1, 160); camera.position.set(12, 14, 17);
    ambient = new THREE.HemisphereLight('#e9f0e2', '#688058', 2); scene.add(ambient);
    sun = new THREE.DirectionalLight('#fff1d2', 2.7); sun.position.set(-15, 24, 9); sun.castShadow = true; sun.shadow.mapSize.set(1024, 1024); sun.shadow.camera.left = -32; sun.shadow.camera.right = 32; sun.shadow.camera.top = 32; sun.shadow.camera.bottom = -32; sun.shadow.normalBias = .04; scene.add(sun);
    fireLight = new THREE.PointLight('#ffbe66', 7, 13, 2); scene.add(fireLight); avatar = humanoid(); scene.add(avatar);
    buildTerrain(); updateHud(); setupInput(); $('loading').classList.add('hidden'); renderer.setAnimationLoop(animate);
    await api('/api/auth/heartbeat', {}).catch(() => {}); await syncPresence();
    setInterval(() => { if (!document.hidden && !shuttingDown) refreshState().catch(e => message(e.message, true)); }, 30000);
    setInterval(() => { if (!shuttingDown) api('/api/auth/heartbeat', {}).catch(() => {}); }, 60000);
    message('Chào mừng! Dùng W A S D để di chuyển, đến gần NPC rồi nhấn E.');
    renderer.domElement.addEventListener('webglcontextlost', e => { e.preventDefault(); renderer.setAnimationLoop(null); fail('Cảnh 3D đã tạm dừng.', 'Trình duyệt vừa mất kết nối đồ họa. Hãy tải lại trang để tiếp tục; dữ liệu nhân vật đã lưu trên máy chủ.'); });
  } catch (error) { fail('Chưa thể bước vào thế giới.', error.message); }
}
start();
