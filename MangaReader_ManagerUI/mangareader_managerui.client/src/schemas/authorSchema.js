import { z } from 'zod'

export const authorSchema = z.object({
  name: z
    .string()
    .min(1, 'Tên tác giả không được để trống')
    .max(255, 'Tên tác giả quá dài (tối đa 255 ký tự)'),
  biography: z.string().max(2000, 'Tiểu sử quá dài (tối đa 2000 ký tự)').optional().nullable(),
})

export const createAuthorSchema = authorSchema

export const updateAuthorSchema = authorSchema 