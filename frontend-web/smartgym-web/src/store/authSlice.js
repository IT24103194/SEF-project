import { createSlice } from '@reduxjs/toolkit';

export const normalizeRole = (user) => {
  if (!user) return null;
  // If user already has a string role
  if (typeof user.role === 'string' && user.role.trim()) {
    const r = user.role.trim().toUpperCase();
    if (r === 'ADMIN' || r === 'FACILITYMANAGER' || r === 'FACILITY_MANAGER') return 'ADMIN';
    if (r === 'TRAINER') return 'TRAINER';
    return r;
  }
  // If user has a roles array
  if (Array.isArray(user.roles) && user.roles.length > 0) {
    const upperRoles = user.roles.map((r) => String(r).toUpperCase());
    if (upperRoles.some((r) => r === 'ADMIN' || r === 'FACILITYMANAGER' || r === 'FACILITY_MANAGER')) {
      return 'ADMIN';
    }
    if (upperRoles.some((r) => r === 'TRAINER')) {
      return 'TRAINER';
    }
    return upperRoles[0] || 'MEMBER';
  }
  return 'MEMBER';
};

const rawToken = localStorage.getItem('smartgym_token');
const initialToken = rawToken && rawToken !== 'undefined' && rawToken !== 'null' ? rawToken : null;

let initialUser = null;
try {
  const rawUser = localStorage.getItem('smartgym_user');
  if (rawUser && rawUser !== 'undefined' && rawUser !== 'null') {
    initialUser = JSON.parse(rawUser);
  }
} catch (e) {
  initialUser = null;
}

const initialRole = normalizeRole(initialUser);

const authSlice = createSlice({
  name: 'auth',
  initialState: {
    token: initialToken,
    user: initialUser ? { ...initialUser, role: initialRole } : null,
    isAuthenticated: !!initialToken && !!initialUser,
    role: initialRole,
    loading: false,
    error: null,
  },
  reducers: {
    loginSuccess: (state, action) => {
      const payload = action.payload || {};
      const token = payload.accessToken || payload.token;
      const user = payload.user || null;
      const role = normalizeRole(user);

      const userWithRole = user ? { ...user, role } : null;

      state.token = token;
      state.user = userWithRole;
      state.role = role;
      state.isAuthenticated = !!token;
      state.error = null;

      if (token) {
        localStorage.setItem('smartgym_token', token);
      }
      if (payload.refreshToken) {
        localStorage.setItem('smartgym_refresh_token', payload.refreshToken);
      }
      if (userWithRole) {
        localStorage.setItem('smartgym_user', JSON.stringify(userWithRole));
      }
    },
    logout: (state) => {
      state.token = null;
      state.user = null;
      state.role = null;
      state.isAuthenticated = false;
      state.error = null;
      localStorage.removeItem('smartgym_token');
      localStorage.removeItem('smartgym_refresh_token');
      localStorage.removeItem('smartgym_user');
    },
    setAuthError: (state, action) => {
      state.error = action.payload;
    },
  },
});

export const { loginSuccess, logout, setAuthError } = authSlice.actions;
export default authSlice.reducer;

