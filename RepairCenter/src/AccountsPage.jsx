import React, { useEffect, useMemo, useState } from 'react';

const b = () => window.repairCenterBridge;

export default function AccountsPage() {
  const [rows, setRows] = useState([]);
  const [availableRoles, setAvailableRoles] = useState([]);
  const [query, setQuery] = useState('');
  const [roleFilter, setRoleFilter] = useState('all');
  const [statusFilter, setStatusFilter] = useState('all');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const refresh = async () => {
    setLoading(true); setError('');
    try {
      const [users, roles] = await Promise.all([b().api('/api/accounts'), b().api('/api/accounts/roles')]);
      setRows(users); setAvailableRoles(roles); window.repairCenterAccounts = users; window.repairCenterAccountRoles = roles;
      b().setAccountData?.(users, roles);
    } catch (e) { setError(e.message || 'Không tải được tài khoản.'); }
    finally { setLoading(false); }
  };
  useEffect(() => { window.refreshAccountsReact = refresh; refresh(); return () => { delete window.refreshAccountsReact; }; }, []);
  const filtered = useMemo(() => rows.filter(u => {
    const matchesQuery=!query.trim()||[u.userName,u.fullName,u.email].join(' ').toLowerCase().includes(query.trim().toLowerCase());
    const matchesRole=roleFilter==='all'||u.roles.includes(roleFilter);
    const matchesStatus=statusFilter==='all'||(statusFilter==='active'?u.isActive:!u.isActive);
    return matchesQuery&&matchesRole&&matchesStatus;
  }), [rows, query, roleFilter, statusFilter]);
  const hasFilters=query.trim()||roleFilter!=='all'||statusFilter!=='all';
  const clearFilters=()=>{setQuery('');setRoleFilter('all');setStatusFilter('all')};
  return <>
    {error && <div className="panel"><strong>Không tải được dữ liệu</strong><p className="muted">{error}</p><button className="button" onClick={refresh}>Thử lại</button></div>}
    {loading ? <div className="empty-state">Đang tải tài khoản…</div> : !error && <>
      <div className="toolbar"><input className="search" aria-label="Tìm tài khoản" placeholder="Tìm tài khoản…" value={query} onChange={e => setQuery(e.target.value)} />
        <select className="filter-select" aria-label="Lọc theo quyền tài khoản" value={roleFilter} onChange={e=>setRoleFilter(e.target.value)}><option value="all">Tất cả quyền</option>{availableRoles.map(role=><option key={role} value={role}>{role}</option>)}</select>
        <select className="filter-select" aria-label="Lọc theo trạng thái tài khoản" value={statusFilter} onChange={e=>setStatusFilter(e.target.value)}><option value="all">Tất cả trạng thái</option><option value="active">Hoạt động</option><option value="inactive">Đã khóa</option></select>
        {hasFilters&&<button className="button" onClick={clearFilters}>Xóa bộ lọc</button>}
        <button className="button primary" onClick={() => window.accountForm()}>＋ Tạo tài khoản</button></div>
      <div className="table-card"><div className="table-head"><h2>Tài khoản · {filtered.length}/{rows.length}</h2><div className="table-tools"><button className="button small" onClick={refresh}>Làm mới</button></div></div>
        <div className="table-scroll"><table><thead><tr><th>Tài khoản</th><th>Tên hiển thị</th><th>Email</th><th>Quyền</th><th>Trạng thái</th><th>Thao tác</th></tr></thead>
          <tbody>{filtered.length ? filtered.map(u => <tr key={u.id}><td className="primary-cell">{u.userName}<span className="secondary">#{u.id}</span></td><td>{u.fullName}</td><td>{u.email}</td><td>{u.roles.map(r => <span key={r} className="badge blue">{r}</span>)}</td><td><span className={`badge ${u.isActive ? 'green' : 'amber'}`}>{u.isActive ? 'Hoạt động' : 'Đã khóa'}</span></td><td><button className="icon-action" onClick={() => window.editRoles(u.id)}>Quyền</button><button className="icon-action" onClick={() => window.toggleAccount(u.id, !u.isActive)}>{u.isActive ? 'Khóa' : 'Mở khóa'}</button><button className="icon-action" onClick={() => window.resetAccountPassword(u.id)}>Đặt lại mật khẩu</button><button className="icon-action" onClick={() => window.revokeAccount(u.id)}>Thu hồi token</button></td></tr>) : <tr><td colSpan="6" className="empty">Không tìm thấy tài khoản phù hợp</td></tr>}</tbody>
        </table></div>
      </div>
    </>}
  </>;
}
