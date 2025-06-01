import { showErrorToast } from '../components/common/Notification'

export const handleApiError = (error, defaultMessage = 'Đã có lỗi xảy ra!') => {
  console.error('API Error:', error)

  if (error && error.response && error.response.data && error.response.data.errors) {
    // API returns a list of errors
    const errorMessages = error.response.data.errors.map(err => err.detail || err.title).join('\n')
    showErrorToast(errorMessages || defaultMessage)
  } else if (error && error.response && error.response.data && error.response.data.title) {
    // API returns a single error object (non-standard in our DTOs, but good for robustness)
    showErrorToast(error.response.data.detail || error.response.data.title)
  } else if (error && error.message) {
    // Generic JS error message
    showErrorToast(error.message)
  } else {
    showErrorToast(defaultMessage)
  }
}

export const getValidationErrors = (error) => {
  const validationErrors = {};
  if (error && error.response && error.response.data && error.response.data.errors) {
    error.response.data.errors.forEach(err => {
      if (err.context && err.context.field) {
        const fieldName = err.context.field.charAt(0).toLowerCase() + err.context.field.slice(1); // Convert to camelCase
        validationErrors[fieldName] = err.detail || err.title;
      }
    });
  }
  return validationErrors;
}; 