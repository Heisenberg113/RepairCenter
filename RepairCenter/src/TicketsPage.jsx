import React, { useCallback, useEffect, useRef, useState } from 'react';
import StatusBadge from './StatusBadge.jsx';

const b = () => window.repairCenterBridge;

export default function TicketsPage() {
  const [rows, setRows] = useState([]);
  const [query, setQuery] = useState('');
  const [status, setStatus] = useState('');
  const [receivedFrom, setReceivedFrom] = useState('');
  const [receivedTo, setReceivedTo] = useState('');
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const pageSize = 50;
  const [selectedId, setSelectedId] = useState(null);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(true);
  const requestId = useRef(0);
  const refresh = useCallback(async () => {
    const currentRequest = ++requestId.current;
    setLoading(true);
    setError('');
    try {
      const params = new URLSearchParams({ page, pageSize });
      if (query.trim()) params.set('search', query.trim());
      if (status) params.set('status', status);
      if (receivedFrom) params.set('receivedFrom', `${receivedFrom}T00:00:00Z`);
      if (receivedTo) {
        const end = new Date(`${receivedTo}T00:00:00Z`);
        end.setUTCDate(end.getUTCDate() + 1);
        params.set('receivedTo', end.toISOString());
      }
      const result = await b().api(`/api/repair-tickets?${params}`);
      if (currentRequest !== requestId.current) return;
      const lastPage = Math.max(1, Math.ceil(result.totalCount / pageSize));
      if (page > lastPage) {
        setPage(lastPage);
        return;
      }
      setRows(result.items);
      setTotalCount(result.totalCount);
    } catch (e) {
      if (currentRequest === requestId.current)
        setError(e.message || 'Không tải được danh sách phiếu.');
    } finally {
      if (currentRequest === requestId.current)
        setLoading(false);
    }
  }, [page, pageSize, query, receivedFrom, receivedTo, status]);

  useEffect(() => {
    refresh();
    const onTicketMutation = event => {
      if (event.detail?.path?.endsWith('/status')) setStatus('');
      refresh();
    };
    window.addEventListener('repair-tickets-changed', onTicketMutation);
    return () => {
      window.removeEventListener('repair-tickets-changed', onTicketMutation);
      requestId.current++;
    };
  }, [refresh]);

  useEffect(() => {
    window.openTicketReact = setSelectedId;
    return () => { delete window.openTicketReact; };
  }, []);

  const updateFilter = setter => value => {
    setter(value);
    setPage(1);
  };
  const pageCount = Math.max(1, Math.ceil(totalCount / pageSize));

  return (
    <>
      <div className="toolbar">
        <input className="search" placeholder="Tìm mã phiếu, khách hàng, số điện thoại, thiết bị…" value={query} onChange={e => updateFilter(setQuery)(e.target.value)} />
        <select value={status} onChange={e => updateFilter(setStatus)(e.target.value)}>
          <option value="">Tất cả trạng thái</option>
          {Object.entries(b().statusLabels).map(([key, label]) => <option key={key} value={key}>{label}</option>)}
        </select>
        <label className="ticket-date-filter">Tiếp nhận từ<input aria-label="Ngày tiếp nhận từ" type="date" value={receivedFrom} max={receivedTo || undefined} onChange={e => updateFilter(setReceivedFrom)(e.target.value)} /></label>
        <label className="ticket-date-filter">Đến ngày<input aria-label="Ngày tiếp nhận đến" type="date" value={receivedTo} min={receivedFrom || undefined} onChange={e => updateFilter(setReceivedTo)(e.target.value)} /></label>
        {(receivedFrom || receivedTo) && <button className="button" onClick={() => { updateFilter(setReceivedFrom)(''); updateFilter(setReceivedTo)(''); }}>Xóa ngày lọc</button>}
      </div>
      {error && (
        <div className="panel">
          <strong>Không tải được dữ liệu</strong>
          <p className="muted">{error}</p>
          <button className="button" onClick={refresh}>Thử lại</button>
        </div>
      )}
      {loading ? (
        <div className="empty-state">Đang tải phiếu…</div>
      ) : !error && (
        <div className="table-card">
          <div className="table-head">
            <h2>Phiếu · {totalCount}</h2>
            <div className="table-tools">
              <button className="button small" onClick={refresh}>Làm mới</button>
            </div>
          </div>
          <div className="table-scroll">
            <table>
              <thead>
                <tr>
                  {['Mã phiếu', 'Khách hàng / Thiết bị', 'Số điện thoại', 'Kỹ thuật viên', 'Trạng thái', 'Tiếp nhận', ''].map(x => <th key={x}>{x}</th>)}
                </tr>
              </thead>
              <tbody>
                {rows.length ? rows.map(t => (
                  <tr key={t.id}>
                    <td><button className="icon-action ticket-number" onClick={() => setSelectedId(t.id)}>{t.ticketNumber}</button></td>
                    <td>{t.customerName}<span className="secondary">{`${t.deviceBrand || ''} ${t.deviceModel || ''} · ${t.deviceType || ''}`}</span></td>
                    <td>{t.customerPhoneNumber || '—'}</td>
                    <td>{t.technicianName || 'Chưa phân công'}</td>
                    <td><StatusBadge status={t.status} /></td>
                    <td>{b().date(t.receivedAt)}</td>
                    <td><button className="button small" onClick={() => setSelectedId(t.id)}>Mở phiếu</button></td>
                  </tr>
                )) : (
                  <tr><td colSpan="7" className="empty">Chưa có phiếu phù hợp</td></tr>
                )}
              </tbody>
            </table>
          </div>
          <div className="table-head">
            <span className="muted">Trang {page} / {pageCount}</span>
            <div className="table-tools">
              <button className="button small" disabled={page <= 1} onClick={() => setPage(current => current - 1)}>Trước</button>
              <button className="button small" disabled={page >= pageCount} onClick={() => setPage(current => current + 1)}>Sau</button>
            </div>
          </div>
        </div>
      )}
      {selectedId && <TicketDetail id={selectedId} onClose={() => setSelectedId(null)} onChanged={refresh} />}
    </>
  );
}

function TicketDetail({ id, onClose, onChanged }) {
  const [data, setData] = useState(null);
  const [error, setError] = useState('');

  const loadTicket = async () => {
    try {
      const [ticket, details, history, quotes] = await Promise.all([
        b().api(`/api/repair-tickets/${id}`),
        b().api(`/api/repair-tickets/${id}/details`),
        b().api(`/api/repair-tickets/${id}/status-history`),
        b().api(`/api/repair-tickets/${id}/quotes`).catch(() => null)
      ]);
      setData({ ticket, details, history, quotes });
    } catch (e) {
      setError(e.message || 'Không tải được chi tiết phiếu.');
    }
  };

  useEffect(() => {
    loadTicket();
    const handleMutation = () => {
      loadTicket();
      onChanged?.();
    };
    window.addEventListener('repair-tickets-changed', handleMutation);
    return () => {
      window.removeEventListener('repair-tickets-changed', handleMutation);
    };
  }, [id]);

  if (error) {
    return (
      <div className="modal-backdrop" onMouseDown={e => { if (e.target === e.currentTarget) onClose(); }}>
        <div className="modal">
          <div className="modal-head">
            <h2>Chi tiết phiếu</h2>
            <button className="close" onClick={onClose}>×</button>
          </div>
          <div className="modal-body">{error}</div>
        </div>
      </div>
    );
  }

  if (!data) {
    return (
      <div className="modal-backdrop">
        <div className="modal">
          <div className="modal-body">Đang tải chi tiết phiếu…</div>
        </div>
      </div>
    );
  }

  const { ticket: t, details, history, quotes } = data;
  const status = b().enumKey(t.status);
  const editable = ['Diagnosing', 'WaitingForApproval', 'InProgress'].includes(status);
  const closed = ['Completed', 'Delivered', 'Cancelled'].includes(status);

  const handleCreateQuote = async () => {
    if (!window.confirm('Tạo và lưu phiên bản báo giá từ các hạng mục hiện tại?')) return;
    try {
      const q = await b().api(`/api/repair-tickets/${id}/quotes`, { method: 'POST', body: {} });
      b().toast?.(q.isDuplicate
        ? `Báo giá không thay đổi; giữ nguyên phiên bản ${q.versionNumber}`
        : `Đã tạo báo giá phiên bản ${q.versionNumber}`);
      await loadTicket();
      onChanged?.();
      window.dispatchEvent(new CustomEvent('repair-tickets-changed', { detail: { path: `/api/repair-tickets/${id}/quotes`, method: 'POST' } }));
    } catch (e) {
      b().toast?.(e.message || 'Không thể tạo báo giá', true);
    }
  };

  const handleDeleteDetail = async (detailId) => {
    if (!window.confirm('Xóa hạng mục này khỏi phiếu? Nếu là phụ tùng, số lượng sẽ được hoàn về kho.')) return;
    try {
      await b().api(`/api/repair-tickets/${id}/details/${detailId}`, { method: 'DELETE' });
      b().toast?.('Đã xóa hạng mục');
      await loadTicket();
      onChanged?.();
      window.dispatchEvent(new CustomEvent('repair-tickets-changed', { detail: { path: `/api/repair-tickets/${id}/details`, method: 'DELETE' } }));
    } catch (e) {
      b().toast?.(e.message || 'Không thể xóa hạng mục', true);
    }
  };

  const openInvoice = () => {
    onClose();
    window.navigate('billing').then(() => window.openInvoiceReact?.(id));
  };

  const quotePanel = quotes === null ? (
    <p className="field-note">Chức năng báo giá chưa sẵn sàng. Cần cập nhật cơ sở dữ liệu.</p>
  ) : quotes.length ? (
    quotes.map(q => (
      <div className="detail-box split" key={q.id}>
        <span>
          <strong>Phiên bản {q.versionNumber} · {b().money(q.totalAmount)}</strong>
          <small className="secondary">
            {b().date(q.createdAt)} · {q.response === 'Accepted' ? 'Khách đồng ý' : q.response === 'Rejected' ? 'Khách từ chối' : q.response === 'RevisionRequested' ? 'Khách yêu cầu sửa' : 'Chờ phản hồi'}
          </small>
          {q.responseNote && <small className="secondary">{q.responseChannel || ''} · {q.responseNote}</small>}
        </span>
        <span className="quick-actions">
          <button className="button small" onClick={() => b().openQuoteVersion(id, q.id, false)}>Xem báo giá</button>
          <button className="button small" onClick={() => b().openQuoteVersion(id, q.id, true)}>In báo giá</button>
          {b().can('Admin', 'Receptionist') && !closed && q.versionNumber === quotes[quotes.length - 1].versionNumber && (
            <button className="button small" onClick={() => window.openWorkflowDialog?.('quoteResponse', { ticketId: id, versionId: q.id })}>
              Ghi nhận phản hồi
            </button>
          )}
        </span>
      </div>
    ))
  ) : (
    <p className="muted">Chưa tạo phiên bản báo giá.</p>
  );

  return (
    <div className="modal-backdrop" onMouseDown={e => { if (e.target === e.currentTarget) onClose(); }}>
      <div className="modal">
        <div className="modal-head">
          <h2>Phiếu {t.ticketNumber}</h2>
          <div className="quick-actions">
            <button className="button small" onClick={() => b().printTicket(id)}>In phiếu</button>
            {['Completed', 'Delivered'].includes(status) && b().can('Admin', 'Receptionist') && (
              <button className="button small primary" onClick={openInvoice}>
                Hóa đơn / thanh toán
              </button>
            )}
            <button className="close" onClick={onClose}>×</button>
          </div>
        </div>
        <div className="modal-body">
          <div className="split">
            <div>
              <StatusBadge status={t.status} />
              <h3>{t.customerName} · {t.deviceBrand} {t.deviceModel}</h3>
              <p className="muted">{t.deviceType || ''} · Tiếp nhận {b().date(t.receivedAt)} · KTV: {t.technicianName || 'Chưa phân công'}</p>
            </div>
            <div className="quick-actions">
              {b().can('Admin', 'Receptionist') && (
                <button className="button small" onClick={() => window.openWorkflowDialog?.('assign', { id })}>
                  {t.technicianId ? 'Đổi kỹ thuật viên' : 'Phân công'}
                </button>
              )}
              <button className="button small soft" onClick={() => window.openWorkflowDialog?.('status', { id, current: status })}>
                Chuyển trạng thái
              </button>
              {status === 'WaitingForApproval' && details.length > 0 && b().can('Admin', 'Receptionist') && (
                <button className="button small primary" onClick={handleCreateQuote}>
                  ＋ Tạo báo giá
                </button>
              )}
              {!closed && (
                <button className="button small" onClick={() => window.openWorkflowDialog?.('addService', { id })}>
                  ＋ Dịch vụ
                </button>
              )}
              {!closed && b().can('Admin', 'Technician', 'Receptionist') && (
                <button className="button small" onClick={() => window.openWorkflowDialog?.('addPart', { id })}>
                  ＋ Phụ tùng
                </button>
              )}
            </div>
          </div>
          <div className="detail-box">
            <strong>Mô tả lỗi</strong>
            <p>{t.issueDescription}</p>
            {t.customerNotes && <small className="secondary">Ghi chú: {t.customerNotes}</small>}
          </div>
          <div className="panel">
            <h3>Lịch sử báo giá</h3>
            {quotePanel}
          </div>
          <div className="grid two-col">
            <div>
              <strong>Hạng mục trên phiếu</strong>
              {details.map(d => (
                <div className="detail-box split" key={d.id}>
                  <span>
                    {d.description}
                    <small className="secondary">{d.quantity} × {b().money(d.unitPrice)}</small>
                  </span>
                  <span>
                    <strong>{b().money(d.totalAmount)}</strong>
                    {editable && (
                      <span>
                        <button className="icon-action" onClick={() => window.openWorkflowDialog?.('editDetail', { ticketId: id, detailId: d.id, quantity: d.quantity, discountPercent: d.discountPercent })}>
                          Sửa
                        </button>
                        <button className="icon-action" onClick={() => handleDeleteDetail(d.id)}>
                          Xóa
                        </button>
                      </span>
                    )}
                  </span>
                </div>
              ))}
            </div>
            <div>
              <strong>Lịch sử trạng thái</strong>
              <div className="timeline">
                {history.map(x => {
                  const isQuoteChange = x.note === 'Thay đổi báo giá';
                  return (
                    <div className="timeline-item" key={x.id}>
                      <strong>{isQuoteChange
                        ? 'Thay đổi báo giá'
                        : b().statusLabels[b().enumKey(x.newStatus)] || x.newStatus}</strong>
                      <small>{b().date(x.changedAt)} · {x.changedByUserName || ''}{x.note && !isQuoteChange ? ` · ${x.note}` : ''}</small>
                    </div>
                  );
                })}
              </div>
            </div>
          </div>
        </div>
        <div className="modal-foot">
          <button className="button" onClick={onClose}>Đóng</button>
        </div>
      </div>
    </div>
  );
}
