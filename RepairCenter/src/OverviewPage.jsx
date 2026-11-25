import React, { useEffect, useState } from 'react';
import StatusBadge from './StatusBadge.jsx';

const bridge = () => window.repairCenterBridge;

function getGmt7DateInputValue(date) {
  const parts = new Intl.DateTimeFormat('en-CA', {
    timeZone: 'Asia/Ho_Chi_Minh',
    year: 'numeric',
    month: '2-digit',
    day: '2-digit'
  }).formatToParts(date);
  const values = Object.fromEntries(parts.map(part => [part.type, part.value]));
  return `${values.year}-${values.month}-${values.day}`;
}

const DEFAULT_REVENUE_TO = getGmt7DateInputValue(new Date());
const DEFAULT_REVENUE_FROM = `${DEFAULT_REVENUE_TO.slice(0, 7)}-01`;

export default function OverviewPage() {
  const [data, setData] = useState(null);
  const [error, setError] = useState('');
  const [revenueFrom, setRevenueFrom] = useState(DEFAULT_REVENUE_FROM);
  const [revenueTo, setRevenueTo] = useState(DEFAULT_REVENUE_TO);
  const [revenue, setRevenue] = useState(null);
  const [revenueLoading, setRevenueLoading] = useState(true);
  const [revenueError, setRevenueError] = useState('');

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

  useEffect(() => {
    if (!bridge().can('Admin')) return;
    if (!revenueFrom || !revenueTo || revenueFrom > revenueTo) {
      setRevenue(null);
      setRevenueError('Ngày bắt đầu phải nhỏ hơn hoặc bằng ngày kết thúc.');
      setRevenueLoading(false);
      return;
    }

    let cancelled = false;
    const params = new URLSearchParams({
      from: revenueFrom,
      to: revenueTo
    });
    setRevenue(null);
    setRevenueError('');
    setRevenueLoading(true);
    bridge().api(`/api/invoices/revenue?${params}`)
      .then(result => {
        if (!cancelled) setRevenue(result);
      })
      .catch(e => {
        if (!cancelled) setRevenueError(e.message || 'Không tải được thống kê doanh số.');
      })
      .finally(() => {
        if (!cancelled) setRevenueLoading(false);
      });

    return () => { cancelled = true; };
  }, [revenueFrom, revenueTo]);

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
    {bridge().can('Admin') && <div className="panel" style={{ marginBottom: 20 }}>
      <div className="panel-head"><div><h2>Doanh số và giao dịch</h2><p>Thực thu trong khoảng ngày đã chọn, tính bằng tiền thu trừ tiền hoàn.</p></div></div>
      <div className="toolbar">
        <label className="ticket-date-filter">Từ ngày<input type="date" aria-label="Doanh số từ ngày" value={revenueFrom} onChange={e => setRevenueFrom(e.target.value)} /></label>
        <label className="ticket-date-filter">Đến ngày<input type="date" aria-label="Doanh số đến ngày" value={revenueTo} onChange={e => setRevenueTo(e.target.value)} /></label>
      </div>
      {revenueError && <p className="field-note">{revenueError}</p>}
      {revenueLoading ? <div className="empty-state">Đang tải thống kê doanh số…</div> : revenue && <>
        <div className="grid three-col" style={{ marginBottom: 20 }}>
          <div className="stat"><div className="stat-label">Đã thu</div><div className="stat-value">{bridge().money(revenue.totalPayments)}</div></div>
          <div className="stat"><div className="stat-label">Đã hoàn</div><div className="stat-value">{bridge().money(revenue.totalRefunds)}</div></div>
          <div className="stat"><div className="stat-label">Doanh số thuần</div><div className="stat-value">{bridge().money(revenue.netRevenue)}</div></div>
        </div>
        <div className="table-card">
          <div className="table-head"><h2>Giao dịch · {revenue.totalCount}</h2></div>
          <div className="table-scroll revenue-transactions-scroll"><table>
            <thead><tr><th>Phiếu / hóa đơn</th><th>Khách hàng</th><th>Loại</th><th>Số tiền</th><th>Phương thức</th><th>Người ghi nhận</th><th>Thời gian</th></tr></thead>
            <tbody>{revenue.transactions.length ? revenue.transactions.map(transaction => <tr key={transaction.id}>
              <td className="primary-cell">{transaction.ticketNumber}<span className="secondary">{transaction.invoiceNumber}</span></td>
              <td>{transaction.customerName}</td>
              <td><span className={`badge ${transaction.type === 'Refund' ? 'red' : 'green'}`}>{transaction.type === 'Refund' ? 'Hoàn tiền' : 'Thu tiền'}</span></td>
              <td>{bridge().money(transaction.type === 'Refund' ? -transaction.amount : transaction.amount)}</td>
              <td>{transaction.paymentMethod}<span className="secondary">{transaction.referenceNumber || ''}</span></td>
              <td>{transaction.processedByUserName || '—'}</td>
              <td>{bridge().date(transaction.paidAt)}</td>
            </tr>) : <tr><td colSpan="7" className="empty">Không có giao dịch trong khoảng ngày này.</td></tr>}</tbody>
          </table></div>
        </div>
      </>}
    </div>}
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
