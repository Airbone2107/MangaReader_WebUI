import apiClient from './apiClient';

const authApi = {
  login: async (username, password) => {
    const response = await apiClient.post('/auth/login', { username, password });
    return response.data;
  },
};

export default authApi; 