import React, { useEffect, useMemo, useState } from 'react';
import TablePagination from './TablePagination.jsx';

const b = () => window.repairCenterBridge;
const PAGE_SIZE = 10;

export default function CustomersPage() {
  const [customers, setCustomers] = useState([]);
  const [devices, setDevices] = useState([]);
  const [customerQuery, setCustomerQuery] = useState('');
  const [customerDeviceFilter, setCustomerDeviceFilter] = useState('all');
  const [deviceQuery, setDeviceQuery] = useState('');
  const [deviceCustomerFilter, setDeviceCustomerFilter] = useState('all');
  const [deviceTypeFilter, setDeviceTypeFilter] = useState('all');
  const [customerPage, setCustomerPage] = useState(1);
  const [devicePage, setDevicePage] = useState(1);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const refresh = async () => {
    setLoading(true);
    setError('');
    try {
      const [customerRows, deviceRows] = await Promise.all([
        b().api('/api/customers'),
        b().api('/api/devices')
      ]);
      setCustomers(customerRows);
      setDevices(deviceRows);
    } catch (e) {
      setError(e.message || 'Không tải được khách hàng và thiết bị.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    window.refreshCustomersReact = refresh;
    refresh();
    return () => { delete window.refreshCustomersReact; };
  }, []);

  const deviceCounts = useMemo(() => {
    const counts = new Map();
    devices.forEach(device => counts.set(device.customerId, (counts.get(device.customerId) || 0) + 1));
    return counts;
  }, [devices]);

  const deviceTypes = useMemo(() => Array.from(new Map(
    devices.map(device => [device.deviceTypeId, device.deviceTypeName || 'Loại thiết bị'])
  ).entries()).sort((a, c) => a[1].localeCompare(c[1], 'vi')), [devices]);

  const sortedCustomers = useMemo(
    () => [...customers].sort((a, c) => a.name.localeCompare(c.name, 'vi')),
    [customers]
  );

  const filteredCustomers = useMemo(() => {
    const term = customerQuery.trim().toLowerCase();
    return customers.filter(customer => {
      const matchesQuery = !term || [
        customer.name, customer.phoneNumber, customer.email, customer.address
      ].join(' ').toLowerCase().includes(term);
      const count = deviceCounts.get(customer.id) || 0;
      const matchesDevice = customerDeviceFilter === 'all' ||
        (customerDeviceFilter === 'with-device' ? count > 0 : count === 0);
      return matchesQuery && matchesDevice;
    });
  }, [customers, customerQuery, customerDeviceFilter, deviceCounts]);

  const filteredDevices = useMemo(() => {
    const term = deviceQuery.trim().toLowerCase();
    return devices.filter(device => {
      const matchesQuery = !term || [
        device.customerName, device.deviceTypeName, device.brand, device.model,
        device.serialNumber, device.color, device.description
      ].join(' ').toLowerCase().includes(term);
      const matchesCustomer = deviceCustomerFilter === 'all' ||
        String(device.customerId) === deviceCustomerFilter;
      const matchesType = deviceTypeFilter === 'all' ||
        String(device.deviceTypeId) === deviceTypeFilter;
      return matchesQuery && matchesCustomer && matchesType;
    });
  }, [devices, deviceQuery, deviceCustomerFilter, deviceTypeFilter]);

  const customerPageCount = Math.max(1, Math.ceil(filteredCustomers.length / PAGE_SIZE));
  const currentCustomerPage = Math.min(customerPage, customerPageCount);
  const customerPageRows = filteredCustomers.slice((currentCustomerPage - 1) * PAGE_SIZE, currentCustomerPage * PAGE_SIZE);
  const devicePageCount = Math.max(1, Math.ceil(filteredDevices.length / PAGE_SIZE));
  const currentDevicePage = Math.min(devicePage, devicePageCount);
  const devicePageRows = filteredDevices.slice((currentDevicePage - 1) * PAGE_SIZE, currentDevicePage * PAGE_SIZE);

  const hasCustomerFilters = customerQuery.trim() || customerDeviceFilter !== 'all';
  const hasDeviceFilters = deviceQuery.trim() || deviceCustomerFilter !== 'all' || deviceTypeFilter !== 'all';
  const clearCustomerFilters = () => {
    setCustomerQuery('');
    setCustomerDeviceFilter('all');
    setCustomerPage(1);
  };
  const clearDeviceFilters = () => {
    setDeviceQuery('');
    setDeviceCustomerFilter('all');
    setDeviceTypeFilter('all');
    setDevicePage(1);
  };

  return <>
    <div className="toolbar">
      <button className="button soft" onClick={() => window.navigate('deviceTypes')}>▱ Loại thiết bị</button>
      <button className="button" onClick={() => window.deviceForm()}>＋ Thiết bị</button>
      <button className="button primary" onClick={() => window.customerForm()}>＋ Khách hàng</button>
      <button className="button" onClick={refresh}>Làm mới</button>
    </div>
    {error && <div className="panel"><strong>Không tải được dữ liệu</strong><p className="muted">{error}</p><button className="button" onClick={refresh}>Thử lại</button></div>}
    {loading ? <div className="empty-state">Đang tải khách hàng và thiết bị…</div> : !error && <>
      <div className="table-card">
        <div className="table-head"><h2>Khách hàng · {filteredCustomers.length}/{customers.length}</h2></div>
        <div className="list-filter-bar">
          <input className="search" aria-label="Tìm khách hàng" placeholder="Tìm tên, số điện thoại, email, địa chỉ…" value={customerQuery} onChange={e => { setCustomerQuery(e.target.value); setCustomerPage(1); }} />
          <select className="filter-select" aria-label="Lọc khách hàng theo thiết bị" value={customerDeviceFilter} onChange={e => { setCustomerDeviceFilter(e.target.value); setCustomerPage(1); }}>
            <option value="all">Tất cả khách hàng</option>
            <option value="with-device">Có thiết bị</option>
            <option value="without-device">Chưa có thiết bị</option>
          </select>
          {hasCustomerFilters && <button className="button" onClick={clearCustomerFilters}>Xóa bộ lọc</button>}
        </div>
        <div className="table-scroll"><table>
          <thead><tr>{['Khách hàng', 'Liên hệ', 'Địa chỉ', 'Ngày tạo', ''].map(x => <th key={x}>{x}</th>)}</tr></thead>
          <tbody>{filteredCustomers.length ? customerPageRows.map(c => <tr key={c.id}>
            <td className="primary-cell"><button className="icon-action ticket-number" onClick={() => window.customerTickets(c.id)}>{c.name}</button><span className="secondary">#{c.id} · {deviceCounts.get(c.id) || 0} thiết bị</span></td>
            <td>{c.phoneNumber || '—'}<span className="secondary">{c.email || ''}</span></td>
            <td>{c.address || '—'}</td><td>{b().date(c.createdAt)}</td>
            <td><button className="icon-action" onClick={() => window.editCustomer(c.id)}>Sửa</button><button className="icon-action" onClick={() => window.deleteCustomer(c.id)}>Xóa</button></td>
          </tr>) : <tr><td colSpan="5" className="empty">{customers.length ? 'Không tìm thấy khách hàng phù hợp. Hãy đổi hoặc xóa bộ lọc.' : 'Chưa có khách hàng.'}</td></tr>}</tbody>
        </table></div>
        <TablePagination page={currentCustomerPage} pageSize={PAGE_SIZE} totalCount={filteredCustomers.length} onPageChange={setCustomerPage} />
      </div>
      <div style={{ height: 16 }} />
      <div className="table-card">
        <div className="table-head"><h2>Thiết bị · {filteredDevices.length}/{devices.length}</h2></div>
        <div className="list-filter-bar">
          <input className="search" aria-label="Tìm thiết bị" placeholder="Tìm khách hàng, loại, hãng, model, serial…" value={deviceQuery} onChange={e => { setDeviceQuery(e.target.value); setDevicePage(1); }} />
          <select className="filter-select" aria-label="Lọc thiết bị theo khách hàng" value={deviceCustomerFilter} onChange={e => { setDeviceCustomerFilter(e.target.value); setDevicePage(1); }}>
            <option value="all">Tất cả khách hàng</option>
            {sortedCustomers.map(customer => <option key={customer.id} value={customer.id}>{customer.name} · {customer.phoneNumber || `#${customer.id}`}</option>)}
          </select>
          <select className="filter-select" aria-label="Lọc theo loại thiết bị" value={deviceTypeFilter} onChange={e => { setDeviceTypeFilter(e.target.value); setDevicePage(1); }}>
            <option value="all">Tất cả loại thiết bị</option>
            {deviceTypes.map(([id, name]) => <option key={id} value={id}>{name}</option>)}
          </select>
          {hasDeviceFilters && <button className="button" onClick={clearDeviceFilters}>Xóa bộ lọc</button>}
        </div>
        <div className="table-scroll"><table>
          <thead><tr>{['Khách hàng', 'Loại / thiết bị', 'Serial', 'Mô tả', ''].map(x => <th key={x}>{x}</th>)}</tr></thead>
          <tbody>{filteredDevices.length ? devicePageRows.map(d => <tr key={d.id}>
            <td>{d.customerName}</td><td className="primary-cell">{d.deviceTypeName} · {d.brand} {d.model}</td>
            <td>{d.serialNumber || '—'}</td><td>{d.description || '—'}</td>
            <td><button className="icon-action" onClick={() => window.deviceForm(d.id)}>Sửa</button><button className="icon-action" onClick={() => window.deleteDevice(d.id)}>Xóa</button></td>
          </tr>) : <tr><td colSpan="5" className="empty">{devices.length ? 'Không tìm thấy thiết bị phù hợp. Hãy đổi hoặc xóa bộ lọc.' : 'Chưa có thiết bị.'}</td></tr>}</tbody>
        </table></div>
        <TablePagination page={currentDevicePage} pageSize={PAGE_SIZE} totalCount={filteredDevices.length} onPageChange={setDevicePage} />
      </div>
    </>}
  </>;
}
