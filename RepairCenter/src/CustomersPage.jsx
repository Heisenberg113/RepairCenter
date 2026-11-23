import React, { useEffect, useMemo, useState } from 'react';

const b = () => window.repairCenterBridge;

export default function CustomersPage() {
  const [customers, setCustomers] = useState([]);
  const [devices, setDevices] = useState([]);
  const [query, setQuery] = useState('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const refresh = async () => {
    setLoading(true); setError('');
    try {
      const [customerRows, deviceRows] = await Promise.all([b().api('/api/customers'), b().api('/api/devices')]);
      setCustomers(customerRows); setDevices(deviceRows);
    } catch (e) { setError(e.message || 'Không tải được khách hàng và thiết bị.'); }
    finally { setLoading(false); }
  };

  useEffect(() => {
    window.refreshCustomersReact = refresh;
    refresh();
    return () => { delete window.refreshCustomersReact; };
  }, []);

  const filteredCustomers = useMemo(() => customers.filter(c =>
    !query || [c.name, c.phoneNumber, c.email].join(' ').toLowerCase().includes(query.toLowerCase())), [customers, query]);

  return <>
    <div className="toolbar">
      <input className="search" placeholder="Tìm tên, số điện thoại, email…" value={query} onChange={e => setQuery(e.target.value)} />
      <button className="button soft" onClick={() => window.navigate('deviceTypes')}>▱ Loại thiết bị</button>
      <button className="button" onClick={() => window.deviceForm()}>＋ Thiết bị</button>
      <button className="button primary" onClick={() => window.customerForm()}>＋ Khách hàng</button>
    </div>
    {error && <div className="panel"><strong>Không tải được dữ liệu</strong><p className="muted">{error}</p><button className="button" onClick={refresh}>Thử lại</button></div>}
    {loading ? <div className="empty-state">Đang tải khách hàng và thiết bị…</div> : !error && <>
      <div className="table-card">
        <div className="table-head"><h2>Khách hàng · {filteredCustomers.length}</h2><div className="table-tools"><button className="button small" onClick={refresh}>Làm mới</button></div></div>
        <div className="table-scroll"><table>
          <thead><tr>{['Khách hàng', 'Liên hệ', 'Địa chỉ', 'Ngày tạo', ''].map(x => <th key={x}>{x}</th>)}</tr></thead>
          <tbody>{filteredCustomers.length ? filteredCustomers.map(c => <tr key={c.id}>
            <td className="primary-cell"><button className="icon-action ticket-number" onClick={() => window.customerTickets(c.id)}>{c.name}</button><span className="secondary">#{c.id}</span></td>
            <td>{c.phoneNumber || '—'}<span className="secondary">{c.email || ''}</span></td>
            <td>{c.address || '—'}</td><td>{b().date(c.createdAt)}</td>
            <td><button className="icon-action" onClick={() => window.editCustomer(c.id)}>Sửa</button><button className="icon-action" onClick={() => window.deleteCustomer(c.id)}>Xóa</button></td>
          </tr>) : <tr><td colSpan="5" className="empty">Chưa có khách hàng phù hợp</td></tr>}</tbody>
        </table></div>
      </div>
      <div style={{ height: 16 }} />
      <div className="table-card">
        <div className="table-head"><h2>Thiết bị · {devices.length}</h2></div>
        <div className="table-scroll"><table>
          <thead><tr>{['Khách hàng', 'Loại / thiết bị', 'Serial', 'Mô tả', ''].map(x => <th key={x}>{x}</th>)}</tr></thead>
          <tbody>{devices.length ? devices.map(d => <tr key={d.id}>
            <td>{d.customerName}</td><td className="primary-cell">{d.deviceTypeName} · {d.brand} {d.model}</td>
            <td>{d.serialNumber || '—'}</td><td>{d.description || '—'}</td>
            <td><button className="icon-action" onClick={() => window.deviceForm(d.id)}>Sửa</button><button className="icon-action" onClick={() => window.deleteDevice(d.id)}>Xóa</button></td>
          </tr>) : <tr><td colSpan="5" className="empty">Chưa có thiết bị</td></tr>}</tbody>
        </table></div>
      </div>
    </>}
  </>;
}
