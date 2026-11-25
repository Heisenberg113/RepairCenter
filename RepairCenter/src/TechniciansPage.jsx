import React, { useEffect, useMemo, useState } from 'react';
const b = () => window.repairCenterBridge;
export default function TechniciansPage() {
  const [rows, setRows] = useState([]), [deviceTypes,setDeviceTypes]=useState([]), [query, setQuery] = useState(''), [deviceTypeFilter,setDeviceTypeFilter]=useState('all'), [statusFilter,setStatusFilter]=useState('all'), [loading, setLoading] = useState(true), [error, setError] = useState('');
  const refresh = async () => { setLoading(true); setError(''); try { const [techs, types] = await Promise.all([b().api('/api/technicians'), b().api('/api/device-types')]); setRows(techs); setDeviceTypes(types); b().setTechnicianData?.(techs, types); } catch (e) { setError(e.message || 'Không tải được kỹ thuật viên.'); } finally { setLoading(false); } };
  useEffect(() => { window.refreshTechniciansReact = refresh; refresh(); return () => { delete window.refreshTechniciansReact; }; }, []);
  const filtered = useMemo(() => rows.filter(t => {
    const matchesQuery=!query.trim()||[t.fullName,t.employeeCode,t.specialty].join(' ').toLowerCase().includes(query.trim().toLowerCase());
    const matchesType=deviceTypeFilter==='all'||t.deviceTypes.some(d=>String(d.id)===deviceTypeFilter);
    const matchesStatus=statusFilter==='all'||(statusFilter==='active'?t.isActive:!t.isActive);
    return matchesQuery&&matchesType&&matchesStatus;
  }), [rows, query, deviceTypeFilter, statusFilter]);
  const hasFilters=query.trim()||deviceTypeFilter!=='all'||statusFilter!=='all';
  const clearFilters=()=>{setQuery('');setDeviceTypeFilter('all');setStatusFilter('all')};
  return <>
    {error && <div className="panel"><strong>Không tải được dữ liệu</strong><p className="muted">{error}</p><button className="button" onClick={refresh}>Thử lại</button></div>}
    {loading ? <div className="empty-state">Đang tải kỹ thuật viên…</div> : !error && <>
      <div className="toolbar"><input className="search" aria-label="Tìm kỹ thuật viên" placeholder="Tìm kỹ thuật viên…" value={query} onChange={e => setQuery(e.target.value)} />
        <select className="filter-select" aria-label="Lọc theo loại thiết bị" value={deviceTypeFilter} onChange={e=>setDeviceTypeFilter(e.target.value)}><option value="all">Tất cả loại thiết bị</option>{deviceTypes.map(t=><option key={t.id} value={t.id}>{t.name}</option>)}</select>
        <select className="filter-select" aria-label="Lọc theo trạng thái kỹ thuật viên" value={statusFilter} onChange={e=>setStatusFilter(e.target.value)}><option value="all">Tất cả trạng thái</option><option value="active">Đang hoạt động</option><option value="inactive">Ngừng hoạt động</option></select>
        {hasFilters&&<button className="button" onClick={clearFilters}>Xóa bộ lọc</button>}
        <button className="button primary" onClick={() => window.technicianForm()}>＋ Thêm kỹ thuật viên</button></div>
      <div className="table-card"><div className="table-head"><h2>Kỹ thuật viên · {filtered.length}/{rows.length}</h2><div className="table-tools"><button className="button small" onClick={refresh}>Làm mới</button></div></div><div className="table-scroll"><table>
        <thead><tr><th>Mã nhân viên</th><th>Họ tên</th><th>Liên hệ</th><th>Chuyên môn</th><th>Trạng thái</th><th /></tr></thead><tbody>{filtered.length ? filtered.map(t => <tr key={t.id}><td className="primary-cell">{t.employeeCode}</td><td><button className="icon-action ticket-number" onClick={() => window.technicianTickets(t.id)}>{t.fullName}</button><span className="secondary">Tài khoản #{t.userId ?? 'chưa liên kết'}</span></td><td>{t.phoneNumber || '—'}<span className="secondary">{t.email || ''}</span></td><td>{t.deviceTypes.map(d => <span key={d.id} className="badge blue">{d.name}</span>)}</td><td><span className={`badge ${t.isActive ? 'green' : 'amber'}`}>{t.isActive ? 'Đang hoạt động' : 'Ngừng hoạt động'}</span></td><td><button className="icon-action" onClick={() => window.technicianForm(t.id)}>Sửa</button><button className="icon-action" onClick={() => window.deleteTechnician(t.id)}>Xóa</button></td></tr>) : <tr><td colSpan="6" className="empty">{rows.length?'Không tìm thấy kỹ thuật viên phù hợp. Hãy thử đổi hoặc xóa bộ lọc.':'Chưa có kỹ thuật viên.'}</td></tr>}</tbody>
      </table></div></div>
    </>}
  </>;
}
