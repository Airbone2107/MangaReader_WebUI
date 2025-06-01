import { persist } from 'zustand/middleware';
import { SESSION_STORAGE_PREFIX, LOCAL_STORAGE_PREFIX } from '../constants/appConstants';

/**
 * Creates a Zustand persist middleware configuration for filter data (sessionStorage).
 * @template TState
 * @param {string} name - Unique name for the store's filter persistence.
 * @returns {import('zustand/middleware').PersistOptions<TState>}
 */
export const createSessionStoragePersist = (name) => ({
  name: `${SESSION_STORAGE_PREFIX}_${name}_filters`,
  getStorage: () => sessionStorage,
  partialize: (state) => ({ filters: state.filters }), // Only persist 'filters' part of the state
  version: 0, // Increase this number to clear stored data when schema changes
});

/**
 * Creates a Zustand persist middleware configuration for pagination and sort data (localStorage).
 * @template TState
 * @param {string} name - Unique name for the store's pagination/sort persistence.
 * @returns {import('zustand/middleware').PersistOptions<TState>}
 */
export const createLocalStoragePersist = (name) => ({
  name: `${LOCAL_STORAGE_PREFIX}_${name}_settings`,
  getStorage: () => localStorage,
  partialize: (state) => ({ page: state.page, rowsPerPage: state.rowsPerPage, sort: state.sort }),
  version: 0, // Increase this number to clear stored data when schema changes
});

/**
 * Combines sessionStorage and localStorage persistence for a Zustand store.
 * Filters go to sessionStorage, pagination/sort go to localStorage.
 * @template TState
 * @param {import('zustand').StateCreator<TState>} config - The Zustand store configuration.
 * @param {string} name - Base name for the store's persistence keys.
 * @returns {import('zustand').StateCreator<TState>}
 */
export const persistStore = (config, name) => {
  return persist(
    persist(
      config,
      createSessionStoragePersist(name)
    ),
    createLocalStoragePersist(name)
  );
}; 