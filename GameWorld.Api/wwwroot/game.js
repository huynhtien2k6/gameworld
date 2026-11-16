'use strict';
let gameState = null, characterId = Number(sessionStorage.getItem('gw.character') || 0), registerMode = false;
let characters = [], playerName = '', stateRequest = 0;
const regionNames = { 1:'Làng Tân Thủ', 2:'Thành Phố Pháo Đài', 3:'Đồi Núi Hắc Ám', 4:'Sa Mạc Gió Hè' };
const weatherNames = { Clear:'Trời quang', Sunny:'Nắng ấm', Cloudy:'Nhiều mây', Rain:'Mưa', Rainy:'Mưa', Snow:'Tuyết rơi', Snowy:'Tuyết rơi', Fog:'Sương mù', Foggy:'Sương mù', Storm:'Mưa giông', Sandstorm:'Bão cát', Heat:'Nắng nóng' };
const actionNames = { Trade:'Giao dịch', Cook:'Nấu ăn', Harvest:'Thu hoạch', Travel:'Di chuyển', Fight:'Chiến đấu' };
const npcIcons = { Merchant:'⚖', Chef:'♨', Farmer:'♧', Trainer:'✧', Blacksmith:'⚒' };
function gamePath(action) {
  if (!characterId) throw new Error('Hãy tạo và chọn một nhân vật trước.');
  if (action !== 'state' && gameState?.character.id !== characterId) throw new Error('Đang tải nhân vật đã chọn. Vui lòng chờ một chút.');
  return `/api/gameplay/${characterId}/${action}`;
}
function addLog(message) {
  const log = $('#activity-log'); $('.empty', log)?.remove();
  log.prepend(el('div', {className:'log-entry'}, el('time', {text:new Date().toLocaleTimeString('vi-VN')}), message));
  while (log.children.length > 30) log.lastChild.remove();
}
async function act(action, data) {
  const result = await api(gamePath(action), data);
  const message = result?.message || 'Đã hoàn tất hành động.';
  toast(message); addLog(message); await loadState(); return result;
}
function mode(register) {
  registerMode = register;
  $('#register-tab').classList.toggle('active', register); $('#login-tab').classList.toggle('active', !register);
  $('#email-label').classList.toggle('hidden', !register); $('#auth-form').elements.email.required = register;
  $('#auth-form').elements.password.autocomplete = register ? 'new-password' : 'current-password';
  $('#auth-submit').textContent = register ? 'Tạo tài khoản & khởi hành' : 'Bước vào thế giới'; $('#auth-error').classList.add('hidden');
}
$('#login-tab').addEventListener('click', () => mode(false)); $('#register-tab').addEventListener('click', () => mode(true));
$('#auth-form').addEventListener('submit', async e => {
  e.preventDefault(); const form = e.currentTarget, b = $('#auth-submit'); b.disabled = true; $('#auth-error').classList.add('hidden');
  try {
    const credentials = { username:value(form,'username'), password:form.elements.password.value };
    if (registerMode) await api('/api/auth/register', {...credentials,email:value(form,'email')}, 'POST', true);
    const result = await api('/api/auth/login', credentials, 'POST', true);
    sessionStorage.setItem(tokenKey,result.token); if(result.refreshToken) sessionStorage.setItem('gw.player.refresh',result.refreshToken);
    playerName = result.username; form.reset(); $('#auth-overlay').classList.add('hidden'); await enterWorld();
  } catch(error) { $('#auth-error').textContent = error.message; $('#auth-error').classList.remove('hidden'); }
  finally { b.disabled = false; }
});
$('#logout').addEventListener('click', e => run(e.currentTarget, async () => {
  try { await api('/api/auth/logout',{}); } finally { sessionStorage.removeItem(tokenKey); sessionStorage.removeItem('gw.player.refresh'); sessionStorage.removeItem('gw.character'); location.reload(); }
}));
setupNav(view => { if(view === 'mail') loadMail().catch(e => toast(e.message,true)); if(view === 'support') loadTickets().catch(e => toast(e.message,true)); if(view === 'community') loadCommunity().catch(e => toast(e.message,true)); });
$('#go-create').addEventListener('click', () => $('[data-view=character]').click());
$('#character-select').addEventListener('change', async e => { characterId = Number(e.target.value); sessionStorage.setItem('gw.character',characterId); await run(null, loadState); });
async function enterWorld() {
  const me = await api('/api/auth/me'); playerName = me?.username || me?.account?.username || playerName;
  $('#account-name').textContent = playerName; $('#greeting-name').textContent = playerName || 'nhà lữ hành'; $('#player-avatar').textContent = playerName ? playerName[0].toUpperCase() : '♙';
  $('#connection-status').textContent = 'Đã kết nối máy chủ';
  await loadCharacters();
  await api('/api/auth/heartbeat',{}).catch(() => {});
}
async function loadCharacters() {
  const result = await api('/api/character/me'); characters = Array.isArray(result) ? result : result.characters || [];
  options($('#character-select'), characters);
  if(!characters.some(c => c.id === characterId)) characterId = characters[0]?.id || 0;
  $('#character-select').value = String(characterId); sessionStorage.setItem('gw.character',characterId);
  $('#no-character').classList.toggle('hidden', !!characterId);
  if(characterId) await loadState(); else { $('#character-details').replaceChildren(empty('Người lữ hành đầu tiên đang đợi được đặt tên.')); $('#inventory').replaceChildren(empty('Hành trang sẽ xuất hiện khi bạn tạo nhân vật.')); }
}
async function loadState() {
  const requestedCharacter = characterId, request = ++stateRequest;
  if(!requestedCharacter) return;
  const state = await api(`/api/gameplay/${requestedCharacter}/state`);
  // A response for an earlier selection must never replace the currently selected character.
  if(request !== stateRequest || requestedCharacter !== characterId) return;
  gameState = state;
  renderState();
}
function renderState() {
  const s = gameState, c = s.character, w = s.weather;
  $('#greeting-name').textContent = c.name; $('#world-subtitle').textContent = `Bạn đang ở ${s.map.name}. Một ngày mới, những cơ hội mới.`;
  $('#stat-level').textContent = String(c.level).padStart(2,'0'); $('#stat-xp').textContent = `${fmt(c.experience)} / ${fmt(c.experienceToNextLevel)} kinh nghiệm`;
  $('#stat-gold').textContent = fmt(c.gold); $('#stat-hp').textContent = `${fmt(c.currentHP)} / ${fmt(c.maxHP)}`; $('#hp-bar').style.width = `${Math.max(0,Math.min(100,c.currentHP/c.maxHP*100))}%`;
  $('#stat-location').textContent = s.map.name; $('#stat-zone').textContent = s.map.isSafeZone ? 'Khu vực an toàn' : 'Hãy cảnh giác với quái vật';
  $('#season-label').textContent = '✧ Mùa ' + w.season; $('#world-clock').textContent = new Date().toLocaleTimeString('vi-VN',{hour:'2-digit',minute:'2-digit'});
  $('#weather-type').textContent = weatherNames[w.type] || w.type; $('#weather-region').textContent = regionNames[s.map.regionId] || s.map.name;
  $('#weather-icon').textContent = /Rain|Storm/.test(w.type) ? '☂' : /Snow/.test(w.type) ? '❄' : /Fog/.test(w.type) ? '≋' : '☀';
  $('#weather-crop').textContent = `Mùa màng ×${fmt(w.cropModifier)}`; $('#weather-move').textContent = `Di chuyển ×${fmt(w.movementModifier)}`;
  $('#world-npcs').replaceChildren(...(s.npcs.length ? s.npcs.map(n => row(npcIcons[n.type] || '♙',n.name,({Merchant:'Thương nhân',Chef:'Đầu bếp',Farmer:'Nông dân',Trainer:'Người thầy',Blacksmith:'Thợ rèn'})[n.type] || n.type, button('Gặp gỡ',async () => $('[data-view='+(n.shopItems.length ? 'trade':'skills')+']').click(),'small ghost'))) : [empty('Miền hoang dã không có dân cư.')]));
  renderDestinations(); renderMonsters(); renderCharacter(); renderInventory(); renderFarm(); renderShops(); renderSkills();
  if($('.nav button.active')?.dataset.view === 'community') loadCommunity().catch(e => toast(e.message,true));
  $('#rest-button').disabled = !s.map.isSafeZone;
}
function renderDestinations() {
  const s = gameState;
  $('#destinations').replaceChildren(...s.maps.filter(m => m.id !== s.map.id).map(m => {
    const locked = s.character.level < m.minLevel;
    const b = button(locked ? `Cấp ${m.minLevel}` : 'Lên đường', () => act('travel',{mapId:m.id}), 'small'); b.disabled = locked;
    return row(m.isSafeZone ? '⌂' : '♧',m.name,`${fmt(m.travelCost)} vàng · ${m.isSafeZone?'An toàn':'Vùng khám phá'}`,b);
  }));
}
function renderMonsters() {
  const monsters = gameState.monsters;
  $('#monsters').replaceChildren(...(monsters.length ? monsters.map(m => row('♟',m.name,`Cấp ${m.level} · ${m.hp} HP · ${m.gold} vàng · ${m.experience} XP`,button('Chiến đấu',() => act('fight',{monsterId:m.id}),'small primary'))) : [empty('Nơi này yên bình. Hãy đến Rừng Ven Làng để thử sức.')]));
}
function renderCharacter() {
  const c = gameState.character;
  $('#character-details').replaceChildren(el('div',{className:'character-summary'},el('div',{className:'avatar-panel',text:'♙'}),el('div',{},el('h3',{text:c.name}),pill(`Nhà lữ hành cấp ${c.level}`))),el('div',{className:'detail-list'},...[
    ['Kinh nghiệm',`${fmt(c.experience)} / ${fmt(c.experienceToNextLevel)}`],['Tấn công',c.stats?.attack ?? '—'],['Phòng thủ',c.stats?.defense ?? '—'],['Sinh lực',`${c.currentHP} / ${c.maxHP}`],['Địa điểm',gameState.map.name]
  ].map(([k,v]) => el('div',{},el('span',{className:'muted',text:k}),el('strong',{text:v})))));
}
function renderInventory() {
  const inv = gameState.inventory.filter(x => x.quantity > 0); $('#inventory-count').textContent = `${inv.length} LOẠI VẬT PHẨM`;
  $('#inventory').replaceChildren(...(inv.length ? inv.map(i => {
    const actions = el('div',{className:'summary-actions'});
    if(i.category === 'Food' || i.category === 'Consumable' || i.itemId === 7) actions.append(button('Sử dụng',() => act('consume',{inventoryId:i.id})));
    if(i.category === 'Equipment' || i.category === 'Weapon' || i.category === 'Armor') actions.append(button(i.isEquipped?'Đã trang bị':'Trang bị',() => act('equip',{inventoryId:i.id})));
    actions.append(pill('× '+fmt(i.quantity)));
    return row(i.category === 'Food'?'♨':i.category === 'Equipment'?'⚔':'❧',i.name,i.isEquipped ? 'Đang trang bị' : i.rarity,actions);
  }) : [empty('Hành trang đang trống.')]));
}
function renderFarm() {
  const s = gameState;
  options($('#crop-select'),s.crops,'id',c => `${c.name} · khoảng ${c.growthTimeMinutes} phút`);
  const plots = s.plots.filter(p => !p.isHarvested);
  $('#plots').replaceChildren(...(plots.length ? plots.map(p => {
    const readyAt = new Date(p.readyAt.endsWith('Z')?p.readyAt:p.readyAt+'Z'), remain = Math.max(0,Math.ceil((readyAt-new Date())/1000));
    const b = button(p.ready ? 'Thu hoạch' : `${remain}s`,() => act('harvest',{plotId:p.id}),p.ready?'small primary':'small'); b.disabled = !p.ready;
    return row('♧',p.name,`Dự kiến ${p.expectedYield} sản phẩm · ${regionNames[p.regionId] || 'Nông trại'}`,b);
  }) : [empty('Mảnh vườn còn trống. Gieo những hạt giống đầu tiên nhé.')]));
  $('#recipes').replaceChildren(...s.recipes.map(r => el('div',{className:'action-card'},el('div',{className:'row'},el('div',{},el('h3',{text:r.name}),el('small',{text:r.ingredients.map(i => `${i.quantity} × ${i.itemId === 1?'Lúa Mì':(s.inventory.find(x => x.itemId === i.itemId)?.name || 'Nguyên liệu #'+i.itemId)}`).join(', ')})),el('div',{className:'row-icon',text:'♨'})),el('p',{text:'Chế biến tại khu vực an toàn. Món ăn được thêm vào hành trang.'}),button('Nhóm bếp & nấu',() => act('cook',{recipeId:r.id}),'small primary'))));
}
function renderShops() {
  const npcs = gameState.npcs.filter(n => n.shopItems.length);
  $('#shops').replaceChildren(...(npcs.length ? npcs.map(n => {
    const box = el('div',{className:'action-card'},el('h3',{text:n.name}),el('small',{text:'Thương nhân địa phương'}));
    for(const i of n.shopItems) {
      const quantity = el('input',{type:'number',className:'qty-input',value:'1',min:'1',max:'99',step:'1','aria-label':'Số lượng '+i.name});
      const actions = el('div',{className:'summary-actions'},quantity);
      if(i.isSell) actions.append(button(`Mua ${fmt(i.buyPrice)} ◉`,() => act('trade',{npcId:n.id,itemId:i.itemId,quantity:Number(quantity.value),buy:true}),'small primary'));
      if(i.isBuy) actions.append(button(`Bán ${fmt(i.sellPrice)} ◉`,() => act('trade',{npcId:n.id,itemId:i.itemId,quantity:Number(quantity.value),buy:false}),'small'));
      box.append(el('div',{className:'row shop-row'},el('div',{},el('div',{className:'row-title',text:i.name}),el('small',{text:`Kho hàng: ${fmt(i.stock)} · Trong túi: ${fmt(gameState.inventory.filter(x=>x.itemId===i.itemId).reduce((a,x)=>a+x.quantity,0))}`})),actions));
    }
    return box;
  }) : [empty('Không có thương nhân tại đây. Hãy trở về làng hoặc đến thành phố.')]));
}
function renderSkills() {
  $('#skills').replaceChildren(...gameState.skills.map(s => {
    const trainer = gameState.npcs.find(n => n.id === s.trainerNpcId), box = el('div',{className:'action-card'},el('div',{className:'card-head'},el('h3',{text:s.name}),pill(s.isUnlocked?'Đã lĩnh hội':'Đang học hỏi',s.isUnlocked?'':'amber')),el('p',{text:s.description}));
    for(const r of s.requirements) { const progress = el('span'); progress.style.width = Math.min(100,r.currentCount/r.requiredCount*100)+'%'; box.append(el('div',{},el('div',{className:'progress-line'},el('span',{text:actionNames[r.actionType] || r.actionType}),el('span',{text:`${Math.min(r.currentCount,r.requiredCount)} / ${r.requiredCount}`})),el('div',{className:'bar'},progress))); }
    const ready = s.requirements.every(r => r.currentCount >= r.requiredCount);
    if(!s.isUnlocked) { const b = button(trainer ? `Học từ ${trainer.name}` : `Tìm người thầy #${s.trainerNpcId}`,() => act('learn',{npcId:s.trainerNpcId,skillId:s.id}),'small primary section-gap'); b.disabled = !trainer || !ready; box.append(b); }
    return box;
  }));
}
formHandler('#create-character',async f => { const result = await api('/api/character/create',{name:value(f,'name'),classId:num(f,'classId'),raceId:num(f,'raceId')}); characterId = result.characterId || result.id || characterId; f.reset(); await loadCharacters(); toast('Nhân vật mới đã đến Làng Tân Thủ.'); });
formHandler('#plant-form',f => act('plant',{cropId:num(f,'cropId')}));
$('#refresh-plots').addEventListener('click',e => run(e.currentTarget,loadState)); $('#rest-button').addEventListener('click',e => run(e.currentTarget,() => act('rest',{})));
async function loadMail() {
  if(!sessionStorage.getItem(tokenKey)) return;
  const [mail,events] = await Promise.all([api('/api/player/mail'),api('/api/player/events')]);
  $('#mail-list').replaceChildren(...(mail.length?mail.map(m => el('article',{className:'mail-card'},el('div',{className:'card-head'},el('h3',{text:m.title}),pill('Hệ thống')),el('p',{text:m.content}),el('small',{text:dateFmt(m.startAt)}))):[empty('Hộp thư của bạn chưa có thông báo.')]));
  $('#events-list').replaceChildren(...(events.length?events.map(m => el('article',{className:'mail-card'},el('h3',{text:m.name}),el('p',{text:m.description}),el('small',{text:'Đến '+dateFmt(m.endsAt)}))):[empty('Hiện chưa có sự kiện đang diễn ra.')]));
}
formHandler('#redeem-form',async f => { const result = await api('/api/player/giftcodes/redeem',{characterId,code:value(f,'code')}); toast(result?.message || 'Đã nhận quà.'); f.reset(); await loadState(); });
async function loadTickets() {
  const tickets = await api('/api/player/tickets');
  $('#tickets-list').replaceChildren(...(tickets.length ? tickets.map(t => el('article',{className:'mail-card'},el('div',{className:'card-head'},el('h3',{text:t.subject}),pill(t.status===2?'Đã giải quyết':'Đang hỗ trợ',t.status===2?'':'amber')),el('p',{text:t.message}),el('small',{text:`#${t.id} · ${dateFmt(t.createdAt)}`}),...(t.messages || []).map(m => el('p',{className:'support-message',text:`${m.senderType === 'Admin'?'Hỗ trợ':'Bạn'}: ${m.message}`})))):[empty('Bạn chưa gửi yêu cầu hỗ trợ nào.')]));
}
formHandler('#support-form',async f => { const result = await api('/api/player/tickets',{category:value(f,'category'),subject:value(f,'subject'),message:value(f,'message'),characterId:characterId || null}); toast(result?.message || 'Đã gửi yêu cầu hỗ trợ.'); f.reset(); await loadTickets(); });
async function loadCommunity() {
  if(!characterId || !gameState) { $('#market-listings').replaceChildren(empty('Tạo nhân vật trước khi tham gia giao thương.')); return; }
  const [listings,guilds,gacha] = await Promise.all([api('/api/community/listings?regionId='+gameState.map.regionId),api('/api/community/guilds'),api('/api/player/gacha')]);
  $('#market-listings').replaceChildren(...(listings.length ? listings.map(l => {
    const owned = characters.some(c=>c.id===l.sellerCharacterId), actions = el('div',{className:'summary-actions'});
    if(l.sellerCharacterId===characterId) actions.append(button('Hủy bán',async()=>{ const r = await api(`/api/community/listings/${l.id}/cancel`,{characterId}); toast(r.message || 'Đã hủy bán.'); await loadState(); }));
    else if(owned) actions.append(pill('Nhân vật của bạn','amber'));
    else { const q=el('input',{type:'number',className:'qty-input',value:'1',min:'1',max:l.quantity,step:'1','aria-label':'Số lượng mua '+l.itemName}); actions.append(q,button('Mua',async()=>{ const r=await api(`/api/community/listings/${l.id}/buy`,{characterId,quantity:Number(q.value)}); toast(r.message || 'Đã mua hàng.'); await loadState(); },'small primary')); }
    return el('div',{className:'row shop-row'},el('div',{},el('div',{className:'row-title',text:`${l.itemName} ×${l.quantity}`}),el('small',{text:`${fmt(l.unitPrice)} vàng / món · ${l.sellerName}`})),actions);
  }):[empty('Chưa có hàng đăng bán tại vùng này. Bạn có thể là người đầu tiên.')]));
  const uniqueInventory = [...new Map(gameState.inventory.filter(i=>i.quantity>0&&!i.isEquipped&&i.itemId!==4).map(i=>[i.itemId,{id:i.itemId,name:i.name}])).values()]; options($('#listing-item'),uniqueInventory);
  const membership=guilds.find(g=>g.members.some(m=>m.characterId===characterId));
  $('#guilds-list').replaceChildren(...(guilds.length?guilds.map(g=>{
    const own = g.members.some(m=>m.characterId===characterId), box=el('article',{className:'mail-card'},el('div',{className:'card-head'},el('h3',{text:g.name}),pill(own?'Hội của bạn':`${g.members.length} thành viên`)),el('p',{text:`Hội trưởng: ${g.leaderName} · ${g.regionName}`}),el('small',{text:g.members.map(m=>`${m.name} (${({Leader:'Hội trưởng',Driver:'Lái xe',Scout:'Trinh sát',Guard:'Hộ tống',Member:'Thành viên'})[m.role] || m.role})`).join(', ')}));
    if(!membership) { const role=el('select',{'aria-label':'Vai trò trong '+g.name},...[['Driver','Lái xe'],['Scout','Trinh sát'],['Guard','Hộ tống'],['Member','Thành viên']].map(([v,t])=>el('option',{value:v,text:t}))); box.append(el('div',{className:'inline-form section-gap'},el('label',{},'Vai trò',role),button('Gia nhập',async()=>{ const r=await api(`/api/community/guilds/${g.id}/join`,{characterId,role:role.value}); toast(r.message || 'Đã gia nhập thương đoàn.'); await loadCommunity(); },'small primary'))); }
    return box;
  }):[empty('Chưa có thương đoàn. Hãy quy tụ những người bạn đầu tiên.')]));
  $('#gacha-rewards').replaceChildren(table(['Phần thưởng','Tỷ lệ'],gacha.rewards.map(r=>[r.itemName,fmt(r.chance*100)+'%']))); $('#gacha-pull').textContent='Mở rương · '+fmt(gacha.cost)+' vàng';
}
formHandler('#listing-form',async f=>{ if(!characterId) throw new Error('Hãy chọn nhân vật.'); const r=await api('/api/community/listings',{characterId,itemId:num(f,'itemId'),quantity:num(f,'quantity'),unitPrice:num(f,'unitPrice')}); toast(r.message || 'Đã đưa hàng lên chợ.'); await loadState(); });
formHandler('#guild-form',async f=>{ if(!characterId) throw new Error('Hãy chọn nhân vật.'); const r=await api('/api/community/guilds',{characterId,name:value(f,'name')}); toast(r.message || 'Đã thành lập thương đoàn.'); f.reset(); await loadCommunity(); });
$('#refresh-community').addEventListener('click',e=>run(e.currentTarget,loadCommunity));
$('#gacha-pull').addEventListener('click',e=>run(e.currentTarget,async()=>{ if(!characterId) throw new Error('Hãy chọn nhân vật.'); const r=await api('/api/player/gacha/pull',{characterId}); toast(r.message || `Bạn nhận được ${r.itemName}.`); addLog(r.message || `Mở rương nhận ${r.itemName}.`); await loadState(); }));
setInterval(() => { if(sessionStorage.getItem(tokenKey)) api('/api/auth/heartbeat',{}).catch(() => {}); },60000);
setInterval(() => { if(sessionStorage.getItem(tokenKey) && characterId && !document.hidden && ['world','farm'].includes($('.nav button.active')?.dataset.view)) loadState().catch(() => {}); },30000);
if(sessionStorage.getItem(tokenKey)) { $('#auth-overlay').classList.add('hidden'); enterWorld().catch(e => { toast(e.message,true); if(!sessionStorage.getItem(tokenKey)) $('#auth-overlay').classList.remove('hidden'); }); }
