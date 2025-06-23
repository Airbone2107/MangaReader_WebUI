import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  Box,
  FormControl,
  InputLabel,
  Select,
  OutlinedInput,
  MenuItem,
  Checkbox,
  ListItemText,
  FormHelperText,
  Typography,
} from '@mui/material';
import { useEffect } from 'react';
import { useForm, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { updateUserRolesSchema } from '../../../schemas/userSchema';
import { showSuccessToast } from '../../../components/common/Notification';
import { handleApiError } from '../../../utils/errorUtils';
import userApi from '../../../api/userApi';
import useUserStore from '../../../stores/userStore';

/**
 * @typedef {import('../../../types/user').UserDto} UserDto
 * @typedef {import('../../../types/user').RoleDto} RoleDto
 */

const ITEM_HEIGHT = 48;
const ITEM_PADDING_TOP = 8;
const MenuProps = {
  PaperProps: {
    style: {
      maxHeight: ITEM_HEIGHT * 4.5 + ITEM_PADDING_TOP,
      width: 250,
    },
  },
};

/**
 * @param {object} props
 * @param {boolean} props.open
 * @param {() => void} props.onClose
 * @param {UserDto | null} props.user
 * @param {RoleDto[]} props.availableRoles
 */
function UserEditRolesDialog({ open, onClose, user, availableRoles }) {
  const fetchUsers = useUserStore((state) => state.fetchUsers);

  const {
    control,
    handleSubmit,
    formState: { errors, isSubmitting },
    reset,
  } = useForm({
    resolver: zodResolver(updateUserRolesSchema),
    defaultValues: {
      roles: [],
    },
  });

  useEffect(() => {
    if (user) {
      reset({ roles: user.roles || [] });
    }
  }, [user, reset]);

  const onSubmit = async (data) => {
    if (!user) return;
    try {
      await userApi.updateUserRoles(user.id, data);
      showSuccessToast('Cập nhật vai trò thành công!');
      fetchUsers(); // không cần reset pagination
      onClose();
    } catch (error) {
      handleApiError(error, 'Không thể cập nhật vai trò.');
    }
  };

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle>Chỉnh sửa vai trò cho {user?.userName}</DialogTitle>
      <Box component="form" onSubmit={handleSubmit(onSubmit)}>
        <DialogContent>
          <Typography variant="body1" gutterBottom>
            Email: {user?.email}
          </Typography>
          <FormControl fullWidth margin="dense" error={!!errors.roles}>
            <InputLabel id="edit-roles-select-label">Vai trò</InputLabel>
            <Controller
              name="roles"
              control={control}
              render={({ field }) => (
                <Select
                  labelId="edit-roles-select-label"
                  multiple
                  {...field}
                  input={<OutlinedInput label="Vai trò" />}
                  renderValue={(selected) => selected.join(', ')}
                  MenuProps={MenuProps}
                >
                  {availableRoles.map((role) => (
                    <MenuItem key={role.id} value={role.name}>
                      <Checkbox checked={field.value.indexOf(role.name) > -1} />
                      <ListItemText primary={role.name} />
                    </MenuItem>
                  ))}
                </Select>
              )}
            />
            {errors.roles && <FormHelperText>{errors.roles.message}</FormHelperText>}
          </FormControl>
        </DialogContent>
        <DialogActions>
          <Button onClick={onClose} variant="outlined">Hủy</Button>
          <Button type="submit" variant="contained" disabled={isSubmitting}>
            {isSubmitting ? 'Đang lưu...' : 'Lưu thay đổi'}
          </Button>
        </DialogActions>
      </Box>
    </Dialog>
  );
}

export default UserEditRolesDialog; 