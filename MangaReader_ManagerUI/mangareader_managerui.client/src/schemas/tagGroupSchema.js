import { z } from 'zod'

export const tagGroupSchema = z.object({
  name: z
    .string()
    .min(1, 'Tên nhóm tag không được để trống')
    .max(100, 'Tên nhóm tag quá dài (tối đa 100 ký tự)'),
})

export const createTagGroupSchema = tagGroupSchema

export const updateTagGroupSchema = tagGroupSchema 