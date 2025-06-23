import AddIcon from '@mui/icons-material/Add';
import { Box, Button, CircularProgress, Typography } from '@mui/material';
import { useEffect, useState } from 'react';
import { handleApiError } from '../../../utils/errorUtils';
import roleApi from '../../../api/roleApi';
import useUserStore from '../../../stores/userStore';
import useUiStore from '../../../stores/uiStore';
import UserTable from '../components/UserTable';
import UserCreateDialog from '../components/UserCreateDialog';
import UserEditRolesDialog from '../components/UserEditRolesDialog';

/**
 * @typedef {import('../../../types/user').UserDto} UserDto
 * @typedef {import('../../../types/user').RoleDto} RoleDto
 */

function UserListPage() {
  const {
    users,
    totalUsers,
    page,
    rowsPerPage,
    fetchUsers,
    setPage,
    setRowsPerPage,
  } = useUserStore();

  const isLoading = useUiStore((state) => state.isLoading);
  const [isDialogLoading, setDialogLoading] = useState(false);

  const [createDialogOpen, setCreateDialogOpen] = useState(false);
  const [editDialogOpen, setEditDialogOpen] = useState(false);
  /** @type {[UserDto | null, React.Dispatch<React.SetStateAction<UserDto | null>>]} */
  const [selectedUser, setSelectedUser] = useState(null);
  /** @type {[RoleDto[], React.Dispatch<React.SetStateAction<RoleDto[]>>]} */
  const [availableRoles, setAvailableRoles] = useState([]);

  useEffect(() => {
    fetchUsers(true);
  }, [fetchUsers]);

  const fetchRoles = async () => {
    try {
      setDialogLoading(true);
      const roles = await roleApi.getRoles();
      if (roles) {
        setAvailableRoles(roles);
      }
    } catch (error) {
      handleApiError(error, 'Không thể tải danh sách vai trò.');
    } finally {
      setDialogLoading(false);
    }
  };

  const handleOpenCreateDialog = async () => {
    await fetchRoles();
    setCreateDialogOpen(true);
  };

  const handleOpenEditDialog = async (userId) => {
    await fetchRoles();
    const userToEdit = users.find((u) => u.id === userId);
    if (userToEdit) {
      setSelectedUser(userToEdit);
      setEditDialogOpen(true);
    }
  };

  return (
    <Box>
      <Typography variant="h4" component="h1" gutterBottom>
        Quản lý Người dùng
      </Typography>

      <Box sx={{ display: 'flex', justifyContent: 'flex-end', mb: 2 }}>
        <Button
          variant="contained"
          color="success"
          startIcon={<AddIcon />}
          onClick={handleOpenCreateDialog}
        >
          Thêm Người dùng mới
        </Button>
      </Box>

      {isDialogLoading ? (
        <Box sx={{ display: 'flex', justifyContent: 'center', p: 5 }}>
          <CircularProgress />
        </Box>
      ) : (
        <UserTable
          users={users}
          totalUsers={totalUsers}
          page={page}
          rowsPerPage={rowsPerPage}
          onPageChange={setPage}
          onRowsPerPageChange={setRowsPerPage}
          onEditRoles={handleOpenEditDialog}
          isLoading={isLoading}
        />
      )}

      <UserCreateDialog
        open={createDialogOpen}
        onClose={() => setCreateDialogOpen(false)}
        availableRoles={availableRoles}
      />

      <UserEditRolesDialog
        open={editDialogOpen}
        onClose={() => setEditDialogOpen(false)}
        user={selectedUser}
        availableRoles={availableRoles}
      />
    </Box>
  );
}

export default UserListPage; 