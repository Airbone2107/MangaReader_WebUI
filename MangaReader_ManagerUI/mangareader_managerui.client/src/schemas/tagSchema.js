import { z } from 'zod'

export const tagSchema = z.object({
  name: z
    .string()
    .min(1, 'Tên tag không được để trống')
    .max(100, 'Tên tag quá dài (tối đa 100 ký tự)'),
  tagGroupId: z.string().uuid('ID nhóm tag không hợp lệ'),
})

export const createTagSchema = tagSchema

export const updateTagSchema = tagSchema 