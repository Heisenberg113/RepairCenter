import React, { useEffect, useState } from 'react';
import StatusBadge from './StatusBadge.jsx';

const bridge = () => window.repairCenterBridge;

export default function OverviewPage() {
  const [data, setData] = useState(null);
  const [error, setError] = useState('');

  useEffect(() => {
    let cancelled = false;
    const load = async () => {
      setError('');
      const can = bridge().can;
      try {
        const [ticketPage, customers, parts, technicians] = await Promise.all([
          bridge().api('/api/repair-tickets?page=1&pageSize=7'),
          can('Admin', 'Receptionist') ? bridge().api('/api/customers') : Promise.resolve([]),
          can('Admin', 'Technician') ? bridge().api('/api/spare-parts?lowStockOnly=true') : Promise.resolve([]),
          can('Admin', 'Receptionist') ? bridge().api('/api/technicians') : Promise.resolve([])
        ]);
        if (!cancelled) setData({ ticketPage, customers, parts, technicians });
      } catch (e) {
        if (!cancelled) setError(e.message || 'Không tải được dữ liệu tổng quan.');
      }
    };
    load();
    window.addEventListener('repair-tickets-changed', load);
    return () => { cancelled = true; window.removeEventListener('repair-tickets-changed', load); };
  }, []);

  if (error) return <div className="panel"><strong>Không tải được dữ liệu</strong><p className="muted">{error}</p><button className="button" onClick={() => window.mountOverviewReact()}>Thử lại</button></div>;
  if (!data) return <div className="empty-state">Đang tải tổng quan…</div>;

  const { ticketPage, customers, parts, technicians } = data;
  const stats = [
    ['Phiếu đang xử lý', ticketPage.activeCount, `Tổng ${ticketPage.totalCount} phiếu tiếp nhận`],
    ['Khách hàng', bridge().can('Admin', 'Receptionist') ? customers.length : '—', 'Hồ sơ đang lưu'],
    ['Cảnh báo tồn kho', bridge().can('Admin', 'Technician') ? parts.length : '—', 'Phụ tùng cần nhập thêm'],
    ['Kỹ thuật viên hoạt động', bridge().can('Admin', 'Receptionist') ? technicians.filter(x => x.isActive).length : '—', 'Đội ngũ kỹ thuật']
  ];

  return <>
    <div className="grid stats">{stats.map(([label, value, hint]) => <div className="stat" key={label}>
      <div className="stat-label">{label}</div><div className="stat-value">{value}</div><div className="stat-hint">{hint}</div>
    </div>)}</div>
    <div className="grid two-col">
      <div className="table-card">
        <div className="table-head"><h2>Phiếu gần đây</h2><div className="table-tools"><button className="button small soft" onClick={() => window.navigate('tickets')}>Xem tất cả →</button></div></div>
        <div className="table-scroll"><table>
          <thead><tr>{['Mã phiếu', 'Khách hàng / Thiết bị', 'Trạng thái', 'Ngày nhận'].map(x => <th key={x}>{x}</th>)}</tr></thead>
          <tbody>{ticketPage.items.map(t => <tr key={t.id}>
            <td><button className="icon-action ticket-number" onClick={() => bridge().ticketDetail(t.id)}>{t.ticketNumber}</button></td>
            <td>{t.customerName}<span className="secondary">{t.deviceBrand} {t.deviceModel}</span></td>
            <td><StatusBadge status={t.status} /></td>
            <td>{bridge().date(t.receivedAt)}</td>
          </tr>)}</tbody>
        </table></div>
      </div>
      <div className="panel">
        <div className="panel-head"><div><h2>Thao tác nhanh</h2><p>Các công việc thường dùng</p></div></div>
        <div className="quick-actions">
          {bridge().can('Admin', 'Receptionist') && <>
            <button className="button primary" onClick={bridge().openIntake}>＋ Tiếp nhận sửa chữa</button>
            <button className="button" onClick={() => window.customerForm()}>＋ Thêm khách hàng</button>
          </>}
          {bridge().can('Admin', 'Technician') && <button className="button" onClick={() => window.navigate('inventory')}>Kiểm tra kho phụ tùng</button>}
          {bridge().can('Admin') && <button className="button" onClick={() => window.navigate('accounts')}>＋ Tạo / quản lý tài khoản</button>}
          <button className="button" onClick={() => window.navigate('warranties')}>Tra cứu bảo hành</button>
        </div>
        <div className="detail-box"><strong>Quy trình tiếp nhận</strong><p className="muted">Chọn hoặc tạo khách hàng, ghi nhận thiết bị và mô tả lỗi. Mã phiếu được tạo tự động.</p></div>
      </div>
    </div>
  </>;
}
