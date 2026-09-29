class UserModel {
  final String id;
  final String email;
  final String firstName;
  final String lastName;
  final String role;
  final List<String> roles;
  final String? phoneNumber;
  final String? token;

  const UserModel({
    required this.id,
    required this.email,
    required this.firstName,
    required this.lastName,
    required this.role,
    this.roles = const [],
    this.phoneNumber,
    this.token,
  });

  String get fullName => '$firstName $lastName'.trim();

  bool get isTrainer =>
      role.toUpperCase().contains('TRAINER') ||
      roles.any((r) => r.toUpperCase().contains('TRAINER'));

  bool get isAdmin =>
      role.toUpperCase().contains('ADMIN') ||
      roles.any((r) => r.toUpperCase().contains('ADMIN'));

  bool get isStaff => isTrainer || isAdmin;

  bool get isMember => !isStaff;

  factory UserModel.fromJson(Map<String, dynamic> json, {String? token}) {
    List<String> parsedRoles = [];
    if (json['roles'] is List) {
      parsedRoles = (json['roles'] as List).map((r) => r.toString()).toList();
    } else if (json['role'] is String) {
      parsedRoles = [json['role'] as String];
    }

    final primaryRole = parsedRoles.isNotEmpty
        ? parsedRoles.first
        : (json['role']?.toString() ?? 'Member');

    return UserModel(
      id: json['id']?.toString() ?? '',
      email: json['email'] ?? '',
      firstName: json['firstName'] ?? '',
      lastName: json['lastName'] ?? '',
      role: primaryRole,
      roles: parsedRoles,
      phoneNumber: json['phoneNumber'],
      token: token ?? json['token'],
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'email': email,
      'firstName': firstName,
      'lastName': lastName,
      'role': role,
      'roles': roles,
      'phoneNumber': phoneNumber,
      'token': token,
    };
  }
}
