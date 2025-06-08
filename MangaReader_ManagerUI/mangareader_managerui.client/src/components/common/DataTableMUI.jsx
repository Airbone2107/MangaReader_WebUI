import {
    Box,
    Paper,
    Table,
    TableBody,
    TableCell,
    TableContainer,
    TableHead,
    TablePagination,
    TableRow,
    TableSortLabel,
} from '@mui/material'
import { visuallyHidden } from '@mui/utils'
import React from 'react'
import { ROWS_PER_PAGE_OPTIONS } from '../../constants/appConstants'

function DataTableMUI({
  columns,
  data,
  totalItems,
  page,
  rowsPerPage,
  onPageChange,
  onRowsPerPageChange,
  onSort,
  orderBy,
  order, // 'asc' or 'desc'
  isLoading,
  onRowClick,
}) {
  const handleSort = (property) => {
    const isAsc = orderBy === property && order === 'asc'
    onSort(property, isAsc ? 'desc' : 'asc')
  }

  return (
    <TableContainer component={Paper}>
      <Table aria-label="data table">
        <TableHead>
          <TableRow>
            {columns.map((column) => (
              <TableCell
                key={column.id}
                align={column.align || 'left'}
                padding={column.disablePadding ? 'none' : 'normal'}
                sortDirection={orderBy === column.id ? order : false}
                style={{ minWidth: column.minWidth }}
              >
                {column.sortable ? (
                  <TableSortLabel
                    active={orderBy === column.id}
                    direction={orderBy === column.id ? order : 'asc'}
                    onClick={() => handleSort(column.id)}
                  >
                    {column.label}
                    {orderBy === column.id ? (
                      <Box component="span" sx={visuallyHidden}>
                        {order === 'desc' ? 'sorted descending' : 'sorted ascending'}
                      </Box>
                    ) : null}
                  </TableSortLabel>
                ) : (
                  column.label
                )}
              </TableCell>
            ))}
          </TableRow>
        </TableHead>
        <TableBody>
          {isLoading ? (
            <TableRow>
              <TableCell colSpan={columns.length} align="center">
                Đang tải dữ liệu...
              </TableCell>
            </TableRow>
          ) : data.length === 0 ? (
            <TableRow>
              <TableCell colSpan={columns.length} align="center">
                Không tìm thấy dữ liệu.
              </TableCell>
            </TableRow>
          ) : (
            data.map((row, index) => (
              <TableRow
                hover
                key={row.id || index}
                onClick={() => onRowClick && onRowClick(row)}
                sx={{ cursor: onRowClick ? 'pointer' : 'default' }}
              >
                {columns.map((column) => {
                  const value = column.format ? column.format(row[column.id], row) : row[column.id]
                  return (
                    <TableCell key={column.id + row.id} align={column.align || 'left'}>
                      {value}
                    </TableCell>
                  )
                })}
              </TableRow>
            ))
          )}
        </TableBody>
      </Table>
      <TablePagination
        rowsPerPageOptions={ROWS_PER_PAGE_OPTIONS}
        component="div"
        count={totalItems}
        rowsPerPage={rowsPerPage}
        page={page}
        onPageChange={onPageChange}
        onRowsPerPageChange={onRowsPerPageChange}
        labelRowsPerPage="Số hàng mỗi trang:"
        labelDisplayedRows={({ from, to, count }) =>
          `${from}-${to} của ${count !== -1 ? count : `hơn ${to}`}`
        }
      />
    </TableContainer>
  )
}

export default DataTableMUI 