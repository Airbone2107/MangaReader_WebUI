import EditIcon from '@mui/icons-material/Edit';
import { Box, Chip, IconButton, Tooltip } from '@mui/material';
import DataTableMUI from '../../../components/common/DataTableMUI';

/**
 * @typedef {import('../../../types/user').UserDto} UserDto
 */

/**
 * @param {object} props
 * @param {UserDto[]} props.users
 * @param {number} props.totalUsers
 * @param {number} props.page
 * @param {number} props.rowsPerPage
 * @param {(event: React.MouseEvent<HTMLButtonElement> | null, newPage: number) => void} props.onPageChange
 * @param {(event: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => void} props.onRowsPerPageChange
 * @param {(id: string) => void} props.onEditRoles
 * @param {boolean} props.isLoading
 */
function UserTable({
  users,
  totalUsers,
  page,
  rowsPerPage,
  onPageChange,
  onRowsPerPageChange,
  onEditRoles,
  isLoading,
}) {
  const columns = [
    { id: 'userName', label: 'Tên đăng nhập', minWidth: 170 },
    { id: 'email', label: 'Email', minWidth: 200 },
    {
      id: 'roles',
      label: 'Vai trò',
      minWidth: 250,
      format: (roles) => (
        <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.5 }}>
          {roles.map((role) => (
            <Chip key={role} label={role} size="small" />
          ))}
        </Box>
      ),
    },
    {
      id: 'actions',
      label: 'Hành động',
      minWidth: 100,
      align: 'center',
      format: (value, row) => (
        <Box sx={{ display: 'flex', justifyContent: 'center', gap: 1 }}>
          <Tooltip title="Chỉnh sửa vai trò">
            <IconButton color="primary" onClick={() => onEditRoles(row.id)}>
              <EditIcon />
            </IconButton>
          </Tooltip>
        </Box>
      ),
    },
  ];

  return (
    <DataTableMUI
      columns={columns}
      data={users}
      totalItems={totalUsers}
      page={page}
      rowsPerPage={rowsPerPage}
      onPageChange={onPageChange}
      onRowsPerPageChange={onRowsPerPageChange}
      isLoading={isLoading}
      // Bỏ qua onSort vì API không hỗ trợ
    />
  );
}

export default UserTable; 