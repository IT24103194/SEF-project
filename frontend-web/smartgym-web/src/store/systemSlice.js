import { createSlice, createAsyncThunk } from '@reduxjs/toolkit';
import apiClient from '../services/apiClient';

export const fetchSystemInfo = createAsyncThunk(
  'system/fetchInfo',
  async (_, { rejectWithValue }) => {
    try {
      const response = await apiClient.get('/api/system/info');
      return response.data;
    } catch (err) {
      return rejectWithValue(err.message || 'Failed to connect to SmartGym API');
    }
  }
);

const systemSlice = createSlice({
  name: 'system',
  initialState: {
    info: null,
    status: 'idle',
    error: null,
  },
  reducers: {},
  extraReducers: (builder) => {
    builder
      .addCase(fetchSystemInfo.pending, (state) => {
        state.status = 'loading';
      })
      .addCase(fetchSystemInfo.fulfilled, (state, action) => {
        state.status = 'succeeded';
        state.info = action.payload;
      })
      .addCase(fetchSystemInfo.rejected, (state, action) => {
        state.status = 'failed';
        state.error = action.payload;
      });
  },
});

export default systemSlice.reducer;
