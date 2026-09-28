import { configureStore } from '@reduxjs/toolkit';
import authReducer from './authSlice';
import systemReducer from './systemSlice';

export const store = configureStore({
  reducer: {
    auth: authReducer,
    system: systemReducer,
  },
});

export default store;
