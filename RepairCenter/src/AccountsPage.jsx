import React, { useEffect, useMemo, useState } from 'react';

const b = () => window.repairCenterBridge;

export default function AccountsPage() {
  const [rows, setRows] = useState([]);
  const [query, setQuery] = useState('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const refresh = async () => {
    setLoading(true); setError('');
    try {
      const [users, roles] = await Promise.all([b().api('/api/accounts'), b().api('/api/accounts/roles')]);
      setRows(users); window.repairCenterAccounts = users; window.repairCenterAccountRoles = roles;
      b().setAccountData?.(users, roles);
    } catch (e) { setError(e.message || 'Không tải được tài khoản.'); }
    finally { setLoading(false); }
  };
  useEffect(() => { window.refreshAccountsReact = refresh; refresh(); return () => { delete window.refreshAccountsReact; }; }, []);
  const filtered = useMemo(() => rows.filter(u => !query || [u.userName, u.fullName, u.email].join(' ').toLowerCase().includes(query.toLowerCase())), [rows, query]);
  return <>
    {error && <div className="panel"><strong>Không tải được dữ liệu</strong><p className="muted">{error}</p><button className="button" onClick={refresh}>Thử lại</button></div>}
    {loading ? <div className="empty-state">Đang tải tài khoản…</div> : !error && <>
      <div className="toolbar"><input className="search" placeholder="Tìm tài khoản…" value={query} onChange={e => setQuery(e.target.value)} /><button className="button primary" onClick={() => window.accountForm()}>＋ Tạo tài khoản</button></div>
      <div className="table-card"><div className="table-head"><h2>Tài khoản · {filtered.length}</h2><div className="table-tools"><button className="button small" onClick={refresh}>Làm mới</button></div></div>
        <div className="table-scroll"><table><thead><tr><th>Tài khoản</th><th>Tên hiển thị</th><th>Email</th><th>Quyền</th><th>Trạng thái</th><th>Thao tác</th></tr></thead>
          <tbody>{filtered.length ? filtered.map(u => <tr key={u.id}><td className="primary-cell">{u.userName}<span className="secondary">#{u.id}</span></td><td>{u.fullName}</td><td>{u.email}</td><td>{u.roles.map(r => <span key={r} className="badge blue">{r}</span>)}</td><td><span className={`badge ${u.isActive ? 'green' : 'amber'}`}>{u.isActive ? 'Hoạt động' : 'Đã khóa'}</span></td><td><button className="icon-action" onClick={() => window.editRoles(u.id)}>Quyền</button><button className="icon-action" onClick={() => window.toggleAccount(u.id, !u.isActive)}>{u.isActive ? 'Khóa' : 'Mở khóa'}</button><button className="icon-action" onClick={() => window.resetAccountPassword(u.id)}>Đặt lại mật khẩu</button><button className="icon-action" onClick={() => window.revokeAccount(u.id)}>Thu hồi token</button></td></tr>) : <tr><td colSpan="6" className="empty">Không tìm thấy tài khoản phù hợp</td></tr>}</tbody>
        </table></div>
      </div>
    </>}
  </>;
}
