import { showErrorToast } from '../components/common/Notification'

export const handleApiError = (error, defaultMessage = 'Đã có lỗi xảy ra!') => {
  console.error('API Error:', error)

  if (error && error.response && error.response.data) {
    // Kiểm tra nếu error.response.data.errors tồn tại VÀ là một mảng
    if (Array.isArray(error.response.data.errors)) {
      const errorMessages = error.response.data.errors.map(err => err.detail || err.title).join('\n')
      showErrorToast(errorMessages || defaultMessage)
    } else if (error.response.data.title) { // Fallback cho đối tượng lỗi đơn lẻ (ví dụ: error.response.data là một ApiError trực tiếp)
      showErrorToast(error.response.data.detail || error.response.data.title)
    } else if (typeof error.response.data === 'string') { // Nếu nó là một chuỗi lỗi thuần túy
      showErrorToast(error.response.data)
    } else {
      showErrorToast(defaultMessage)
    }
  } else if (error && error.message) {
    // Thông điệp lỗi JS chung (ví dụ: lỗi mạng, CORS)
    showErrorToast(error.message)
  } else {
    showErrorToast(defaultMessage)
  }
}

export const getValidationErrors = (error) => {
  const validationErrors = {};
  if (error && error.response && error.response.data && Array.isArray(error.response.data.errors)) { // Thêm Array.isArray check ở đây
    error.response.data.errors.forEach(err => {
      // Chuyển đổi tên trường từ PascalCase sang camelCase nếu có context field
      if (err.context && typeof err.context === 'object' && err.context.field) {
        const fieldName = String(err.context.field).charAt(0).toLowerCase() + String(err.context.field).slice(1);
        validationErrors[fieldName] = err.detail || err.title;
      }
    });
  }
  return validationErrors;
}; 