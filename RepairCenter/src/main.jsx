import React from 'react';
import { createRoot } from 'react-dom/client';
import { flushSync } from 'react-dom';
import TicketsPage from './TicketsPage.jsx';
import OverviewPage from './OverviewPage.jsx';
import CustomersPage from './CustomersPage.jsx';
import DeviceTypesPage from './DeviceTypesPage.jsx';
import AccountsPage from './AccountsPage.jsx';
import TechniciansPage from './TechniciansPage.jsx';
import InventoryPage from './InventoryPage.jsx';
import ServicesPage from './ServicesPage.jsx';
import BillingPage from './BillingPage.jsx';
import WarrantiesPage from './WarrantiesPage.jsx';
import WorkflowDialogs from './WorkflowDialogs.jsx';

const h = React.createElement;
const pageMeta = {
  overview:['TỔNG QUAN','Tổng quan','◫'], tickets:['NGHIỆP VỤ','Phiếu sửa chữa','▤'], customers:['DANH MỤC','Khách hàng & thiết bị','♙'],
  deviceTypes:['DANH MỤC','Loại thiết bị','▱'], services:['DANH MỤC','Dịch vụ sửa chữa','🔧'], technicians:['NHÂN SỰ','Kỹ thuật viên','⚙'],
  inventory:['KHO VẬT TƯ','Kho phụ tùng','▦'], billing:['THU NGÂN','Hóa đơn & thanh toán','＄'], warranties:['HẬU MÃI','Bảo hành','◇'], accounts:['QUẢN TRỊ','Tài khoản','♧']
};

function Navigation(){
  const [page,setPage]=React.useState('overview');
  const [authVersion,setAuthVersion]=React.useState(0);
  const [notifications,setNotifications]=React.useState({counts:{},errors:{}});
  React.useEffect(()=>{
    let cancelled=false,requestId=0;
    const refreshNotifications=async()=>{
      const currentRequest=++requestId,bridge=window.repairCenterBridge;
      if(!bridge?.token){setNotifications({counts:{},errors:{}});return}
      const counts={},errors={},tasks=[];
      const load=async(keys,action)=>{try{await action()}catch{keys.forEach(key=>{errors[key]=true})}};
      if(bridge.can('Admin','Receptionist'))
        tasks.push(load(['tickets'],async()=>{const result=await bridge.api('/api/repair-tickets?page=1&pageSize=1&status=3');counts.tickets=result.totalCount}));
      if(bridge.can('Admin','Receptionist','Technician'))
        tasks.push(load(['services','inventory'],async()=>{
          const requests=await bridge.api('/api/catalog-change-requests');
          const pending=requests.filter(request=>request.status==='Pending');
          counts.services=pending.filter(request=>request.itemType==='RepairService').length;
          counts.inventory=pending.filter(request=>request.itemType==='SparePart').length;
        }));
      if(bridge.can('Admin','Receptionist','Technician'))
        tasks.push(load(['warranties'],async()=>{counts.warranties=(await bridge.api('/api/warranties/claims/pending')).length}));
      await Promise.all(tasks);
      if(!cancelled&&currentRequest===requestId)setNotifications({counts,errors});
    };
    const onRoute=event=>{setPage(event.detail?.page||'overview');refreshNotifications()};
    const eventNames=['repair-center-notifications-changed','repair-tickets-changed'];
    const onChange=()=>refreshNotifications(),onAuth=()=>setAuthVersion(v=>v+1);
    refreshNotifications();
    const timer=setInterval(refreshNotifications,60000);
    window.addEventListener('repair-center-navigate',onRoute);
    window.addEventListener('repair-center-auth',onAuth);
    window.addEventListener('repair-center-auth-state',onAuth);
    eventNames.forEach(name=>window.addEventListener(name,onChange));
    return()=>{
      cancelled=true;clearInterval(timer);
      window.removeEventListener('repair-center-navigate',onRoute);
      window.removeEventListener('repair-center-auth',onAuth);
      window.removeEventListener('repair-center-auth-state',onAuth);
      eventNames.forEach(name=>window.removeEventListener(name,onChange));
    };
  },[authVersion]);
  const items=[['overview',true],['tickets',window.repairCenterBridge?.can('Admin','Receptionist','Technician')],['customers',window.repairCenterBridge?.can('Admin','Receptionist')],['deviceTypes',window.repairCenterBridge?.can('Admin','Receptionist')],['services',window.repairCenterBridge?.can('Admin','Receptionist','Technician')],['technicians',window.repairCenterBridge?.can('Admin','Receptionist')],['inventory',window.repairCenterBridge?.can('Admin','Receptionist','Technician')],['billing',window.repairCenterBridge?.can('Admin','Receptionist')],['warranties',window.repairCenterBridge?.can('Admin','Receptionist','Technician')],['accounts',window.repairCenterBridge?.can('Admin')]];
  const notificationLabels={tickets:'phiếu chờ duyệt',services:'yêu cầu dịch vụ chờ duyệt',inventory:'yêu cầu phụ tùng chờ duyệt',warranties:'yêu cầu bảo hành đang xử lý'};
  const notificationFor=key=>{
    const count=notifications.counts[key],failed=notifications.errors[key];
    if(failed)return h('span',{className:'nav-count failed',title:`Không tải được số lượng ${notificationLabels[key]||'yêu cầu'}`,'aria-label':'Không tải được số lượng yêu cầu'},'!');
    if(!count)return null;
    const label=`${count} ${notificationLabels[key]||'yêu cầu đang chờ'}`;
    return h('span',{className:'nav-count',title:label,'aria-label':label},count>99?'99+':count);
  };
  return h('nav',null,items.filter(([,allowed])=>allowed).map(([key])=>h('button',{key,className:`nav-item ${page===key?'active':''}`,onClick:()=>window.navigate(key)},h('span',{className:'nav-icon'},pageMeta[key][2]),pageMeta[key][1],notificationFor(key))));
}
function PageHeading(){
  const [page,setPage]=React.useState('overview');
  React.useEffect(()=>{const onRoute=e=>setPage(e.detail?.page||'overview');window.addEventListener('repair-center-navigate',onRoute);return()=>window.removeEventListener('repair-center-navigate',onRoute)},[]);
  const [crumb,title]=pageMeta[page]||pageMeta.overview;
  return h('div',null,h('p',{className:'eyebrow',id:'crumb'},crumb),h('h1',{id:'page-title'},title));
}
function UserProfile(){
  const [user,setUser]=React.useState(window.repairCenterBridge?.user||null),[roles,setRoles]=React.useState(window.repairCenterBridge?.roles||window.repairCenterBridge?.user?.roles||[]);
  React.useEffect(()=>{const update=()=>{setUser(window.repairCenterBridge?.user||null);setRoles(window.repairCenterBridge?.roles||window.repairCenterBridge?.user?.roles||[])};window.addEventListener('repair-center-auth',update);return()=>window.removeEventListener('repair-center-auth',update)},[]);
  const name=user?.fullName||'Người dùng';
  return h('div',{className:'user-chip'},h('div',{className:'avatar'},name.trim().charAt(0).toUpperCase()||'U'),h('div',{className:'user-meta'},h('strong',null,name),h('small',null,roles.join(' · '))),h('button',{className:'icon-button',title:'Đăng xuất',onClick:()=>window.logout?.(true)},'↗'));
}
function ToastHost(){
  const [toast,setToast]=React.useState(null);
  React.useEffect(()=>{let timer;const onToast=e=>{setToast(e.detail||null);clearTimeout(timer);timer=setTimeout(()=>setToast(null),e.detail?.bad?8000:3200)};window.addEventListener('repair-center-toast',onToast);return()=>{clearTimeout(timer);window.removeEventListener('repair-center-toast',onToast)}},[]);
  return h('div',{className:`toast ${toast?'show':''}`,style:toast?.bad?{background:'#b93845'}:undefined},toast?.message||'');
}

function Login({ mode, onModeChange }) {
  const [error,setError]=React.useState(''),[notice,setNotice]=React.useState('');
  const submitLogin=async e=>{e.preventDefault();setError('');setNotice('');const f=new FormData(e.currentTarget);try{const r=await window.repairCenterBridge.api('/api/auth/login',{method:'POST',body:{userName:f.get('userName'),password:f.get('password')}});window.repairCenterBridge.completeLogin(r)}catch(err){setError(err.message||'Không thể đăng nhập.')}};
  const submitPassword=async e=>{e.preventDefault();setError('');const f=new FormData(e.currentTarget),currentPassword=f.get('currentPassword'),newPassword=f.get('newPassword');if(newPassword!==f.get('confirmPassword')){setError('Mật khẩu xác nhận không khớp.');return}try{await window.repairCenterBridge.api('/api/auth/change-temporary-password',{method:'POST',body:{currentPassword,newPassword}});window.logout(false);onModeChange('login');setNotice('Đổi mật khẩu thành công. Vui lòng đăng nhập lại bằng mật khẩu mới.')}catch(err){setError(err.message||'Không thể đổi mật khẩu.')}};
  return h('div',{className:`login-shell ${mode==='login'||mode==='change'?'':'hidden'}`},h('div',{className:'login-card'},
    h('div',{className:'brand-mark'},'R'),h('p',{className:'eyebrow'},'REPAIR CENTER'),
    h('h1',null,mode==='change'?'Đổi mật khẩu tạm':'Đăng nhập'),
    h('p',{className:'muted'},'Quản lý tiếp nhận và vận hành trung tâm sửa chữa.'),
    mode==='change'?h('form',{onSubmit:submitPassword},h('p',{className:'field-note'},'Đây là mật khẩu tạm. Hãy tạo mật khẩu mới để tiếp tục.'),
      h('label',null,'Mật khẩu tạm',h('input',{name:'currentPassword',type:'password',autoComplete:'current-password',required:true})),
      h('label',null,'Mật khẩu mới (ít nhất 12 ký tự)',h('input',{name:'newPassword',type:'password',autoComplete:'new-password',minLength:12,required:true})),
      h('label',null,'Nhập lại mật khẩu mới',h('input',{name:'confirmPassword',type:'password',autoComplete:'new-password',minLength:12,required:true})),
      h('button',{className:'button primary wide'},'Đổi mật khẩu và tiếp tục')):
      h('form',{onSubmit:submitLogin},h('label',null,'Tên đăng nhập',h('input',{name:'userName',autoComplete:'username',required:true})),
      h('label',null,'Mật khẩu',h('input',{name:'password',type:'password',autoComplete:'current-password',required:true})),
      h('button',{className:'button primary wide'},'Đăng nhập')),
    h('p',{className:'error'},error||notice)));
}

function AppShell() {
  const [auth,setAuth]=React.useState(()=>({
    authenticated:!!window.repairCenterBridge?.token && !!window.repairCenterBridge?.user && !window.repairCenterBridge?.user?.mustChangePassword,
    mode:window.repairCenterBridge?.user?.mustChangePassword?'change':'login'
  }));
  React.useEffect(()=>{
    const sync=e=>setAuth(e.detail||{authenticated:!!window.repairCenterBridge?.token && !!window.repairCenterBridge?.user,mode:'login'});
    window.addEventListener('repair-center-auth-state',sync);
    return()=>window.removeEventListener('repair-center-auth-state',sync)
  },[]);
  const setMode=mode=>setAuth(v=>({...v,mode,authenticated:false}));
  return h(React.Fragment, null,
    h(Login,{mode:auth.authenticated?'app':auth.mode,onModeChange:setMode}),
    h('div', { id: 'app', className: `app-shell ${auth.authenticated?'':'hidden'}` },
      h('aside', { className: 'sidebar' },
        h('div', { className: 'brand' }, h('div', { className: 'brand-mark' }, 'R'),
          h('div', null, h('strong', null, 'Repair Center'), h('small', null, 'Điều hành dịch vụ'))),
        h('div', { className: 'nav-label' }, 'TỔNG QUAN'),
          h(Navigation),
        h('div', { className: 'sidebar-bottom' }, h(UserProfile))),
      h('main', { className: 'main' },
        h('header', { className: 'topbar' },
          h(PageHeading),
          h('div', { className: 'top-actions' }, h('span', { className: 'today' }, new Intl.DateTimeFormat('vi-VN',{weekday:'long',day:'numeric',month:'long',timeZone:'Asia/Ho_Chi_Minh'}).format(new Date())),
            h('button', { className: `button primary ${window.repairCenterBridge?.can('Admin','Receptionist')?'':'hidden'}`, onClick:()=>window.repairCenterBridge?.openIntake() }, '＋ Tiếp nhận phiếu'))),
        h('div', { id: 'notice', className: 'notice hidden' }),
        h('section', { id: 'content' }))),
    h('div', { id: 'modal-root' }, h(WorkflowDialogs)),
    h(ToastHost));
}

const root = createRoot(document.getElementById('react-root'));
flushSync(() => root.render(h(AppShell)));

let businessRoot;
function renderBusinessPage(component) {
  const container = document.getElementById('content');
  if (businessRoot) {
    flushSync(() => businessRoot.unmount());
    businessRoot = null;
  }
  container.replaceChildren();
  businessRoot = createRoot(container);
  flushSync(() => businessRoot.render(h(component)));
}
window.mountTicketsReact = () => renderBusinessPage(TicketsPage);
window.mountOverviewReact = () => renderBusinessPage(OverviewPage);
window.mountCustomersReact = () => renderBusinessPage(CustomersPage);
window.mountDeviceTypesReact = () => renderBusinessPage(DeviceTypesPage);
window.mountAccountsReact = () => renderBusinessPage(AccountsPage);
window.mountTechniciansReact = () => renderBusinessPage(TechniciansPage);
window.mountInventoryReact = () => renderBusinessPage(InventoryPage);
window.mountServicesReact = () => renderBusinessPage(ServicesPage);
window.mountBillingReact = () => renderBusinessPage(BillingPage);
window.mountWarrantiesReact = () => renderBusinessPage(WarrantiesPage);
// app.js can request the initial route before this module finishes loading.
// Mount the default page once its React entry point is available.
if (window.repairCenterBridge?.token && !window.repairCenterBridge?.user?.mustChangePassword) {
  window.mountOverviewReact();
}
window.unmountBusinessReact = () => {
  if (!businessRoot) return;
  flushSync(() => businessRoot.unmount());
  businessRoot = null;
};
