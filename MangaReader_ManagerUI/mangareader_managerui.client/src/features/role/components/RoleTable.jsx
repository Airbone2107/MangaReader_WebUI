import EditIcon from '@mui/icons-material/Edit';
import { Box, IconButton, Tooltip } from '@mui/material';
import DataTableMUI from '../../../components/common/DataTableMUI';

/**
 * @typedef {import('../../../types/user').RoleDto} RoleDto
 */

/**
 * @param {object} props
 * @param {RoleDto[]} props.roles
 * @param {(roleId: string) => void} props.onManagePermissions
 * @param {boolean} props.isLoading
 */
function RoleTable({ roles, onManagePermissions, isLoading }) {
  const columns = [
    { id: 'name', label: 'Tên Vai trò', minWidth: 200 },
    {
      id: 'actions',
      label: 'Hành động',
      minWidth: 100,
      align: 'center',
      format: (value, row) =>
        row.name !== 'SuperAdmin' ? ( // Không cho chỉnh sửa SuperAdmin
          (<Box sx={{ display: 'flex', justifyContent: 'center' }}>
            <Tooltip title="Quản lý quyền">
              <IconButton color="primary" onClick={() => onManagePermissions(row.id)}>
                <EditIcon />
              </IconButton>
            </Tooltip>
          </Box>)
        ) : null,
    },
  ];

  return (
    <DataTableMUI
      columns={columns}
      data={roles}
      totalItems={roles.length}
      page={0}
      rowsPerPage={roles.length > 0 ? roles.length : 10}
      onPageChange={() => {}} // No pagination
      onRowsPerPageChange={() => {}} // No pagination
      isLoading={isLoading}
    />
  );
}

export default RoleTable;
