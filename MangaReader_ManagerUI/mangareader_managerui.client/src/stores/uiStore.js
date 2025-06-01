import { create } from 'zustand'

const useUiStore = create((set) => ({
  isLoading: false,
  isSidebarOpen: true, // Initial state for sidebar
  
  setLoading: (loading) => set({ isLoading: loading }),
  toggleSidebar: () => set((state) => ({ isSidebarOpen: !state.isSidebarOpen })),
  setSidebarOpen: (open) => set({ isSidebarOpen: open }),
}))

export default useUiStore 