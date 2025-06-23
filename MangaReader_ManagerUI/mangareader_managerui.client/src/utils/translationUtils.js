import { ORIGINAL_LANGUAGE_OPTIONS, CONTENT_RATING_OPTIONS } from '../constants/appConstants';

/**
 * Dịch mã ngôn ngữ sang tên đầy đủ.
 * @param {string} code - Mã ngôn ngữ (ví dụ: 'ja', 'en').
 * @returns {string} Tên ngôn ngữ đầy đủ hoặc mã gốc nếu không tìm thấy.
 */
export const translateLanguageCode = (code) => {
  if (!code) return 'Không xác định';
  const languageOption = ORIGINAL_LANGUAGE_OPTIONS.find(option => option.value === code.toLowerCase());
  return languageOption ? languageOption.label : code;
};

/**
 * Dịch giá trị content rating sang tên hiển thị mới.
 * @param {string} ratingValue - Giá trị content rating từ API (ví dụ: 'Safe', 'Erotica').
 * @returns {string} Tên hiển thị tương ứng hoặc giá trị gốc nếu không tìm thấy.
 */
export const translateContentRating = (ratingValue) => {
  if (!ratingValue) return 'Không xác định';
  const ratingOption = CONTENT_RATING_OPTIONS.find(option => option.value === ratingValue);
  return ratingOption ? ratingOption.label : ratingValue;
}; 