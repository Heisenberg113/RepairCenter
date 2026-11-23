import React, { useEffect, useState } from 'react';

const b = () => window.repairCenterBridge;

export default function DeviceTypesPage() {
  const [rows, setRows] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const refresh = async () => {
    setLoading(true); setError('');
    try {
      const data = await b().api('/api/device-types');
      setRows(data);
      window.repairCenterDeviceTypes = data;
    } catch (e) { setError(e.message || 'Không tải được loại thiết bị.'); }
    finally { setLoading(false); }
  };

  useEffect(() => {
    window.refreshDeviceTypesReact = refresh;
    refresh();
    return () => { delete window.refreshDeviceTypesReact; };
  }, []);

  return <>
    {error && <div className="panel"><strong>Không tải được dữ liệu</strong><p className="muted">{error}</p><button className="button" onClick={refresh}>Thử lại</button></div>}
    {loading ? <div className="empty-state">Đang tải loại thiết bị…</div> : !error && <div className="panel">
      <div className="panel-head">
        <div><h2>Danh mục loại thiết bị</h2><p>{rows.length} loại thiết bị · dùng khi ghi nhận thiết bị và cấu hình chuyên môn kỹ thuật viên.</p></div>
        {b().can('Admin') && <button className="button primary" onClick={() => window.deviceTypeForm()}>＋ Thêm loại</button>}
      </div>
      <div className="table-card">
        <div className="table-head"><h2>Các loại hiện có</h2><div className="table-tools"><button className="button small" onClick={refresh}>Làm mới</button></div></div>
        <div className="table-scroll"><table>
          <thead><tr><th>ID</th><th>Tên loại</th><th /></tr></thead>
          <tbody>{rows.length ? rows.map(row => <tr key={row.id}>
            <td>#{row.id}</td><td className="primary-cell">{row.name}</td>
            <td>{b().can('Admin') ? <><button className="icon-action" onClick={() => window.deviceTypeForm(row.id)}>Sửa</button><button className="icon-action" onClick={() => window.deleteDeviceType(row.id)}>Xóa</button></> : 'Chỉ xem'}</td>
          </tr>) : <tr><td colSpan="3" className="empty">Chưa có loại thiết bị</td></tr>}</tbody>
        </table></div>
      </div>
    </div>}
  </>;
}
