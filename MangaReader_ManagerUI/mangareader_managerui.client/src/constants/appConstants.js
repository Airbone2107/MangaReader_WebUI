export const MANGA_STATUS_OPTIONS = [
  { value: 'Ongoing', label: 'Đang tiến hành' },
  { value: 'Completed', label: 'Hoàn thành' },
  { value: 'Hiatus', label: 'Tạm ngừng' },
  { value: 'Cancelled', label: 'Đã hủy' },
]

export const PUBLICATION_DEMOGRAPHIC_OPTIONS = [
  { value: 'Shounen', label: 'Shounen' },
  { value: 'Shoujo', label: 'Shoujo' },
  { value: 'Josei', label: 'Josei' },
  { value: 'Seinen', label: 'Seinen' },
  { value: 'None', label: 'Khác' },
]

export const CONTENT_RATING_OPTIONS = [
  { value: 'Safe', label: 'An Toàn' },
  { value: 'Suggestive', label: 'Gợi cảm' },
  { value: 'Erotica', label: 'R18' }, 
  { value: 'Pornographic', label: 'NSFW' }, 
]

export const MANGA_STAFF_ROLE_OPTIONS = [
  { value: 'Author', label: 'Tác giả' },
  { value: 'Artist', label: 'Họa sĩ' },
]

export const ORIGINAL_LANGUAGE_OPTIONS = [
  { value: 'ja', label: 'Tiếng Nhật' },
  { value: 'en', label: 'Tiếng Anh' },
  { value: 'vi', label: 'Tiếng Việt' },
  { value: 'ko', label: 'Tiếng Hàn' },
  { value: 'zh', label: 'Tiếng Trung' },
  { value: 'others', label: 'Ngôn ngữ khác' },
]

// Các loại mối quan hệ (type) trong API Response
export const RELATIONSHIP_TYPES = {
  AUTHOR: 'author',
  ARTIST: 'artist',
  TAG: 'tag',
  TAG_GROUP: 'tag_group',
  COVER_ART: 'cover_art',
  MANGA: 'manga',
  USER: 'user',
  CHAPTER: 'chapter',
  CHAPTER_PAGE: 'chapter_page',
  TRANSLATED_MANGA: 'translated_manga',
}

// Other constants
export const ROWS_PER_PAGE_OPTIONS = [10, 50, 100];
export const DEFAULT_PAGE_LIMIT = 10;
export const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || '/api'; // Use /api as proxy, or env variable if direct backend
export const CLOUDINARY_BASE_URL = 'https://res.cloudinary.com/dew5tpdko/image/upload/';

// Storage Prefixes for Zustand Persistence
export const SESSION_STORAGE_PREFIX = 'mrm_sess';
export const LOCAL_STORAGE_PREFIX = 'mrm_local';