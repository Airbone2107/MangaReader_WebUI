import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  Box,
  Typography,
  FormGroup,
  FormControlLabel,
  Checkbox,
  CircularProgress,
} from '@mui/material';
import { useEffect, useState } from 'react';
import roleApi from '../../../api/roleApi';
import { showSuccessToast } from '../../../components/common/Notification';
import { handleApiError } from '../../../utils/errorUtils';

/**
 * @typedef {import('../../../types/user').RoleDto} RoleDto
 * @typedef {import('../../../types/user').RoleDetailsDto} RoleDetailsDto
 */

// Hardcoded list of all available permissions in the system
const ALL_PERMISSIONS = {
  Users: [
    'Permissions.Users.View',
    'Permissions.Users.Create',
    'Permissions.Users.Edit',
    'Permissions.Users.Delete',
  ],
  Roles: [
    'Permissions.Roles.View',
    'Permissions.Roles.Create',
    'Permissions.Roles.Edit',
    'Permissions.Roles.Delete',
  ],
  // Add other permission groups here as they are created
  // Mangas: [ ... ],
};

const getPermissionDisplayName = (permission) => {
    return permission.split('.').pop(); // e.g., "Permissions.Users.View" -> "View"
}


/**
 * @param {object} props
 * @param {boolean} props.open
 * @param {() => void} props.onClose
 * @param {RoleDto | null} props.role
 */
function RolePermissionsDialog({ open, onClose, role }) {
  const [selectedPermissions, setSelectedPermissions] = useState([]);
  const [isLoading, setIsLoading] = useState(false);
  const [isSaving, setIsSaving] = useState(false);

  useEffect(() => {
    if (open && role) {
      const fetchDetails = async () => {
        setIsLoading(true);
        try {
          const details = await roleApi.getRolePermissions(role.id);
          setSelectedPermissions(details.permissions || []);
        } catch (error) {
          handleApiError(error, `Không thể tải quyền cho vai trò ${role.name}.`);
          onClose();
        } finally {
          setIsLoading(false);
        }
      };
      fetchDetails();
    } else {
      setSelectedPermissions([]);
    }
  }, [open, role, onClose]);

  const handlePermissionChange = (permission) => {
    setSelectedPermissions((prev) =>
      prev.includes(permission)
        ? prev.filter((p) => p !== permission)
        : [...prev, permission]
    );
  };

  const handleSave = async () => {
    if (!role) return;
    setIsSaving(true);
    try {
      await roleApi.updateRolePermissions(role.id, { permissions: selectedPermissions });
      showSuccessToast('Cập nhật quyền thành công!');
      onClose();
    } catch (error) {
      handleApiError(error, 'Không thể cập nhật quyền.');
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="md">
      <DialogTitle>Quản lý Quyền cho Vai trò: {role?.name}</DialogTitle>
      <DialogContent>
        {isLoading ? (
          <Box sx={{ display: 'flex', justifyContent: 'center', p: 5 }}>
            <CircularProgress />
          </Box>
        ) : (
          <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 4 }}>
            {Object.entries(ALL_PERMISSIONS).map(([groupName, permissions]) => (
              <Box key={groupName} sx={{ minWidth: '200px' }}>
                <Typography variant="h6" gutterBottom>{groupName}</Typography>
                <FormGroup>
                  {permissions.map((permission) => (
                    <FormControlLabel
                      key={permission}
                      control={
                        <Checkbox
                          checked={selectedPermissions.includes(permission)}
                          onChange={() => handlePermissionChange(permission)}
                        />
                      }
                      label={getPermissionDisplayName(permission)}
                    />
                  ))}
                </FormGroup>
              </Box>
            ))}
          </Box>
        )}
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose} variant="outlined">Hủy</Button>
        <Button onClick={handleSave} variant="contained" disabled={isSaving}>
          {isSaving ? 'Đang lưu...' : 'Lưu thay đổi'}
        </Button>
      </DialogActions>
    </Dialog>
  );
}

export default RolePermissionsDialog; 