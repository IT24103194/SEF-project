import { createSlice } from '@reduxjs/toolkit';

const initialToken = localStorage.getItem('smartgym_token');
const initialUser = localStorage.getItem('smartgym_user')
  ? JSON.parse(localStorage.getItem('smartgym_user'))
  : null;

const authSlice = createSlice({
  name: 'auth',
  initialState: {
    token: initialToken,
    user: initialUser,
    isAuthenticated: !!initialToken,
    role: initialUser ? initialUser.role : null,
    loading: false,
    error: null,
  },
  reducers: {
    loginSuccess: (state, action) => {
      state.token = action.payload.token;
      state.user = action.payload.user;
      state.role = action.payload.user.role;
      state.isAuthenticated = true;
      state.error = null;
      localStorage.setItem('smartgym_token', action.payload.token);
      localStorage.setItem('smartgym_user', JSON.stringify(action.payload.user));
    },
    logout: (state) => {
      state.token = null;
      state.user = null;
      state.role = null;
      state.isAuthenticated = false;
      state.error = null;
      localStorage.removeItem('smartgym_token');
      localStorage.removeItem('smartgym_user');
    },
    setAuthError: (state, action) => {
      state.error = action.payload;
    },
  },
});

export const { loginSuccess, logout, setAuthError } = authSlice.actions;
export default authSlice.reducer;
