import axios from 'axios';
import { API_BASE_URL } from '../constants/appConstants';
import useUiStore from '../stores/uiStore';
import { handleApiError } from '../utils/errorUtils';

const apiClient = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
})

// Request interceptor: Hiển thị loading spinner
apiClient.interceptors.request.use(
  (config) => {
    useUiStore.getState().setLoading(true)
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
    handleApiError(error)
    return Promise.reject(error)
  },
)

export default apiClient 