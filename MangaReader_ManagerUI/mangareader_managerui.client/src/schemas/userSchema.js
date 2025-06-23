import { z } from 'zod';

export const createUserSchema = z.object({
  userName: z.string().min(3, 'Tên đăng nhập phải có ít nhất 3 ký tự.'),
  email: z.string().email('Địa chỉ email không hợp lệ.'),
  password: z.string().min(6, 'Mật khẩu phải có ít nhất 6 ký tự.'),
  roles: z.array(z.string()).min(1, 'Phải chọn ít nhất một vai trò.'),
});

export const updateUserRolesSchema = z.object({
  roles: z.array(z.string()).min(1, 'Phải chọn ít nhất một vai trò.'),
}); 