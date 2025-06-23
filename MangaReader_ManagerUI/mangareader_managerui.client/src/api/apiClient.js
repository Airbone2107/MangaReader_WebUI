import axios from 'axios';
import { API_BASE_URL } from '../constants/appConstants';
import useUiStore from '../stores/uiStore';
import useAuthStore from '../stores/authStore';
import { handleApiError } from '../utils/errorUtils';

const apiClient = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
})

// Request interceptor: Gắn token và hiển thị loading spinner
apiClient.interceptors.request.use(
  (config) => {
    useUiStore.getState().setLoading(true)
    const token = useAuthStore.getState().accessToken;
    if (token) {
      config.headers['Authorization'] = `Bearer ${token}`;
    }
    return config
  },
  (error) => {
    useUiStore.getState().setLoading(false)
    handleApiError(error, 'Lỗi request không xác định.')
    return Promise.reject(error)
  },
)

// Response interceptor: Ẩn loading spinner và xử lý lỗi
apiClient.interceptors.response.use(
  (response) => {
    useUiStore.getState().setLoading(false)
    return response
  },
  (error) => {
    useUiStore.getState().setLoading(false)
    
    // Nếu lỗi là 401 Unauthorized, thực hiện logout
    if (error.response && error.response.status === 401) {
      useAuthStore.getState().logout();
      // Chuyển hướng về trang login, có thể thực hiện ở đây hoặc trong component
      // window.location.href = '/login'; 
    }
    
    handleApiError(error)
    return Promise.reject(error)
  },
)

export default apiClient 