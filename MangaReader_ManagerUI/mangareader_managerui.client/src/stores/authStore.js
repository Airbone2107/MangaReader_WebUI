import { create } from 'zustand';
import { createJSONStorage, persist } from 'zustand/middleware';
import { jwtDecode } from 'jwt-decode'; 
import { LOCAL_STORAGE_PREFIX } from '../constants/appConstants';
import authApi from '../api/authApi';
import { showErrorToast, showSuccessToast } from '../components/common/Notification';

const useAuthStore = create(
  persist(
    (set, get) => ({
      user: null, // Sẽ chứa thông tin đã giải mã từ token như { nameid, name, email, role, ... }
      accessToken: null,
      refreshToken: null,
      isAuthenticated: false,

      login: async (username, password) => {
        try {
          const response = await authApi.login(username, password);
          if (response && response.isSuccess && response.accessToken) {
            const decodedToken = jwtDecode(response.accessToken);
            set({
              user: decodedToken,
              accessToken: response.accessToken,
              refreshToken: response.refreshToken,
              isAuthenticated: true,
            });
            showSuccessToast(response.message || 'Đăng nhập thành công!');
            return true;
          } else {
            showErrorToast(response.message || 'Tên đăng nhập hoặc mật khẩu không đúng.');
            return false;
          }
        } catch (error) {
          // Lỗi đã được xử lý bởi interceptor của apiClient
          console.error('Login failed:', error);
          return false;
        }
      },

      logout: () => {
        set({
          user: null,
          accessToken: null,
          refreshToken: null,
          isAuthenticated: false,
        });
        // Không cần gọi API /revoke ở đây, chỉ cần xóa token phía client
        // Việc revoke có thể là một tính năng nâng cao hơn (ví dụ: logout all devices)
      },
    }),
    {
      name: `${LOCAL_STORAGE_PREFIX}_auth`,
      storage: createJSONStorage(() => localStorage),
    }
  )
);

export default useAuthStore; 