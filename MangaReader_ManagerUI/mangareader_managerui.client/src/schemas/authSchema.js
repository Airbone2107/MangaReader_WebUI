import { z } from 'zod';

export const loginSchema = z.object({
  username: z.string().min(1, 'Tên đăng nhập không được để trống.'),
  password: z.string().min(1, 'Mật khẩu không được để trống.'),
}); 