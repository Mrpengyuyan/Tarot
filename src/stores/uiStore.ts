import { create } from 'zustand';

interface UiState {
    theme: 'dark' | 'light';
    sidebarOpen: boolean;
    notifications: Array<{
      id: string;
      type: 'success' | 'error' | 'warning' | 'info';
      message: string;
      timestamp: number;
    }>;

    // Actions
    setTheme: (theme: 'dark' | 'light') => void;
    toggleSidebar: () => void;
    setSidebarOpen: (open: boolean) => void;
    addNotification: (type: 'success' | 'error' | 'warning' | 'info', message: string) => void;
    removeNotification: (id: string) => void;
    clearNotifications: () => void;
}

export const useUiStore = create<UiState>((set, get) => ({
    theme: 'dark',
    sidebarOpen: false,
    notifications: [],

    setTheme: (theme) => set({ theme }),

    toggleSidebar: () => set((state) => ({ sidebarOpen: !state.sidebarOpen })),

    setSidebarOpen: (open) => set({ sidebarOpen: open }),

    addNotification: (type, message) => {
      const id = Date.now().toString();
      const notification = {
        id,
        type,
        message,
        timestamp: Date.now(),
      };

      set((state) => ({
        notifications: [...state.notifications, notification],
      }));

      // 3秒后自动移除通知
      setTimeout(() => {
        get().removeNotification(id);
      }, 3000);
    },

    removeNotification: (id) => set((state) => ({
      notifications: state.notifications.filter(n => n.id !== id),
    })),

    clearNotifications: () => set({ notifications: [] }),
}));