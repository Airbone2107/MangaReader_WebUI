import { z } from 'zod';
import { ORIGINAL_LANGUAGE_OPTIONS } from '../constants/appConstants';

// Trích xuất chỉ các giá trị 'value' (languageKey) từ ORIGINAL_LANGUAGE_OPTIONS
const languageKeys = ORIGINAL_LANGUAGE_OPTIONS.map((opt) => opt.value);

export const createTranslatedMangaSchema = z.object({
  mangaId: z.string().uuid('ID manga không hợp lệ'),
  languageKey: z
    .enum(languageKeys, {
      errorMap: () => ({ message: 'Ngôn ngữ không hợp lệ' }),
    }),
  title: z
    .string()
    .min(1, 'Tiêu đề không được để trống')
    .max(500, 'Tiêu đề quá dài (tối đa 500 ký tự)'),
  description: z.string().max(4000, 'Mô tả quá dài (tối đa 4000 ký tự)').optional().nullable(),
})

export const updateTranslatedMangaSchema = z.object({
  languageKey: z
    .enum(languageKeys, {
      errorMap: () => ({ message: 'Ngôn ngữ không hợp lệ' }),
    }), // Không cần .min() ở đây vì z.enum đã đảm bảo giá trị hợp lệ từ danh sách
  title: z
    .string()
    .min(1, 'Tiêu đề không được để trống')
    .max(500, 'Tiêu đề quá dài (tối đa 500 ký tự)'),
  description: z.string().max(4000, 'Mô tả quá dài (tối đa 4000 ký tự)').optional().nullable(),
})