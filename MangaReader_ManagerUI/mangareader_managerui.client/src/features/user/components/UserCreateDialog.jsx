import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  Box,
  TextField,
  FormControl,
  InputLabel,
  Select,
  OutlinedInput,
  MenuItem,
  Checkbox,
  ListItemText,
  FormHelperText,
} from '@mui/material';
import { useEffect } from 'react';
import { useForm, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { createUserSchema } from '../../../schemas/userSchema';
import { showSuccessToast } from '../../../components/common/Notification';
import { handleApiError } from '../../../utils/errorUtils';
import userApi from '../../../api/userApi';
import useUserStore from '../../../stores/userStore';

/**
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
 * @param {RoleDto[]} props.availableRoles
 */
function UserCreateDialog({ open, onClose, availableRoles }) {
  const fetchUsers = useUserStore((state) => state.fetchUsers);

  const {
    control,
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
    reset,
  } = useForm({
    resolver: zodResolver(createUserSchema),
    defaultValues: {
      userName: '',
      email: '',
      password: '',
      roles: [],
    },
  });

  useEffect(() => {
    if (open) {
      reset();
    }
  }, [open, reset]);

  const onSubmit = async (data) => {
    try {
      await userApi.createUser(data);
      showSuccessToast('Tạo người dùng thành công!');
      fetchUsers(true);
      onClose();
    } catch (error) {
      handleApiError(error, 'Không thể tạo người dùng.');
    }
  };

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle>Tạo Người Dùng Mới</DialogTitle>
      <Box component="form" onSubmit={handleSubmit(onSubmit)}>
        <DialogContent>
          <TextField
            autoFocus
            margin="dense"
            label="Tên đăng nhập"
            fullWidth
            variant="outlined"
            {...register('userName')}
            error={!!errors.userName}
            helperText={errors.userName?.message}
          />
          <TextField
            margin="dense"
            label="Email"
            type="email"
            fullWidth
            variant="outlined"
            {...register('email')}
            error={!!errors.email}
            helperText={errors.email?.message}
          />
          <TextField
            margin="dense"
            label="Mật khẩu"
            type="password"
            fullWidth
            variant="outlined"
            {...register('password')}
            error={!!errors.password}
            helperText={errors.password?.message}
          />
          <FormControl fullWidth margin="dense" error={!!errors.roles}>
            <InputLabel id="roles-select-label">Vai trò</InputLabel>
            <Controller
              name="roles"
              control={control}
              render={({ field }) => (
                <Select
                  labelId="roles-select-label"
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
            {isSubmitting ? 'Đang tạo...' : 'Tạo'}
          </Button>
        </DialogActions>
      </Box>
    </Dialog>
  );
}

export default UserCreateDialog; 