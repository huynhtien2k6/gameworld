'use strict';
let selectedAccount = null, selectedTicket = null, accountRecords = [], ticketRecords = [], items = [], gachaEntries = [], overview = null;
const iso = (form,name) => value(form,name) ? new Date(value(form,name)).toISOString() : null;
const localDateInput = date => { const d = new Date(date); return new Date(d.getTime()-d.getTimezoneOffset()*60000).toISOString().slice(0,16); };
const nowValue = localDateInput(new Date()), tomorrowValue = localDateInput(Date.now()+86400000);
$('#event-form').elements.startsAt.value = nowValue; $('#event-form').elements.endsAt.value = tomorrowValue; $('#announcement-form').elements.startAt.value = nowValue;
setupNav(view => run(null, () => loadView(view)));
$('#admin-login').addEventListener('submit',async e => {
  e.preventDefault(); const f = e.currentTarget, b = $('button[type=submit]',f); b.disabled = true; $('#auth-error').classList.add('hidden');
  try { const result = await api('/api/auth/admin/login',{username:value(f,'username'),password:f.elements.password.value},'POST',true); sessionStorage.setItem(tokenKey,result.token); $('#account-name').textContent = result.username; f.reset(); $('#auth-overlay').classList.add('hidden'); await enterAdmin(); }
  catch(error) { $('#auth-error').textContent = error.message; $('#auth-error').classList.remove('hidden'); }
  finally { b.disabled = false; }
});
$('#logout').addEventListener('click',() => { sessionStorage.removeItem(tokenKey); location.reload(); });
$('#go-support').addEventListener('click',() => $('[data-view=support]').click());
$('#refresh-all').addEventListener('click',e => run(e.currentTarget,async () => { await loadOverview(); const view = $('.nav button.active')?.dataset.view || 'overview'; if(view !== 'overview') await loadView(view); toast('Đã cập nhật dữ liệu từ máy chủ.'); }));
async function enterAdmin() {
  const me = await api('/api/auth/me'); $('#account-name').textContent = me.username || 'Quản trị viên'; $('#connection-status').textContent = 'Đã kết nối máy chủ';
  await loadOverview();
}
async function loadView(view) {
  if(!sessionStorage.getItem(tokenKey)) return;
  if(view === 'overview') await loadOverview();
  else if(view === 'players') await Promise.all([loadAccounts(),loadItems()]);
  else if(view === 'economy') await Promise.all([loadItems(),loadGacha()]);
  else if(view === 'support') await loadTickets();
  else if(view === 'liveops') { await loadItems(); await loadLiveops(); }
  else if(view === 'logs') await loadLogs();
}
function renderLogs(container,logs) {
  container.replaceChildren(...(logs.length ? logs.map(x => el('div',{className:'log-entry'},el('time',{text:dateFmt(x.createdAt)}),pill(x.level,x.level==='Error'?'red':''),el('span',{text:` ${x.source}: ${x.message}`}))) : [empty('Chưa ghi nhận log hệ thống.')]));
}
async function loadOverview() {
  overview = await api('/api/admin/overview');
  $('#kpi-ccu').textContent = fmt(overview.ccu); $('#kpi-revenue').textContent = fmt(overview.revenueToday); $('#kpi-tickets').textContent = fmt(overview.openTickets); $('#kpi-errors').textContent = fmt(overview.errorsToday);
  $('#kpi-time').textContent = new Date().toLocaleTimeString('vi-VN'); $('#health-pill').textContent = 'Máy chủ phản hồi';
  $('#world-health').replaceChildren(...[['Tài khoản đã đăng ký',fmt(overview.accounts)],['Nhân vật trong thế giới',fmt(overview.characters)],['Vàng đang lưu hành',fmt(overview.totalGold)+' ◉'],['Thời gian máy chủ',dateFmt(overview.serverTime)],['Thời gian hoạt động',fmt(Math.floor(overview.uptimeSeconds/60))+' phút']].map(([k,v])=>el('div',{},el('span',{className:'muted',text:k}),el('strong',{text:v}))));
  $('#revenue-table').replaceChildren(table(['Ngày (7 ngày gần nhất)','Doanh thu'],(overview.revenue||[]).slice(-7).map(r=>[new Date(r.date+'T00:00:00Z').toLocaleDateString('vi-VN'),fmt(r.amount)])));
  renderLogs($('#overview-logs'),overview.logs||[]); renderLogs($('#system-logs'),overview.logs||[]);
  $('#last-refresh').textContent = 'Cập nhật lúc '+new Date().toLocaleTimeString('vi-VN');
}
async function loadAccounts(search = value($('#search-form'),'search')) {
  accountRecords = await api('/api/admin/accounts?search='+encodeURIComponent(search));
  $('#accounts-table').replaceChildren(table(['Tài khoản','Email','Trạng thái','Vàng','Nhân vật',''],accountRecords.map(a=>[
    el('div',{},el('strong',{text:a.username}),el('div',{className:'badge-id',text:'#'+a.id})),a.email,pill(a.status===2?'Đã khóa':'Hoạt động',a.status===2?'red':''),fmt(a.gold),a.characters.length,button('Chi tiết',async()=>selectAccount(a))
  ])));
  if(selectedAccount) { const updated = accountRecords.find(a=>a.id===selectedAccount.id); if(updated) selectAccount(updated); }
}
function selectAccount(account) {
  selectedAccount = account; $('#account-panel').classList.remove('hidden'); $('#selected-account').textContent = account.username+' · #'+account.id; $('#selected-status').replaceChildren(pill(account.status===2?'Đã khóa':'Hoạt động',account.status===2?'red':''));
  $('#account-details').replaceChildren(table(['Nhân vật','Cấp','Bản đồ','ID'],account.characters.map(c=>[c.name,c.level,c.mapId,c.id])));
  $$('.account-characters').forEach(s=>options(s,account.characters,'id',c=>`${c.name} · #${c.id}`));
}
formHandler('#search-form',f=>loadAccounts(value(f,'search')));
formHandler('#ban-form',async f=>{ if(!selectedAccount) throw new Error('Hãy chọn tài khoản.'); await api(`/api/admin/accounts/${selectedAccount.id}/ban`,{reason:value(f,'reason'),until:iso(f,'until')}); toast('Đã khóa tài khoản và thu hồi các phiên đăng nhập.'); await loadAccounts(); await loadOverview(); });
$('#unban-button').addEventListener('click',e=>run(e.currentTarget,async()=>{ const f = $('#ban-form'); if(!f.elements.reason.reportValidity()) return; if(!selectedAccount) throw new Error('Hãy chọn tài khoản.'); await api(`/api/admin/accounts/${selectedAccount.id}/unban`,{reason:value(f,'reason')}); toast('Đã mở khóa tài khoản.'); await loadAccounts(); }));
formHandler('#restore-form',async f=>{ await api(`/api/admin/characters/${num(f,'characterId')}/restore`,{itemId:num(f,'itemId'),quantity:num(f,'quantity'),reason:value(f,'reason')}); toast('Đã khôi phục vật phẩm và ghi nhật ký.'); });
formHandler('#currency-form',async f=>{ await api(`/api/admin/characters/${num(f,'characterId')}/currency`,{amount:num(f,'amount'),reason:value(f,'reason')}); toast('Đã ghi nhận điều chỉnh tiền tệ.'); await loadAccounts(); await loadOverview(); });
async function loadItems() {
  items = await api('/api/admin/items');
  $$('.item-select').forEach(s=>options(s,items,'id',i=>`${i.name} · #${i.id}`));
  $('#items-table').replaceChildren(table(['Vật phẩm','Phân loại','Giá'],items.map(i=>[i.name,i.category,fmt(i.basePrice)+' ◉'])));
  syncPrice();
}
function syncPrice() { const f=$('#price-form'),i=items.find(i=>i.id===num(f,'itemId')); if(i) f.elements.price.value=i.basePrice; }
$('#price-form').elements.itemId.addEventListener('change',syncPrice);
formHandler('#price-form',async f=>{ await api(`/api/admin/items/${num(f,'itemId')}/price`,{price:num(f,'price'),reason:value(f,'reason')},'PUT'); toast('Đã cập nhật giá cơ sở vật phẩm.'); await loadItems(); });
async function loadGacha() {
  gachaEntries = await api('/api/admin/gacha'); const total = gachaEntries.filter(x=>x.isActive).reduce((a,x)=>a+x.weight,0);
  $('#gacha-table').replaceChildren(table(['Phần thưởng','Trọng số','Tỷ lệ','Trạng thái'],gachaEntries.map(g=>[g.itemName,fmt(g.weight),g.isActive && total?fmt(g.weight/total*100)+'%':'0%',pill(g.isActive?'Đang bật':'Đã tắt',g.isActive?'':'amber')])));
  options($('#gacha-select'),gachaEntries,'id','itemName'); syncGacha();
}
function syncGacha() { const f=$('#gacha-form'),g=gachaEntries.find(x=>x.id===num(f,'id')); if(g) { f.elements.weight.value=g.weight; f.elements.isActive.checked=g.isActive; } }
$('#gacha-select').addEventListener('change',syncGacha);
formHandler('#gacha-form',async f=>{ await api(`/api/admin/gacha/${num(f,'id')}`,{weight:num(f,'weight'),isActive:f.elements.isActive.checked,reason:value(f,'reason')},'PUT'); toast('Đã cập nhật trọng số Gacha.'); await loadGacha(); });
async function loadTickets() {
  ticketRecords=await api('/api/admin/tickets');
  $('#tickets-list').replaceChildren(...(ticketRecords.length?ticketRecords.map(t=>row('✉',t.subject,`#${t.id} · Tài khoản #${t.accountId} · ${dateFmt(t.createdAt)}`,button(t.status===2?'Đã xử lý':'Xem',async()=>selectTicket(t)))):[empty('Chưa có yêu cầu hỗ trợ.')]));
  if(selectedTicket) { const updated=ticketRecords.find(t=>t.id===selectedTicket.id); if(updated) selectTicket(updated); }
}
function selectTicket(t) {
  selectedTicket=t; $('#reply-form').classList.remove('hidden');
  $('#ticket-detail').replaceChildren(el('h3',{text:t.subject}),pill(t.status===2?'Đã giải quyết':'Đang hỗ trợ',t.status===2?'':'amber'),el('p',{className:'section-intro section-gap',text:`Tài khoản #${t.accountId} · ${t.category}`}),el('p',{className:'support-message',text:t.message}),...(t.messages||[]).map(m=>el('p',{className:'support-message',text:`${m.senderType==='Admin'?'Quản trị':'Người chơi'}: ${m.message}`})));
  $('#reply-form').elements.resolve.checked=t.status===2;
}
formHandler('#reply-form',async f=>{ if(!selectedTicket) throw new Error('Chọn yêu cầu hỗ trợ trước.'); await api(`/api/admin/tickets/${selectedTicket.id}/reply`,{message:value(f,'message'),resolve:f.elements.resolve.checked}); f.elements.message.value=''; toast('Đã gửi phản hồi hỗ trợ.'); await loadTickets(); await loadOverview(); });
$$('[data-live-tab]').forEach(b=>b.addEventListener('click',()=>{ $$('[data-live-tab]').forEach(x=>x.classList.toggle('active',x===b)); $$('.live-panel').forEach(x=>x.classList.toggle('hidden',x.id!=='live-'+b.dataset.liveTab)); }));
async function loadLiveops() {
  const [events,announcements,gifts]=await Promise.all([api('/api/admin/events'),api('/api/admin/announcements'),api('/api/admin/giftcodes')]);
  $('#events-list').replaceChildren(...(events.length?events.map(e=>el('article',{className:'mail-card'},el('div',{className:'card-head'},el('h3',{text:e.name}),pill(e.isActive?'Đang bật':'Đã tắt',e.isActive?'':'amber')),el('p',{text:e.description}),el('small',{text:`${dateFmt(e.startsAt)} → ${dateFmt(e.endsAt)}`}),el('div',{className:'section-gap'},button(e.isActive?'Tắt sự kiện':'Bật sự kiện',async()=>{ await api(`/api/admin/events/${e.id}/toggle`,{isActive:!e.isActive},'PUT'); toast('Đã cập nhật trạng thái sự kiện.'); await loadLiveops(); })))):[empty('Chưa có sự kiện được tạo.')]));
  $('#announcements-list').replaceChildren(...(announcements.length?announcements.map(m=>el('article',{className:'mail-card'},el('h3',{text:m.title}),el('p',{text:m.content}),el('small',{text:dateFmt(m.startAt)}))):[empty('Chưa có thông báo hệ thống.')]));
  $('#giftcodes-list').replaceChildren(table(['Mã','Quà','Đã nhận','Hết hạn'],gifts.map(g=>[g.code,`${g.rewardType==='Gold'?'Vàng':(items.find(i=>i.id===g.rewardItemId)?.name || 'Vật phẩm #'+g.rewardItemId)} ×${fmt(g.rewardAmount)}`,`${fmt(g.usedCount)} / ${fmt(g.maxUsage)}`,dateFmt(g.validTo)])));
}
formHandler('#event-form',async f=>{ await api('/api/admin/events',{name:value(f,'name'),description:value(f,'description'),startsAt:iso(f,'startsAt'),endsAt:iso(f,'endsAt')}); toast('Đã tạo sự kiện.'); await loadLiveops(); });
formHandler('#announcement-form',async f=>{ await api('/api/admin/announcements',{title:value(f,'title'),content:value(f,'content'),startAt:iso(f,'startAt'),endAt:iso(f,'endAt')}); toast('Đã phát thông báo hệ thống.'); await loadLiveops(); });
$('#gift-type').addEventListener('change',e=>$('#gift-item-label').classList.toggle('hidden',e.target.value!=='Item'));
formHandler('#giftcode-form',async f=>{ await api('/api/admin/giftcodes',{code:value(f,'code').toUpperCase(),rewardType:value(f,'rewardType'),rewardItemId:value(f,'rewardType')==='Item'?num(f,'rewardItemId'):null,rewardAmount:num(f,'rewardAmount'),maxUsage:num(f,'maxUsage'),validTo:iso(f,'validTo')}); toast('Đã phát hành Giftcode.'); await loadLiveops(); });
async function loadLogs() {
  await loadOverview(); const records=await api('/api/admin/audit');
  $('#audit-logs').replaceChildren(table(['Thời gian','Quản trị','Thao tác','Đối tượng','Chi tiết'],records.map(r=>[dateFmt(r.createdAt),r.adminUsername || r.adminId,r.actionType,r.targetAccountId?'Tài khoản #'+r.targetAccountId:r.targetCharacterId?'Nhân vật #'+r.targetCharacterId:'Hệ thống',el('span',{className:'wrap-details',text:typeof r.details==='string'?r.details:JSON.stringify(r.details || {})})])));
}
if(sessionStorage.getItem(tokenKey)) { $('#auth-overlay').classList.add('hidden'); enterAdmin().catch(e=>{ toast(e.message,true); if(!sessionStorage.getItem(tokenKey)) $('#auth-overlay').classList.remove('hidden'); }); }
