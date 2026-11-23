function getStoredToken() {
  const t = sessionStorage.getItem('rc-token');
  return (t && t !== 'undefined' && t !== 'null') ? t : null;
}
function getStoredUser() {
  try {
    const raw = sessionStorage.getItem('rc-user');
    if (!raw || raw === 'undefined' || raw === 'null') return null;
    return JSON.parse(raw);
  } catch {
    return null;
  }
}
const state = { token: getStoredToken(), user: getStoredUser(), page: 'overview', roles: [], data: {} };
const pageMeta = {
  overview:['TỔNG QUAN','Tổng quan','◫'], tickets:['NGHIỆP VỤ','Phiếu sửa chữa','▤'], customers:['DANH MỤC','Khách hàng & thiết bị','♙'], deviceTypes:['DANH MỤC','Loại thiết bị','▱'], services:['DANH MỤC','Dịch vụ sửa chữa','🔧'], technicians:['NHÂN SỰ','Kỹ thuật viên','⚙'], inventory:['KHO VẬT TƯ','Kho phụ tùng','▦'], billing:['THU NGÂN','Hóa đơn & thanh toán','＄'], warranties:['HẬU MÃI','Bảo hành','◇'], accounts:['QUẢN TRỊ','Tài khoản','♧']
};
const statusLabels={Received:'Mới tiếp nhận',Diagnosing:'Đang kiểm tra',WaitingForApproval:'Chờ khách duyệt',InProgress:'Đang sửa',WaitingForParts:'Chờ linh kiện',Completed:'Hoàn tất',Delivered:'Đã giao',Cancelled:'Đã hủy'};
const enumValues={1:'Received',2:'Diagnosing',3:'WaitingForApproval',4:'InProgress',5:'WaitingForParts',6:'Completed',7:'Delivered',8:'Cancelled'};
const warrantyValues={1:'Active',2:'Expired',3:'Claimed',4:'Voided'};
const warrantyDurationUnitValues={1:'Days',2:'Months'};
const claimValues={1:'Pending',2:'Approved',3:'Rejected',4:'Cancelled',5:'Assigned'};
const paymentValues={1:'Pending',2:'PartiallyPaid',3:'Paid',4:'Pending',5:'Overdue'};
const badgeClass={Received:'blue',Diagnosing:'amber',WaitingForApproval:'amber',InProgress:'blue',WaitingForParts:'amber',Completed:'green',Delivered:'green',Cancelled:'red',Paid:'green',PartiallyPaid:'blue',Unpaid:'amber',Overdue:'red',Active:'green',Expired:'amber',Voided:'red',Claimed:'blue',Pending:'amber',Approved:'green',Rejected:'red',Cancelled:'red'};
const errText = e => e?.message || 'Có lỗi xảy ra. Vui lòng thử lại.';
const money = v => `${Number(v || 0).toLocaleString('vi-VN')} đ`;
const percent = v => `${Number(v || 0)}%`;
const date = v => {
  if (!v) return '—';
  const d = new Date(v);
  return isNaN(d.getTime()) ? '—' : new Intl.DateTimeFormat('vi-VN', { dateStyle: 'short', timeStyle: 'short' }).format(d);
};
function esc(v){return String(v??'').replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]))}
function warrantyDurationText(value,unit){return `${value} ${Number(unit)===1?'ngày':'tháng'}`}
function toast(message,bad=false){window.dispatchEvent(new CustomEvent('repair-center-toast',{detail:{message,bad}}))}
async function api(path, options={}){
  const headers={...(options.body?{'Content-Type':'application/json'}:{}),...(state.token?{Authorization:`Bearer ${state.token}`}:{})};
  const isTicketMutation=String(options.method||'GET').toUpperCase()!=='GET'&&path.startsWith('/api/repair-tickets');
  const notifyTicketMutation=()=>{if(isTicketMutation)window.dispatchEvent(new CustomEvent('repair-tickets-changed',{detail:{path,method:String(options.method||'GET').toUpperCase()}}))};
  const response=await fetch(path,{...options,headers:{...headers,...options.headers},body:options.body?JSON.stringify(options.body):undefined});
  if(response.status===401){logout(false);throw new Error('Phiên đăng nhập hết hạn. Vui lòng đăng nhập lại.')}
  if(response.status===204){notifyTicketMutation();return null;}
  const text=await response.text();let data=null;try{data=text?JSON.parse(text):null}catch{data=text}
  if(!response.ok){
    const validationErrors=data?.errors&&typeof data.errors==='object'
      ?Object.entries(data.errors).flatMap(([field,messages])=>(Array.isArray(messages)?messages:[messages]).filter(Boolean).map(message=>`${field}: ${message}`))
      :[];
    const message=validationErrors.length?validationErrors.join(' · '):(data?.detail||data?.message||data?.title||data||`Yêu cầu thất bại (${response.status})`);
    const error=new Error(typeof message==='string'?message:JSON.stringify(message));error.status=response.status;throw error;
  }
  notifyTicketMutation();
  return data;
}
function tokenRoles(token,fallback=[]){try{const p=JSON.parse(atob(token.split('.')[1].replace(/-/g,'+').replace(/_/g,'/')));let r=p['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']||p['http://schemas.microsoft.com/ws/2008/06/identity/claims/roles']||p.role||p.roles||[];const roles=(Array.isArray(r)?r:[r]).filter(Boolean);return roles.length?roles:fallback}catch{return fallback}}
function can(...roles){return roles.some(r=>state.roles.includes(r))}
function init(){
  state.token = getStoredToken();
  state.user = getStoredUser();
  if(state.token && state.user){
    state.roles = tokenRoles(state.token, state.user?.roles || []);
    state.user?.mustChangePassword ? showPasswordChange() : showApp();
  } else {
    logout(false);
  }
}
function showLogin(){window.dispatchEvent(new CustomEvent("repair-center-auth-state",{detail:{authenticated:false,mode:"login"}}));window.dispatchEvent(new CustomEvent("repair-center-auth"))}
function showApp(){const mode=state.user?.mustChangePassword?"change":"app";window.dispatchEvent(new CustomEvent("repair-center-auth-state",{detail:{authenticated:mode==="app",mode}}));window.dispatchEvent(new CustomEvent("repair-center-auth"));if(mode==="app"){const ticketCode=new URLSearchParams(location.search).get("ticket");if(ticketCode){history.replaceState(null,"",location.pathname);openTicketByNumber(ticketCode)}else navigate("overview")}}
async function openTicketByNumber(ticketCode){try{const ticket=await api(`/api/repair-tickets/by-number/${encodeURIComponent(ticketCode)}`);await navigate("tickets");window.openTicketReact?.(ticket.id)}catch(e){toast(errText(e),true)}}
async function login(){throw new Error("Login is handled by the React login form.")}
function showPasswordChange(){window.dispatchEvent(new CustomEvent("repair-center-auth-state",{detail:{authenticated:false,mode:"change"}}))}
async function changeTemporaryPassword(){throw new Error("Password changes are handled by the React login form.")}
async function logout(callApi=true){
  if(callApi&&state.token){
    try{await fetch("/api/auth/revoke",{method:"POST",headers:{Authorization:`Bearer ${state.token}`}})}catch{}
  }
  state.token=null;
  state.user=null;
  state.roles=[];
  sessionStorage.removeItem("rc-token");
  sessionStorage.removeItem("rc-user");
  window.unmountBusinessReact?.();
  showLogin();
}
function buildNav(){window.dispatchEvent(new CustomEvent("repair-center-auth"))}
async function navigate(page){if(!pageMeta[page])return;state.page=page;window.dispatchEvent(new CustomEvent("repair-center-navigate",{detail:{page}}));const mount={overview:window.mountOverviewReact,tickets:window.mountTicketsReact,customers:window.mountCustomersReact,deviceTypes:window.mountDeviceTypesReact,services:window.mountServicesReact,technicians:window.mountTechniciansReact,inventory:window.mountInventoryReact,billing:window.mountBillingReact,warranties:window.mountWarrantiesReact,accounts:window.mountAccountsReact}[page];if(mount)mount()}
function makeQrSvg(value){
  const version=5,size=37,eccCount=26,dataCount=108,bytes=[...new TextEncoder().encode(value)];
  if(bytes.length>106)throw new Error('Đường dẫn tra cứu quá dài để tạo mã QR.');
  const bits=[];const append=(n,count)=>{for(let i=count-1;i>=0;i--)bits.push((n>>>i)&1)};
  append(4,4);append(bytes.length,8);bytes.forEach(b=>append(b,8));
  for(let i=0;i<Math.min(4,dataCount*8-bits.length);i++)bits.push(0);
  while(bits.length%8)bits.push(0);
  const data=[];for(let i=0;i<bits.length;i+=8)data.push(bits.slice(i,i+8).reduce((n,b)=>(n<<1)|b,0));
  for(let pad=0;data.length<dataCount;pad++)data.push(pad%2?0x11:0xec);
  const exp=new Array(512),log=new Array(256);let x=1;for(let i=0;i<255;i++){exp[i]=x;log[x]=i;x<<=1;if(x&0x100)x^=0x11d}for(let i=255;i<512;i++)exp[i]=exp[i-255];
  const mul=(a,b)=>!a||!b?0:exp[log[a]+log[b]];let generator=[1];for(let i=0;i<eccCount;i++){const next=new Array(generator.length+1).fill(0);generator.forEach((c,j)=>{next[j]^=c;next[j+1]^=mul(c,exp[i])});generator=next}
  const work=data.concat(new Array(eccCount).fill(0));for(let i=0;i<dataCount;i++){const factor=work[i];if(factor)generator.forEach((c,j)=>work[i+j]^=mul(c,factor))}const codewords=data.concat(work.slice(dataCount));
  const matrix=Array.from({length:size},()=>Array(size).fill(false)),reserved=Array.from({length:size},()=>Array(size).fill(false));
  const set=(cx,cy,dark)=>{if(cx>=0&&cy>=0&&cx<size&&cy<size){matrix[cy][cx]=!!dark;reserved[cy][cx]=true}};
  const finder=(ox,oy)=>{for(let dy=-1;dy<=7;dy++)for(let dx=-1;dx<=7;dx++){const dark=dx>=0&&dx<=6&&dy>=0&&dy<=6&&(dx===0||dx===6||dy===0||dy===6||(dx>=2&&dx<=4&&dy>=2&&dy<=4));set(ox+dx,oy+dy,dark)}};
  finder(0,0);finder(size-7,0);finder(0,size-7);
  for(let dy=-2;dy<=2;dy++)for(let dx=-2;dx<=2;dx++)set(30+dx,30+dy,Math.abs(dx)===2||Math.abs(dy)===2||(dx===0&&dy===0));
  for(let i=8;i<size-8;i++){if(!reserved[6][i])set(i,6,i%2===0);if(!reserved[i][6])set(6,i,i%2===0)}
  const fmtA=[],fmtB=[];for(let i=0;i<15;i++){if(i<6)fmtA.push([8,i]);else if(i===6)fmtA.push([8,7]);else if(i===7)fmtA.push([8,8]);else if(i===8)fmtA.push([7,8]);else fmtA.push([14-i,8]);if(i<8)fmtB.push([size-1-i,8]);else fmtB.push([8,size-15+i])}fmtA.forEach(([cx,cy])=>set(cx,cy,false));fmtB.forEach(([cx,cy])=>set(cx,cy,false));set(8,size-8,true);
  let bitIndex=0,up=true;for(let right=size-1;right>=1;right-=2){if(right===6)right--;for(let v=0;v<size;v++){const cy=up?size-1-v:v;for(let j=0;j<2;j++){const cx=right-j;if(reserved[cy][cx])continue;let dark=bitIndex<codewords.length*8?((codewords[bitIndex>>>3]>>>(7-(bitIndex&7)))&1)!==0:false;bitIndex++;if((cx+cy)%2===0)dark=!dark;matrix[cy][cx]=dark}}up=!up}
  let format=(1<<3)<<10;for(let i=14;i>=10;i--)if((format>>i)&1)format^=0x537<<(i-10);format=(((1<<3)<<10)|format)^0x5412;
  fmtA.forEach(([cx,cy],i)=>matrix[cy][cx]=((format>>>i)&1)!==0);fmtB.forEach(([cx,cy],i)=>matrix[cy][cx]=((format>>>i)&1)!==0);matrix[size-8][8]=true;
  let path='';for(let y=0;y<size;y++)for(let cx=0;cx<size;cx++)if(matrix[y][cx])path+=`M${cx+4} ${y+4}h1v1h-1z`;
  return `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 ${size+8} ${size+8}" width="38mm" height="38mm" shape-rendering="crispEdges"><rect width="100%" height="100%" fill="#fff"/><path d="${path}" fill="#111"/></svg>`;
}
function enumKey(value,map=enumValues){return typeof value==='number'?map[value]||String(value):value}
function openIntake(){if(can("Admin","Receptionist"))window.openWorkflowDialog?.("intake");}
function ticketDetail(id){if(state.page==="tickets"&&window.openTicketReact){window.openTicketReact(id);return}navigate("tickets").then(()=>window.openTicketReact?.(id))}
async function printTicket(id){
  let t=(state.data.activeTicket?.ticket?.id===id)?state.data.activeTicket.ticket:null;
  if(!t){try{t=await api(`/api/repair-tickets/${id}`)}catch(e){return toast('Không tìm thấy thông tin phiếu để in.',true)}}
  let qr;try{const url=new URL('/',location.origin);url.searchParams.set('ticket',t.ticketNumber);qr=makeQrSvg(url.toString())}catch(e){return toast(errText(e),true)}
  const win=window.open('','_blank');if(!win)return toast('Trình duyệt đã chặn cửa sổ in. Hãy cho phép cửa sổ bật lên rồi thử lại.',true);
  const title=t.isWarrantyRepair?'PHIẾU BÀN GIAO SỬA CHỮA BẢO HÀNH':'PHIẾU BÀN GIAO SỬA CHỮA';
  win.document.open();win.document.write(`<!doctype html><html lang="vi"><head><meta charset="utf-8"><title>${esc(t.ticketNumber)}</title><style>@page{size:A6 portrait;margin:7mm}*{box-sizing:border-box}body{font:12px Arial,sans-serif;color:#172033;margin:0}.sheet{border:1px solid #ccd4e2;border-radius:8px;padding:12px}.head{border-bottom:2px solid #435cf5;padding-bottom:8px;margin-bottom:10px}.head h1{font-size:15px;margin:0 0 5px}.code{font-size:16px;font-weight:bold;letter-spacing:.3px}.meta{line-height:1.55}.label{color:#667085}.issue{border:1px solid #dbe1ea;border-radius:6px;padding:8px;margin:10px 0;min-height:42px}.qrrow{display:flex;align-items:center;justify-content:space-between;gap:8px;margin-top:8px}.qr{width:37mm;height:37mm}.hint{font-size:10px;color:#5b6575}.footer{margin-top:8px;padding-top:7px;border-top:1px dashed #bbc3cf;font-size:10px;color:#596579}@media print{body{print-color-adjust:exact;-webkit-print-color-adjust:exact}}</style></head><body><main class="sheet"><header class="head"><h1>${title}</h1><div class="code">${esc(t.ticketNumber)}</div>${t.isWarrantyRepair?'<div class="hint">Phiếu bảo hành phát sinh</div>':''}</header><section class="meta"><div><span class="label">Khách hàng:</span> <strong>${esc(t.customerName)}</strong></div><div><span class="label">Thiết bị:</span> ${esc(t.deviceType)} · ${esc(t.deviceBrand)} ${esc(t.deviceModel)}</div><div><span class="label">Serial:</span> ${esc(t.serialNumber||'—')}</div><div><span class="label">Ngày tiếp nhận:</span> ${date(t.receivedAt)}</div>${t.parentRepairTicketNumber?`<div><span class="label">Phiếu gốc:</span> ${esc(t.parentRepairTicketNumber)}</div>`:''}</section><div class="issue"><strong>Tình trạng / nội dung:</strong><br>${esc(t.issueDescription)}</div><div class="qrrow"><div class="hint">Quét QR để mở phiếu trong RepairCenter.<br>Mã QR dùng để tra cứu, không thay bước phân công.</div><div class="qr">${qr}</div></div><footer class="footer">Vui lòng đối chiếu mã phiếu, thiết bị và tình trạng khi bàn giao.</footer></main><script>window.onload=()=>setTimeout(()=>window.print(),250)<\/script></body></html>`);win.document.close()
}
function editTicketDetail(ticketId,detailId,quantity,discountPercent){window.openWorkflowDialog?.("editDetail",{ticketId,detailId,quantity,discountPercent})}
async function deleteTicketDetail(ticketId,detailId){if(!confirm('Xóa hạng mục này khỏi phiếu? Nếu là phụ tùng, số lượng sẽ được hoàn về kho.'))return;try{await api(`/api/repair-tickets/${ticketId}/details/${detailId}`,{method:'DELETE'});toast('Đã xóa hạng mục');window.dispatchEvent(new CustomEvent('repair-tickets-changed',{detail:{path:`/api/repair-tickets/${ticketId}/details`,method:'DELETE'}}));ticketDetail(ticketId)}catch(e){toast(errText(e),true)}}
async function exportQuote(ticketId){const printWindow=window.open('','_blank');if(!printWindow){toast('Trình duyệt đang chặn cửa sổ báo giá. Hãy cho phép cửa sổ bật lên rồi thử lại.',true);return}try{const [ticket,details]=await Promise.all([api(`/api/repair-tickets/${ticketId}`),api(`/api/repair-tickets/${ticketId}/details`)]);if(enumKey(ticket.status)!=='WaitingForApproval'||!details.length){printWindow.close();toast('Chỉ xuất báo giá khi phiếu đang chờ khách duyệt và đã có hạng mục.',true);return}const subtotal=details.reduce((sum,x)=>sum+x.quantity*x.unitPrice,0),discount=details.reduce((sum,x)=>sum+x.discountAmount,0),total=details.reduce((sum,x)=>sum+x.totalAmount,0);const html=`<!doctype html><html lang="vi"><head><meta charset="utf-8"><title>Báo giá ${esc(ticket.ticketNumber)}</title><style>body{font:14px Arial,sans-serif;color:#172033;max-width:900px;margin:32px auto;padding:0 24px}h1{margin:0 0 6px;font-size:24px}.muted{color:#667085}.top{display:flex;justify-content:space-between;border-bottom:2px solid #425cf5;padding-bottom:16px}.meta{display:grid;grid-template-columns:1fr 1fr;gap:8px;margin:20px 0}.issue{background:#f5f7fb;padding:12px;border-radius:8px;margin:16px 0}table{border-collapse:collapse;width:100%;margin-top:18px}th,td{border-bottom:1px solid #dce1ea;padding:10px;text-align:left}th{background:#f5f7fb}td.num,th.num{text-align:right}.totals{width:320px;margin:18px 0 0 auto}.totals div{display:flex;justify-content:space-between;padding:6px 0}.grand{font-size:18px;font-weight:bold;border-top:2px solid #172033;margin-top:6px;padding-top:12px!important}.notice{margin-top:26px;padding:14px;border:1px solid #e6b94a;background:#fff9e6;border-radius:8px}.sign{display:flex;justify-content:space-between;margin-top:70px;text-align:center}.sign div{width:40%}.toolbar{margin:20px 0}@media print{body{margin:0 auto}.toolbar{display:none}}</style></head><body><div class="toolbar"><button onclick="window.print()">In / Lưu PDF</button></div><div class="top"><div><h1>BÁO GIÁ SỬA CHỮA</h1><div class="muted">Repair Center · Báo giá tham khảo, chưa phải hóa đơn</div></div><div><strong>${esc(ticket.ticketNumber)}</strong><div>Ngày ${new Intl.DateTimeFormat('vi-VN',{dateStyle:'long'}).format(new Date())}</div></div></div><div class="meta"><div><strong>Khách hàng:</strong> ${esc(ticket.customerName)}</div><div><strong>Thiết bị:</strong> ${esc(ticket.deviceType)} ${esc(ticket.deviceBrand)} ${esc(ticket.deviceModel)}</div><div><strong>Tình trạng:</strong> ${esc(ticket.issueDescription)}</div><div><strong>Trạng thái:</strong> Chờ khách duyệt</div></div><table><thead><tr><th>Hạng mục</th><th class="num">SL</th><th class="num">Đơn giá</th><th class="num">Chiết khấu (%)</th><th class="num">Thành tiền</th></tr></thead><tbody>${details.map(x=>`<tr><td>${esc(x.description)}</td><td class="num">${x.quantity}</td><td class="num">${money(x.unitPrice)}</td><td class="num">${percent(x.discountPercent)} · ${money(x.discountAmount)}</td><td class="num">${money(x.totalAmount)}</td></tr>`).join('')}</tbody></table><div class="totals"><div><span>Tạm tính</span><strong>${money(subtotal)}</strong></div><div><span>Chiết khấu (${percent(subtotal?discount*100/subtotal:0)})</span><strong>${money(discount)}</strong></div><div class="grand"><span>Tổng báo giá</span><strong>${money(total)}</strong></div></div><div class="notice">Báo giá được lập theo các hạng mục hiện có trên phiếu. Chỉ tiến hành sửa chữa sau khi khách hàng xác nhận.</div><div class="sign"><div>Khách hàng xác nhận<br><br><br>................................</div><div>Trung tâm sửa chữa<br><br><br>................................</div></div><script>window.onload=()=>window.print()</script></body></html>`;printWindow.document.open();printWindow.document.write(html);printWindow.document.close()}catch(e){printWindow.close();toast(errText(e),true)}}
function addServiceToTicket(id){window.openWorkflowDialog?.("addService",{id})}
function assignTech(id){window.openWorkflowDialog?.("assign",{id})}
async function createQuoteVersion(id){if(!confirm('Tạo và lưu phiên bản báo giá từ các hạng mục hiện tại?'))return;try{const q=await api(`/api/repair-tickets/${id}/quotes`,{method:'POST',body:{}});toast(`Đã tạo báo giá phiên bản ${q.versionNumber}`);window.dispatchEvent(new CustomEvent('repair-tickets-changed',{detail:{path:`/api/repair-tickets/${id}/quotes`,method:'POST'}}));ticketDetail(id)}catch(e){toast(errText(e),true)}}
async function openQuoteVersion(ticketId,versionId,printAfterOpen=false){const win=window.open('','_blank');if(!win){toast('Trình duyệt đang chặn cửa sổ báo giá. Hãy cho phép cửa sổ bật lên rồi thử lại.',true);return}win.document.write('<!doctype html><html lang="vi"><meta charset="utf-8"><title>Đang tải báo giá…</title><body style="font:16px Arial;padding:32px">Đang tải báo giá…</body></html>');try{const [ticket,quotes]=await Promise.all([api(`/api/repair-tickets/${ticketId}`),api(`/api/repair-tickets/${ticketId}/quotes`)]);const quote=quotes.find(x=>Number(x.id)===Number(versionId));if(!quote)throw new Error('Không tìm thấy phiên bản báo giá.');const lines=JSON.parse(quote.snapshotJson||'[]');if(!Array.isArray(lines)||!lines.length)throw new Error('Phiên bản báo giá không có hạng mục.');const subtotal=lines.reduce((s,x)=>s+Number(x.quantity||0)*Number(x.unitPrice||0),0),discount=lines.reduce((s,x)=>s+Number(x.discountAmount||0),0),total=Number(quote.totalAmount||lines.reduce((s,x)=>s+Number(x.totalAmount||0),0));const response=quote.response==='Accepted'?'Đã ghi nhận khách đồng ý':quote.response==='Rejected'?'Đã ghi nhận khách từ chối':quote.response==='RevisionRequested'?'Khách yêu cầu điều chỉnh':'Chờ ghi nhận phản hồi';const html=`<!doctype html><html lang="vi"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Báo giá phiên bản ${quote.versionNumber} · ${esc(ticket.ticketNumber)}</title><style>*{box-sizing:border-box}body{font:14px Arial,sans-serif;color:#172033;max-width:900px;margin:32px auto;padding:0 24px}.toolbar{display:flex;gap:8px;margin-bottom:22px}.toolbar button{padding:9px 14px;border:1px solid #ccd4e2;background:white;border-radius:6px;cursor:pointer}.top{display:flex;justify-content:space-between;gap:20px;border-bottom:2px solid #425cf5;padding-bottom:16px}h1{margin:0 0 6px;font-size:24px}.muted{color:#667085}.meta{display:grid;grid-template-columns:1fr 1fr;gap:10px;margin:20px 0}.issue,.notice{background:#f5f7fb;padding:12px;border-radius:8px;margin:16px 0}table{border-collapse:collapse;width:100%;margin-top:18px}th,td{border-bottom:1px solid #dce1ea;padding:10px;text-align:left}th{background:#f5f7fb}td.num,th.num{text-align:right}.totals{width:330px;margin:18px 0 0 auto}.totals div{display:flex;justify-content:space-between;padding:6px 0}.grand{font-size:18px;font-weight:bold;border-top:2px solid #172033;margin-top:6px;padding-top:12px!important}.notice{border:1px solid #dce1ea}.sign{display:flex;justify-content:space-between;margin-top:65px;text-align:center}.sign div{width:42%}@media print{body{margin:0 auto;padding:0}.toolbar{display:none}}</style></head><body><div class="toolbar"><button onclick="window.print()">In / Lưu PDF</button><button onclick="window.close()">Đóng</button></div><div class="top"><div><h1>BÁO GIÁ SỬA CHỮA</h1><div class="muted">Repair Center · Báo giá tham khảo, không phải hóa đơn</div></div><div><strong>${esc(ticket.ticketNumber)}</strong><div>Phiên bản ${quote.versionNumber}</div><div>Ngày lập ${date(quote.createdAt)}</div></div></div><div class="meta"><div><strong>Khách hàng:</strong> ${esc(ticket.customerName)}</div><div><strong>Thiết bị:</strong> ${esc(ticket.deviceType||'')} ${esc(ticket.deviceBrand||'')} ${esc(ticket.deviceModel||'')}</div><div><strong>Tình trạng:</strong> ${esc(ticket.issueDescription||'—')}</div><div><strong>Phản hồi:</strong> ${response}${quote.respondedAt?' · '+date(quote.respondedAt):''}</div></div><table><thead><tr><th>Hạng mục</th><th class="num">Số lượng</th><th class="num">Đơn giá</th><th class="num">Chiết khấu</th><th class="num">Thành tiền</th></tr></thead><tbody>${lines.map(x=>`<tr><td>${esc(x.description||'')}</td><td class="num">${Number(x.quantity||0)}</td><td class="num">${money(x.unitPrice||0)}</td><td class="num">${percent(x.discountPercent||0)} · ${money(x.discountAmount||0)}</td><td class="num">${money(x.totalAmount||0)}</td></tr>`).join('')}</tbody></table><div class="totals"><div><span>Tạm tính</span><strong>${money(subtotal)}</strong></div><div><span>Chiết khấu</span><strong>${money(discount)}</strong></div><div class="grand"><span>Tổng báo giá</span><strong>${money(total)}</strong></div></div>${quote.responseNote?`<div class="notice"><strong>Ghi chú phản hồi:</strong> ${esc(quote.responseNote)}</div>`:''}<div class="notice">Báo giá được in từ bản chụp của phiên bản ${quote.versionNumber}, phản ánh đúng các hạng mục và đơn giá tại thời điểm lập.</div><div class="sign"><div>Khách hàng xác nhận<br><br><br>................................</div><div>Trung tâm sửa chữa<br><br><br>................................</div></div>${printAfterOpen?'<script>window.onload=()=>setTimeout(()=>window.print(),250)</script>':''}</body></html>`;win.document.open();win.document.write(html);win.document.close()}catch(e){win.close();toast(errText(e),true)}}
function recordQuoteResponse(ticketId,versionId){window.openWorkflowDialog?.("quoteResponse",{ticketId,versionId})}
function changeStatus(id,current){window.openWorkflowDialog?.("status",{id,current})}
function addPartToTicket(id){window.openWorkflowDialog?.("addPart",{id})}
function customerTickets(id){const customer=(state.data.customers||[]).find(x=>x.id===id);window.openWorkflowDialog?.("customerTickets",{id,name:customer?.name})}
function requestCatalogChange(itemType,action,targetId){if(action==="Deactivate"){if(!confirm(`Gửi yêu cầu Admin ngừng ${itemType==="RepairService"?"dịch vụ":"phụ tùng"} này?`))return;api("/api/catalog-change-requests",{method:"POST",body:{itemType,action,targetId}}).then(()=>{toast("Đã gửi yêu cầu lên Admin");window.refreshServicesReact?.();window.refreshInventoryReact?.()}).catch(e=>toast(errText(e),true));return}const item=targetId?(itemType==="RepairService"?(state.data.repairServices||[]).find(x=>x.id===targetId):(state.data.parts||[]).find(x=>x.id===targetId)):{};window.openWorkflowDialog?.("catalog",{itemType,action,targetId,item})}
async function cancelCatalogRequest(id){if(!confirm('Hủy yêu cầu đang chờ duyệt?'))return;try{await api(`/api/catalog-change-requests/${id}`,{method:'DELETE'});toast('Đã hủy yêu cầu');navigate(state.page)}catch(e){toast(errText(e),true)}}
function resolveCatalogRequest(id,approve){window.openWorkflowDialog?.("catalogResolve",{id,approve})}
function repairServiceForm(id){window.openWorkflowDialog?.("service",{id})}
async function toggleRepairService(id,isActive){const item=(state.data.repairServices||[]).find(x=>x.id===id);try{await api(`/api/repair-services/${id}`,{method:'PUT',body:{name:item.name,description:item.description,unitPrice:item.unitPrice,isActive}});toast(isActive?'Đã kích hoạt dịch vụ':'Đã ngừng dịch vụ');window.refreshServicesReact?.()}catch(e){toast(errText(e),true)}}
async function deleteRepairService(id){if(!confirm('Xóa dịch vụ này? Dịch vụ đã có trên phiếu sẽ không thể xóa.'))return;try{await api(`/api/repair-services/${id}`,{method:'DELETE'});toast('Đã xóa dịch vụ');window.refreshServicesReact?.()}catch(e){toast(errText(e),true)}}
function refreshDeviceTypesPage(){window.refreshDeviceTypesReact?.()}
function deviceTypeForm(id){window.openWorkflowDialog?.("deviceType",{id})}
async function deleteDeviceType(id){if(!confirm('Xóa loại thiết bị này? Nếu đang được sử dụng hệ thống sẽ từ chối.'))return;try{await api(`/api/device-types/${id}`,{method:'DELETE'});toast('Đã xóa loại thiết bị');refreshDeviceTypesPage()}catch(e){toast(errText(e),true)}}
function refreshCustomersPage(){window.refreshCustomersReact?.()}
function customerForm(customer={}){window.openWorkflowDialog?.("customer",{id:customer.id,item:customer})}
async function editCustomer(id){const c=(state.data.customers||[]).find(x=>x.id===id)||await api(`/api/customers/${id}`);customerForm(c)}
async function deleteCustomer(id){if(!confirm('Xóa khách hàng này?'))return;try{await api(`/api/customers/${id}`,{method:'DELETE'});toast('Đã xóa khách hàng');refreshCustomersPage()}catch(e){toast(errText(e),true)}}
function deviceForm(id){window.openWorkflowDialog?.("device",{id,item:id?(state.data.devices||[]).find(x=>x.id===id):undefined})}
async function deleteDevice(id){if(!confirm('Xóa thiết bị này?'))return;try{await api(`/api/devices/${id}`,{method:'DELETE'});toast('Đã xóa thiết bị');refreshCustomersPage()}catch(e){toast(errText(e),true)}}
function technicianTickets(id){const technician=(state.data.techs||[]).find(x=>x.id===id);window.openWorkflowDialog?.("technicianTickets",{id,name:technician?.fullName})}
function technicianForm(id){window.openWorkflowDialog?.("technician",{id})}
async function deleteTechnician(id){if(!confirm('Xóa hồ sơ kỹ thuật viên?'))return;try{await api(`/api/technicians/${id}`,{method:'DELETE'});toast('Đã xóa hồ sơ');window.refreshTechniciansReact?.()}catch(e){toast(errText(e),true)}}
function showPartTransactions(id){window.openWorkflowDialog?.("partTransactions",{id})}
function partForm(id){window.openWorkflowDialog?.("part",{id})}
function stockForm(id){window.openWorkflowDialog?.("stock",{id})}
async function exportInvoice(ticketId){
  const win=window.open('','_blank');if(!win){toast('Trình duyệt đang chặn cửa sổ hóa đơn. Hãy cho phép cửa sổ bật lên rồi thử lại.',true);return}
  try{
    const invoice=await api(`/api/invoices/by-ticket/${ticketId}`);
    const [ticket,details]=await Promise.all([api(`/api/repair-tickets/${ticketId}`),api(`/api/repair-tickets/${ticketId}/details`)]);
    const customer=await api(`/api/customers/${ticket.customerId}`),paymentLabels={Payment:'Thanh toán',Refund:'Hoàn tiền'};
    const services=details.filter(x=>x.itemType==='Service'),parts=details.filter(x=>x.itemType!=='Service');
    const warrantyItems=details.filter(x=>Number(x.warrantyDuration)>0),datedWarranty=warrantyItems.filter(x=>x.warrantyStartDate&&x.warrantyEndDate);
    const latestWarrantyEnd=datedWarranty.length?datedWarranty.map(x=>new Date(x.warrantyEndDate)).sort((a,b)=>b-a)[0]:null;
    const warrantySummary=!warrantyItems.length?'Không có hạng mục được áp dụng bảo hành':latestWarrantyEnd?`${warrantyItems.length} hạng mục có bảo hành; thời hạn tổng hợp đến ${date(latestWarrantyEnd.toISOString())}`:`${warrantyItems.length} hạng mục có chính sách bảo hành; thời gian áp dụng bắt đầu khi cấp bảo hành`;
    const itemTable=(title,items)=>!items.length?'':`<h3>${title}</h3><table><thead><tr><th>Hạng mục</th><th class="num">SL</th><th class="num">Đơn giá</th><th class="num">Chiết khấu (%)</th><th class="num">Thành tiền</th><th>Bảo hành</th></tr></thead><tbody>${items.map(x=>{const duration=Number(x.warrantyDuration)>0?warrantyDurationText(x.warrantyDuration,x.warrantyDurationUnit):'Không bảo hành';const period=x.warrantyStartDate&&x.warrantyEndDate?`<span class="secondary">${date(x.warrantyStartDate)} → ${date(x.warrantyEndDate)}</span>`:'';return `<tr><td>${esc(x.description)}</td><td class="num">${x.quantity}</td><td class="num">${money(x.unitPrice)}</td><td class="num">${percent(x.discountPercent)} · ${money(x.discountAmount)}</td><td class="num">${money(x.totalAmount)}</td><td>${esc(duration)}${period}</td></tr>`}).join('')}</tbody></table>`;
    const html=`<!doctype html><html lang="vi"><head><meta charset="utf-8"><title>Hóa đơn ${esc(invoice.invoiceNumber)}</title><style>body{font:14px Arial,sans-serif;color:#172033;max-width:900px;margin:32px auto;padding:0 24px}.toolbar{margin:16px 0 24px}h1{margin:0 0 6px;font-size:24px}h3{margin:24px 0 0}.muted,.secondary{color:#667085}.secondary{display:block;font-size:12px;margin-top:4px}.top{display:flex;justify-content:space-between;border-bottom:2px solid #425cf5;padding-bottom:16px}.meta{display:grid;grid-template-columns:1fr 1fr;gap:8px;margin:20px 0}.box{background:#f5f7fb;padding:12px;border-radius:8px;margin:16px 0}table{border-collapse:collapse;width:100%;margin-top:12px}th,td{border-bottom:1px solid #dce1ea;padding:10px;text-align:left;vertical-align:top}th{background:#f5f7fb}td.num,th.num{text-align:right}.totals{width:350px;margin:18px 0 0 auto}.totals div{display:flex;justify-content:space-between;padding:6px 0}.grand{font-size:18px;font-weight:bold;border-top:2px solid #172033;margin-top:6px;padding-top:12px!important}.sign{display:flex;justify-content:space-between;margin-top:70px;text-align:center}.sign div{width:40%}@media print{body{margin:0 auto}.toolbar{display:none}}</style></head><body><div class="toolbar"><button onclick="window.print()">In / Lưu PDF</button></div><div class="top"><div><h1>HÓA ĐƠN SỬA CHỮA</h1><div class="muted">Repair Center</div></div><div><strong>${esc(invoice.invoiceNumber)}</strong><div>Phiếu ${esc(invoice.ticketNumber)}</div></div></div><div class="meta"><div><strong>Khách hàng:</strong> ${esc(customer.name)}</div><div><strong>Điện thoại:</strong> ${esc(customer.phoneNumber||'—')}</div><div><strong>Email:</strong> ${esc(customer.email||'—')}</div><div><strong>Địa chỉ:</strong> ${esc(customer.address||'—')}</div><div><strong>Thiết bị:</strong> ${esc(ticket.deviceType)} ${esc(ticket.deviceBrand)} ${esc(ticket.deviceModel)}</div><div><strong>Serial:</strong> ${esc(ticket.serialNumber||'—')}</div><div><strong>Ngày phát hành:</strong> ${date(invoice.issuedAt)}</div><div><strong>Hạn thanh toán:</strong> ${date(invoice.dueAt)}</div></div><div class="box"><strong>Bảo hành tổng hợp:</strong> ${esc(warrantySummary)}</div>${itemTable('Dịch vụ sửa chữa',services)}${itemTable('Phụ tùng thay thế',parts)}<div class="totals"><div><span>Tạm tính</span><strong>${money(invoice.subtotal)}</strong></div><div><span>Chiết khấu tổng (${percent(invoice.subtotal?invoice.discountAmount*100/invoice.subtotal:0)})</span><strong>${money(invoice.discountAmount)}</strong></div><div><span>Thuế (${percent(invoice.taxPercent)})</span><strong>${money(invoice.taxAmount)}</strong></div><div class="grand"><span>Tổng cộng</span><strong>${money(invoice.totalAmount)}</strong></div><div><span>Đã thanh toán</span><strong>${money(invoice.amountPaid)}</strong></div><div><span>Còn phải thu</span><strong>${money(Math.max(0,invoice.totalAmount-invoice.amountPaid))}</strong></div></div><h3>Lịch sử thanh toán</h3><table><thead><tr><th>Thời gian</th><th>Loại</th><th>Phương thức</th><th>Người xử lý</th><th class="num">Số tiền</th></tr></thead><tbody>${invoice.payments.map(p=>`<tr><td>${date(p.paidAt)}</td><td>${paymentLabels[p.type]||esc(p.type)}</td><td>${esc(p.paymentMethod)}</td><td>${esc(p.processedByUserName||'—')}</td><td class="num">${money(p.amount)}</td></tr>`).join('')||'<tr><td colspan="5">Chưa có giao dịch</td></tr>'}</tbody></table>${invoice.notes?`<div class="box"><strong>Ghi chú:</strong> ${esc(invoice.notes)}</div>`:''}<div class="sign"><div>Khách hàng<br><br><br>................................</div><div>Đại diện Repair Center<br><br><br>................................</div></div><script>window.onload=()=>window.print()</script></body></html>`;
    win.document.open();win.document.write(html);win.document.close()
  }catch(e){win.close();toast(errText(e),true)}
}
function accountForm(){window.openWorkflowDialog?.("account")}
function editRoles(id){window.openWorkflowDialog?.("roles",{id})}
async function toggleAccount(id,isActive){try{await api(`/api/accounts/${id}/active`,{method:'PATCH',body:{isActive}});toast('Đã cập nhật trạng thái tài khoản');window.refreshAccountsReact?.()}catch(e){toast(errText(e),true)}}
async function revokeAccount(id){if(!confirm('Thu hồi tất cả token của tài khoản này?'))return;try{await api(`/api/accounts/${id}/revoke-tokens`,{method:'POST'});toast('Đã thu hồi token')}catch(e){toast(errText(e),true)}}
function resetAccountPassword(id){const user=(state.data.users||[]).find(x=>x.id===id);if(!user||!user.isActive)return toast("Chỉ có thể đặt lại mật khẩu cho tài khoản đang hoạt động.",true);if(!confirm(`Tạo mật khẩu tạm mới cho ${user.userName}? Các phiên đăng nhập hiện tại sẽ bị thu hồi.`))return;window.openWorkflowDialog?.("resetPassword",{id})}
window.repairCenterBridge = {
  get token() { return state.token; },
  get user() { return state.user; },
  get roles() { return state.roles; },
  get data() { return state.data; },
  api,
  can,
  toast,
  date,
  money,
  percent,
  enumKey,
  statusLabels,
  badgeClass,
  openIntake,
  ticketDetail,
  printTicket,
  exportQuote,
  openQuoteVersion,
  exportInvoice,
  assignTech,
  changeStatus,
  createQuoteVersion,
  addServiceToTicket,
  addPartToTicket,
  editTicketDetail,
  deleteTicketDetail,
  recordQuoteResponse,
  customerForm,
  editCustomer,
  deleteCustomer,
  deviceForm,
  deleteDevice,
  deviceTypeForm,
  deleteDeviceType,
  technicianForm,
  deleteTechnician,
  technicianTickets,
  repairServiceForm,
  toggleRepairService,
  deleteRepairService,
  partForm,
  stockForm,
  showPartTransactions,
  customerTickets,
  requestCatalogChange,
  cancelCatalogRequest,
  resolveCatalogRequest,
  accountForm,
  editRoles,
  toggleAccount,
  revokeAccount,
  resetAccountPassword,
  completeLogin(authResult) {
    if (!authResult) return;
    const token = authResult.accessToken || authResult.token;
    const user = {
      userId: authResult.userId ?? authResult.user?.userId ?? authResult.user?.id,
      userName: authResult.userName || authResult.user?.userName,
      fullName: authResult.fullName || authResult.user?.fullName || authResult.userName,
      roles: authResult.roles || authResult.user?.roles || [],
      mustChangePassword: !!(authResult.mustChangePassword ?? authResult.user?.mustChangePassword)
    };
    state.token = token;
    state.user = user;
    state.roles = tokenRoles(token, user.roles);
    if (token) sessionStorage.setItem('rc-token', token);
    else sessionStorage.removeItem('rc-token');
    if (user) sessionStorage.setItem('rc-user', JSON.stringify(user));
    else sessionStorage.removeItem('rc-user');
    showApp();
  },
  setAccountData(users, roles) {
    state.data.users = users;
    state.data.roles = roles;
  }
};

window.navigate = navigate;
window.logout = logout;
window.api = api;
window.can = can;
window.toast = toast;
window.openIntake = openIntake;
window.ticketDetail = ticketDetail;
window.customerForm = customerForm;
window.editCustomer = editCustomer;
window.deleteCustomer = deleteCustomer;
window.deviceForm = deviceForm;
window.deleteDevice = deleteDevice;
window.deviceTypeForm = deviceTypeForm;
window.deleteDeviceType = deleteDeviceType;
window.technicianForm = technicianForm;
window.deleteTechnician = deleteTechnician;
window.technicianTickets = technicianTickets;
window.repairServiceForm = repairServiceForm;
window.toggleRepairService = toggleRepairService;
window.deleteRepairService = deleteRepairService;
window.partForm = partForm;
window.stockForm = stockForm;
window.showPartTransactions = showPartTransactions;
window.customerTickets = customerTickets;
window.requestCatalogChange = requestCatalogChange;
window.cancelCatalogRequest = cancelCatalogRequest;
window.resolveCatalogRequest = resolveCatalogRequest;
window.editTicketDetail = editTicketDetail;
window.deleteTicketDetail = deleteTicketDetail;
window.exportQuote = exportQuote;
window.exportInvoice = exportInvoice;
window.printTicket = printTicket;
window.addServiceToTicket = addServiceToTicket;
window.addPartToTicket = addPartToTicket;
window.assignTech = assignTech;
window.createQuoteVersion = createQuoteVersion;
window.openQuoteVersion = openQuoteVersion;
window.recordQuoteResponse = recordQuoteResponse;
window.changeStatus = changeStatus;
window.accountForm = accountForm;
window.editRoles = editRoles;
window.toggleAccount = toggleAccount;
window.revokeAccount = revokeAccount;
window.resetAccountPassword = resetAccountPassword;

init();
