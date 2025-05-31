import { z } from 'zod';

// Helper schema for date strings that are required and must be valid dates
const requiredDateStringSchema = z
  .string()
  .min(1, 'Trường này là bắt buộc.') // Áp dụng .min(1) TRƯỚC khi refine
  .refine(
    (str) => {
      // Nếu chuỗi rỗng sau khi .min(1) không bắt được (ví dụ: do giá trị ban đầu là null/undefined)
      // thì refined sẽ xử lý. Tuy nhiên, .min(1) ở trên đã đảm bảo chuỗi không rỗng.
      // Refine này chỉ tập trung vào kiểm tra định dạng ngày.
      const date = new Date(str);
      return !isNaN(date.getTime());
    },
    {
      message: 'Ngày không hợp lệ. Vui lòng nhập định dạng ngày/giờ hợp lệ (ví dụ: 2023-10-27T10:00).',
    },
  );

export const createChapterSchema = z.object({
  translatedMangaId: z.string().uuid('ID bản dịch manga không hợp lệ'),
  volume: z.string().max(50, 'Volume quá dài (tối đa 50 ký tự)').optional().nullable(),
  chapterNumber: z.string().max(50, 'Số chương quá dài (tối đa 50 ký tự)').optional().nullable(),
  title: z.string().max(255, 'Tiêu đề chương quá dài (tối đa 255 ký tự)').optional().nullable(),
  publishAt: requiredDateStringSchema,
  readableAt: requiredDateStringSchema,
  // uploadedByUserId sẽ được xử lý ở API client/backend, không cần trong schema form frontend
});

export const updateChapterSchema = z.object({
  volume: z.string().max(50, 'Volume quá dài (tối đa 50 ký tự)').optional().nullable(),
  chapterNumber: z.string().max(50, 'Số chương quá dài (tối đa 50 ký tự)').optional().nullable(),
  title: z.string().max(255, 'Tiêu đề chương quá dài (tối đa 255 ký tự)').optional().nullable(),
  publishAt: requiredDateStringSchema,
  readableAt: requiredDateStringSchema,
});

export const createChapterPageEntrySchema = z.object({
  pageNumber: z.number().int('Số trang phải là số nguyên').min(1, 'Số trang phải lớn hơn 0'),
});

export const updateChapterPageDetailsSchema = z.object({
  pageNumber: z.number().int('Số trang phải là số nguyên').min(1, 'Số trang phải lớn hơn 0'),
});

export const uploadChapterPageImageSchema = z.object({
  file: z
    .any() // Sử dụng z.any() cho input type="file" vì nó trả về FileList
    .refine((file) => file instanceof FileList && file.length > 0, 'Ảnh trang là bắt buộc.')
    .refine((file) => file[0]?.size <= 5 * 1024 * 1024, `Kích thước file tối đa là 5MB.`)
    .refine(
      (file) =>
        ['image/jpeg', 'image/jpg', 'image/png', 'image/webp'].includes(file[0]?.type),
      'Chỉ hỗ trợ định dạng .jpg, .jpeg, .png, .webp.',
    ),
}); 