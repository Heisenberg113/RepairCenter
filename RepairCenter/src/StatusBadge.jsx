import React from 'react';

export const statusLabels = {
  Received: 'Mới tiếp nhận',
  Diagnosing: 'Đang kiểm tra',
  WaitingForApproval: 'Chờ khách duyệt',
  InProgress: 'Đang sửa',
  WaitingForParts: 'Chờ linh kiện',
  Completed: 'Hoàn tất',
  Delivered: 'Đã giao',
  Cancelled: 'Đã hủy'
};

export const enumValues = {
  1: 'Received',
  2: 'Diagnosing',
  3: 'WaitingForApproval',
  4: 'InProgress',
  5: 'WaitingForParts',
  6: 'Completed',
  7: 'Delivered',
  8: 'Cancelled'
};

export const badgeClass = {
  Received: 'blue',
  Diagnosing: 'amber',
  WaitingForApproval: 'amber',
  InProgress: 'blue',
  WaitingForParts: 'amber',
  Completed: 'green',
  Delivered: 'green',
  Cancelled: 'red'
};

export function getStatusKey(value) {
  return typeof value === 'number' ? enumValues[value] || String(value) : value;
}

export default function StatusBadge({ status, value, label }) {
  const val = status !== undefined ? status : value;
  const key = getStatusKey(val);
  const color = badgeClass[key] || '';
  const text = label || statusLabels[key] || key || '—';
  return <span className={`badge ${color}`.trim()}>{text}</span>;
}
