import { toast } from 'react-toastify';

export const showSuccessToast = (message) => {
  toast.success(message, {
    position: "top-right",
    autoClose: 3000,
    hideProgressBar: false,
    closeOnClick: true,
    pauseOnHover: true,
    draggable: true,
    progress: undefined,
  });
};

export const showErrorToast = (message) => {
  toast.error(message, {
    position: "top-right",
    autoClose: 5000,
    hideProgressBar: false,
    closeOnClick: true,
    pauseOnHover: true,
    draggable: true,
    progress: undefined,
  });
};

export const showWarningToast = (message) => {
  toast.warning(message, {
    position: "top-right",
    autoClose: 4000,
    hideProgressBar: false,
    closeOnClick: true,
    pauseOnHover: true,
    draggable: true,
    progress: undefined,
  });
};

export const showInfoToast = (message) => {
  toast.info(message, {
    position: "top-right",
    autoClose: 3000,
    hideProgressBar: false,
    closeOnClick: true,
    pauseOnHover: true,
    draggable: true,
    progress: undefined,
  });
};

// You can also create a generic one for API errors
export const handleApiError = (error, defaultMessage = "Đã có lỗi xảy ra!") => {
  if (error && error.response && error.response.data && error.response.data.errors) {
    const errorMessages = error.response.data.errors.map(err => err.detail || err.title).join('\n');
    showErrorToast(errorMessages || defaultMessage);
  } else if (error && error.message) {
    showErrorToast(error.message);
  } else {
    showErrorToast(defaultMessage);
  }
}; 