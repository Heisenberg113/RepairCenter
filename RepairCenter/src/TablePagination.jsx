import React from 'react';

export default function TablePagination({ page, pageSize, totalCount, onPageChange }) {
  if (totalCount <= pageSize) return null;

  const pageCount = Math.ceil(totalCount / pageSize);
  const currentPage = Math.min(Math.max(1, page), pageCount);
  const firstItem = (currentPage - 1) * pageSize + 1;
  const lastItem = Math.min(currentPage * pageSize, totalCount);

  return (
    <div className="table-pagination" aria-label="Phân trang danh sách">
      <span className="muted">Hiển thị {firstItem}–{lastItem} / {totalCount} · Trang {currentPage} / {pageCount}</span>
      <div className="table-tools">
        <button type="button" className="button small" disabled={currentPage <= 1} onClick={() => onPageChange(currentPage - 1)}>Trước</button>
        <button type="button" className="button small" disabled={currentPage >= pageCount} onClick={() => onPageChange(currentPage + 1)}>Sau</button>
      </div>
    </div>
  );
}
