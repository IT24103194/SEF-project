import 'dart:convert';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../models/user_model.dart';
import '../services/api_service.dart';
import '../services/secure_storage_service.dart';

class AuthState {
  final UserModel? user;
  final bool isLoading;
  final String? errorMessage;

  const AuthState({
    this.user,
    this.isLoading = false,
    this.errorMessage,
  });

  bool get isAuthenticated => user != null;

  AuthState copyWith({
    UserModel? user,
    bool? isLoading,
    String? errorMessage,
  }) {
    return AuthState(
      user: user ?? this.user,
      isLoading: isLoading ?? this.isLoading,
      errorMessage: errorMessage,
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
    state = state.copyWith(isLoading: true, errorMessage: null);
    try {
      final response = await _apiService.post('/auth/login', {
        'email': email,
        'password': password,
      });

      if (response.statusCode == 200) {
        final data = jsonDecode(response.body);
        final user = UserModel.fromJson(data['user'], token: data['token']);
        await _storage.saveToken(data['token']);
        await _storage.saveUserData(jsonEncode(user.toJson()));
        state = state.copyWith(user: user, isLoading: false);
        return true;
      } else {
        final error = jsonDecode(response.body);
        state = state.copyWith(
          isLoading: false,
          errorMessage: error['detail'] ?? 'Login failed. Please check credentials.',
        );
        return false;
      }
    } catch (e) {
      state = state.copyWith(
        isLoading: false,
        errorMessage: 'Network error: Unable to connect to server.',
      );
      return false;
    }
  }

  Future<void> logout() async {
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
