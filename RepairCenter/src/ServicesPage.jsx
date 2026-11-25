import React, { useEffect, useMemo, useState } from 'react';
import TablePagination from './TablePagination.jsx';
const b = () => window.repairCenterBridge;
const PAGE_SIZE = 10;
export default function ServicesPage() {
  const [rows,setRows]=useState([]),[requests,setRequests]=useState([]),[query,setQuery]=useState(''),[deviceTypeFilter,setDeviceTypeFilter]=useState('all'),[statusFilter,setStatusFilter]=useState('all'),[requestPage,setRequestPage]=useState(1),[servicePage,setServicePage]=useState(1),[loading,setLoading]=useState(true),[error,setError]=useState('');
  const admin=b().can('Admin'), tech=b().can('Technician');
  const refresh=async()=>{setLoading(true);setError('');try{const data=await b().api(`/api/repair-services?includeInactive=${admin}`);setRows(data);b().setServicesData?.(data);if(admin||tech)setRequests(await b().api('/api/catalog-change-requests'));}catch(e){setError(e.message||'Không tải được dịch vụ.');}finally{setLoading(false)}};
  useEffect(()=>{window.refreshServicesReact=refresh;refresh();return()=>{delete window.refreshServicesReact}},[]);
  const deviceTypes=useMemo(()=>Array.from(new Map(rows.filter(x=>x.deviceTypeId!=null).map(x=>[x.deviceTypeId,x.deviceTypeName||'Loại thiết bị'])).entries()).sort((a,c)=>a[1].localeCompare(c[1],'vi')),[rows]);
  const visible=useMemo(()=>rows.filter(x=>{
    const matchesQuery=!query.trim()||[x.name,x.deviceTypeName,x.description].join(' ').toLowerCase().includes(query.trim().toLowerCase());
    const matchesType=deviceTypeFilter==='all'||(deviceTypeFilter==='unclassified'?x.deviceTypeId==null:String(x.deviceTypeId)===deviceTypeFilter);
    const matchesStatus=statusFilter==='all'||(statusFilter==='active'?x.isActive:!x.isActive);
    return matchesQuery&&matchesType&&matchesStatus;
  }),[rows,query,deviceTypeFilter,statusFilter]);
  const pending=requests.filter(r=>r.status==='Pending');
  const requestPageCount=Math.max(1,Math.ceil(pending.length/PAGE_SIZE));
  const currentRequestPage=Math.min(requestPage,requestPageCount);
  const requestPageRows=pending.slice((currentRequestPage-1)*PAGE_SIZE,currentRequestPage*PAGE_SIZE);
  const servicePageCount=Math.max(1,Math.ceil(visible.length/PAGE_SIZE));
  const currentServicePage=Math.min(servicePage,servicePageCount);
  const servicePageRows=visible.slice((currentServicePage-1)*PAGE_SIZE,currentServicePage*PAGE_SIZE);
  const esc=s=>String(s??'');
  const hasFilters=query.trim()||deviceTypeFilter!=='all'||statusFilter!=='all';
  const clearFilters=()=>{setQuery('');setDeviceTypeFilter('all');setStatusFilter('all');setServicePage(1)};
  return <>
    {error&&<div className="panel"><strong>Không tải được dữ liệu</strong><p className="muted">{error}</p><button className="button" onClick={refresh}>Thử lại</button></div>}
    {!loading&&!error&&<>
      {(admin||tech)&&<div className="panel"><div className="panel-head"><div><h2>{admin?'Yêu cầu danh mục đang chờ duyệt':'Yêu cầu thay đổi danh mục của bạn'}</h2><p>{admin?`${pending.length} yêu cầu của kỹ thuật viên`:'Chỉ hiển thị yêu cầu đang chờ xử lý; yêu cầu đã duyệt hoặc từ chối không còn trong danh sách này.'}</p></div><button className="button small" onClick={refresh}>Làm mới</button></div><div className="table-scroll"><table><thead><tr><th>Loại</th><th>Thao tác</th><th>Mục tiêu / nội dung</th><th>Người gửi</th><th>Ngày gửi</th><th /></tr></thead><tbody>{requestPageRows.length?requestPageRows.map(r=><tr key={r.id}><td>{r.itemType==='RepairService'?'Dịch vụ':'Phụ tùng'}</td><td>{r.action==='Create'?'Thêm':r.action==='Update'?'Sửa':'Ngừng'}</td><td>{r.targetId?`#${r.targetId}`:(r.payload?.name||r.payload?.partNumber||'Mục mới')}<span className="secondary">{r.payload?.name||r.payload?.partNumber||''}</span></td><td>{r.requestedBy||'—'}</td><td>{b().date(r.requestedAt)}</td><td>{admin&&<><button className="icon-action" onClick={()=>window.resolveCatalogRequest(r.id,true)}>Duyệt</button><button className="icon-action" onClick={()=>window.resolveCatalogRequest(r.id,false)}>Từ chối</button></>}{!admin&&<button className="icon-action" onClick={()=>window.cancelCatalogRequest(r.id)}>Hủy yêu cầu</button>}</td></tr>):<tr><td colSpan="6" className="empty">Không có yêu cầu đang chờ xử lý.</td></tr>}</tbody></table></div><TablePagination page={currentRequestPage} pageSize={PAGE_SIZE} totalCount={pending.length} onPageChange={setRequestPage}/></div>}
      <div className="toolbar">
        <input className="search" aria-label="Tìm dịch vụ sửa chữa" placeholder="Tìm dịch vụ…" value={query} onChange={e=>{setQuery(e.target.value);setServicePage(1)}}/>
        <select className="filter-select" aria-label="Lọc theo loại thiết bị" value={deviceTypeFilter} onChange={e=>{setDeviceTypeFilter(e.target.value);setServicePage(1)}}>
          <option value="all">Tất cả loại thiết bị</option>
          {deviceTypes.map(([id,name])=><option key={id} value={id}>{name}</option>)}
          {rows.some(x=>x.deviceTypeId==null)&&<option value="unclassified">Chưa phân loại</option>}
        </select>
        {admin&&<select className="filter-select" aria-label="Lọc theo trạng thái dịch vụ" value={statusFilter} onChange={e=>{setStatusFilter(e.target.value);setServicePage(1)}}>
          <option value="all">Tất cả trạng thái</option><option value="active">Đang áp dụng</option><option value="inactive">Ngừng áp dụng</option>
        </select>}
        {hasFilters&&<button className="button" onClick={clearFilters}>Xóa bộ lọc</button>}
        {admin?<button className="button primary" onClick={()=>window.repairServiceForm()}>＋ Thêm dịch vụ</button>:tech&&<button className="button primary" onClick={()=>window.requestCatalogChange('RepairService','Create')}>＋ Yêu cầu thêm dịch vụ</button>}
      </div>
      <div className="table-card"><div className="table-head"><h2>Dịch vụ sửa chữa · {visible.length}/{rows.length}</h2><div className="table-tools"><button className="button small" onClick={refresh}>Làm mới</button></div></div><div className="table-scroll"><table><thead><tr><th>Tên dịch vụ</th><th>Loại thiết bị</th><th>Mô tả</th><th>Đơn giá</th><th>Bảo hành mặc định</th><th>Trạng thái</th><th/></tr></thead><tbody>{visible.length?servicePageRows.map(x=><tr key={x.id}><td className="primary-cell">{esc(x.name)}<span className="secondary">#{x.id}</span></td><td>{x.deviceTypeName||'Chưa phân loại'}</td><td>{x.description||'—'}</td><td>{b().money(x.unitPrice)}</td><td>{x.warrantyDuration?`${x.warrantyDuration} ${Number(x.warrantyDurationUnit)===1?'ngày':'tháng'}`:'Không bảo hành'}<span className="secondary">{x.warrantyTerms||''}</span></td><td><span className={`badge ${x.isActive?'green':'amber'}`}>{x.isActive?'Đang áp dụng':'Ngừng áp dụng'}</span></td><td>{admin?<><button className="icon-action" onClick={()=>window.repairServiceForm(x.id)}>Sửa</button><button className="icon-action" onClick={()=>window.toggleRepairService(x.id,!x.isActive)}>{x.isActive?'Ngừng':'Kích hoạt'}</button><button className="icon-action" onClick={()=>window.deleteRepairService(x.id)}>Xóa</button></>:tech?<><button className="icon-action" onClick={()=>window.requestCatalogChange('RepairService','Update',x.id)}>Yêu cầu sửa</button>{x.isActive&&<button className="icon-action" onClick={()=>window.requestCatalogChange('RepairService','Deactivate',x.id)}>Yêu cầu ngừng</button>}</>:'Chỉ xem'}</td></tr>):<tr><td colSpan="7" className="empty">{rows.length?'Không tìm thấy dịch vụ phù hợp. Hãy thử đổi hoặc xóa bộ lọc.':'Chưa có dịch vụ sửa chữa trong danh mục.'}</td></tr>}</tbody></table></div><TablePagination page={currentServicePage} pageSize={PAGE_SIZE} totalCount={visible.length} onPageChange={setServicePage}/></div>
    </>}
    {loading&&<div className="empty-state">Đang tải dịch vụ…</div>}
  </>;
}
