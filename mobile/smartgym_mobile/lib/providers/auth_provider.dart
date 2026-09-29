import 'dart:convert';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../models/user_model.dart';
import '../services/api_service.dart';
import '../services/secure_storage_service.dart';

class AuthState {
  final UserModel? user;
  final bool isLoading;
  final String? errorMessage;
  final String? successMessage;

  const AuthState({
    this.user,
    this.isLoading = false,
    this.errorMessage,
    this.successMessage,
  });

  bool get isAuthenticated => user != null;

  AuthState copyWith({
    UserModel? user,
    bool? isLoading,
    String? errorMessage,
    String? successMessage,
    bool clearUser = false,
  }) {
    return AuthState(
      user: clearUser ? null : (user ?? this.user),
      isLoading: isLoading ?? this.isLoading,
      errorMessage: errorMessage,
      successMessage: successMessage,
    );
  }
}

class AuthNotifier extends StateNotifier<AuthState> {
  final ApiService _apiService;
  final SecureStorageService _storage;

  AuthNotifier({
    required ApiService apiService,
    required SecureStorageService storage,
  })  : _apiService = apiService,
        _storage = storage,
        super(const AuthState()) {
    _loadPersistedUser();
  }

  Future<void> _loadPersistedUser() async {
    final userData = await _storage.getUserData();
    final token = await _storage.getToken();
    if (userData != null && token != null) {
      try {
        final map = jsonDecode(userData);
        state = state.copyWith(user: UserModel.fromJson(map, token: token));
      } catch (_) {
        await _storage.clearAuth();
      }
    }
  }

  Future<bool> login(String email, String password) async {
    state = state.copyWith(isLoading: true, errorMessage: null, successMessage: null);
    try {
      final response = await _apiService.post('/auth/login', {
        'email': email,
        'password': password,
      });

      if (response.statusCode == 200) {
        final data = jsonDecode(response.body);
        final token = data['accessToken'] ?? data['token'];
        final userMap = data['user'] ?? data;
        final user = UserModel.fromJson(userMap, token: token);
        await _storage.saveToken(token);
        await _storage.saveUserData(jsonEncode(user.toJson()));
        state = state.copyWith(user: user, isLoading: false);
        return true;
      } else {
        String msg = 'Login failed. Please check credentials.';
        try {
          final error = jsonDecode(response.body);
          msg = error['detail'] ?? error['message'] ?? error['title'] ?? msg;
        } catch (_) {}
        state = state.copyWith(
          isLoading: false,
          errorMessage: msg,
        );
        return false;
      }
    } catch (e) {
      state = state.copyWith(
        isLoading: false,
        errorMessage: 'Network error: Unable to connect to server ($e).',
      );
      return false;
    }
  }

  Future<bool> register({
    required String email,
    required String password,
    required String firstName,
    required String lastName,
    String? phoneNumber,
    String role = 'Member',
  }) async {
    state = state.copyWith(isLoading: true, errorMessage: null, successMessage: null);
    try {
      final response = await _apiService.post('/auth/register', {
        'email': email,
        'password': password,
        'firstName': firstName,
        'lastName': lastName,
        if (phoneNumber != null && phoneNumber.isNotEmpty) 'phoneNumber': phoneNumber,
        'role': role,
      });

      if (response.statusCode == 200 || response.statusCode == 201) {
        final data = jsonDecode(response.body);
        final token = data['accessToken'] ?? data['token'];
        final userMap = data['user'] ?? data;
        final user = UserModel.fromJson(userMap, token: token);
        if (token != null) {
          await _storage.saveToken(token);
          await _storage.saveUserData(jsonEncode(user.toJson()));
        }
        state = state.copyWith(
          user: user,
          isLoading: false,
          successMessage: 'Registration successful! Welcome to SmartGym.',
        );
        return true;
      } else {
        String msg = 'Registration failed.';
        try {
          final error = jsonDecode(response.body);
          if (error['errors'] != null && error['errors'] is Map) {
            final firstErrorList = (error['errors'] as Map).values.first;
            if (firstErrorList is List && firstErrorList.isNotEmpty) {
              msg = firstErrorList.first.toString();
            }
          } else {
            msg = error['detail'] ?? error['message'] ?? error['title'] ?? msg;
          }
        } catch (_) {}
        state = state.copyWith(
          isLoading: false,
          errorMessage: msg,
        );
        return false;
      }
    } catch (e) {
      state = state.copyWith(
        isLoading: false,
        errorMessage: 'Network error: Unable to connect to server ($e).',
      );
      return false;
    }
  }

  Future<void> fetchProfile() async {
    try {
      final response = await _apiService.get('/auth/me');
      if (response.statusCode == 200) {
        final data = jsonDecode(response.body);
        final userMap = data['user'] ?? data;
        final token = await _storage.getToken();
        final updatedUser = UserModel.fromJson(userMap, token: token);
        await _storage.saveUserData(jsonEncode(updatedUser.toJson()));
        state = state.copyWith(user: updatedUser);
      }
    } catch (_) {}
  }

  Future<void> logout() async {
    try {
      await _apiService.post('/auth/logout', {});
    } catch (_) {}
    await _storage.clearAuth();
    state = const AuthState();
  }
}

final storageProvider = Provider<SecureStorageService>((ref) {
  return SecureStorageService();
});

final apiServiceProvider = Provider<ApiService>((ref) {
  final storage = ref.watch(storageProvider);
  return ApiService(storage: storage);
});

final authProvider = StateNotifierProvider<AuthNotifier, AuthState>((ref) {
  final api = ref.watch(apiServiceProvider);
  final storage = ref.watch(storageProvider);
  return AuthNotifier(apiService: api, storage: storage);
});
