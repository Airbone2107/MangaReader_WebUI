import { Box, Typography } from '@mui/material';
import { useEffect, useState } from 'react';
import useRoleStore from '../../../stores/roleStore';
import useUiStore from '../../../stores/uiStore';
import RoleTable from '../components/RoleTable';
import RolePermissionsDialog from '../components/RolePermissionsDialog';

/**
 * @typedef {import('../../../types/user').RoleDto} RoleDto
 */

function RoleListPage() {
  const { roles, fetchRoles } = useRoleStore();
  const isLoading = useUiStore((state) => state.isLoading);

  /** @type {[RoleDto | null, React.Dispatch<React.SetStateAction<RoleDto | null>>]} */
  const [selectedRole, setSelectedRole] = useState(null);
  const [isPermissionsDialogOpen, setIsPermissionsDialogOpen] = useState(false);

  useEffect(() => {
    fetchRoles();
  }, [fetchRoles]);

  const handleManagePermissions = (roleId) => {
    const roleToManage = roles.find((r) => r.id === roleId);
    if (roleToManage) {
      setSelectedRole(roleToManage);
      setIsPermissionsDialogOpen(true);
    }
  };

  const handleCloseDialog = () => {
    setIsPermissionsDialogOpen(false);
    setSelectedRole(null);
  };

  return (
    <Box>
      <Typography variant="h4" component="h1" gutterBottom>
        Quản lý Vai trò
      </Typography>

      <RoleTable
        roles={roles}
        isLoading={isLoading}
        onManagePermissions={handleManagePermissions}
      />
      
      <RolePermissionsDialog 
        open={isPermissionsDialogOpen}
        onClose={handleCloseDialog}
        role={selectedRole}
      />
    </Box>
  );
}

export default RoleListPage; 