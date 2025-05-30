import { z } from 'zod'
import {
  MANGA_STATUS_OPTIONS,
  PUBLICATION_DEMOGRAPHIC_OPTIONS,
  CONTENT_RATING_OPTIONS,
  MANGA_STAFF_ROLE_OPTIONS,
} from '../constants/appConstants'

const commonMangaFields = {
  title: z
    .string()
    .min(1, 'Tiêu đề không được để trống')
    .max(500, 'Tiêu đề quá dài (tối đa 500 ký tự)'),
  originalLanguage: z
    .string()
    .min(1, 'Ngôn ngữ gốc không được để trống')
    .max(10, 'Ngôn ngữ gốc quá dài (tối đa 10 ký tự)'),
  publicationDemographic: z
    .enum(PUBLICATION_DEMOGRAPHIC_OPTIONS.map((opt) => opt.value))
    .optional()
    .nullable(),
  status: z.enum(MANGA_STATUS_OPTIONS.map((opt) => opt.value)),
  year: z
    .number()
    .int('Năm phải là số nguyên')
    .min(1000, 'Năm không hợp lệ')
    .max(new Date().getFullYear(), 'Năm không thể lớn hơn năm hiện tại')
    .optional()
    .nullable(),
  contentRating: z.enum(CONTENT_RATING_OPTIONS.map((opt) => opt.value)),
}

export const mangaAuthorSchema = z.object({
  authorId: z.string().uuid('ID tác giả không hợp lệ'),
  role: z.enum(MANGA_STAFF_ROLE_OPTIONS.map((opt) => opt.value)),
})

export const createMangaSchema = z.object({
  ...commonMangaFields,
  tagIds: z.array(z.string().uuid('ID tag không hợp lệ')).optional(),
  authors: z.array(mangaAuthorSchema).optional(),
})

export const updateMangaSchema = z.object({
  ...commonMangaFields,
  isLocked: z.boolean(),
  tagIds: z.array(z.string().uuid('ID tag không hợp lệ')).optional(),
  authors: z.array(mangaAuthorSchema).optional(),
})

export const uploadCoverArtSchema = z.object({
  file: z
    .any()
    .refine((file) => file !== undefined, 'Ảnh bìa là bắt buộc.')
    .refine((file) => file?.size <= 5 * 1024 * 1024, `Kích thước file tối đa là 5MB.`)
    .refine(
      (file) =>
        ['image/jpeg', 'image/jpg', 'image/png', 'image/webp'].includes(file?.type),
      'Chỉ hỗ trợ định dạng .jpg, .jpeg, .png, .webp.',
    ),
  volume: z.string().max(50, 'Volume quá dài (tối đa 50 ký tự)').optional().nullable(),
  description: z.string().max(512, 'Mô tả quá dài (tối đa 512 ký tự)').optional().nullable(),
}); 