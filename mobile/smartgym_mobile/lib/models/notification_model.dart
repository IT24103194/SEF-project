class NotificationModel {
  final String id;
  final String userId;
  final String title;
  final String message;
  final String eventType;
  final String category;
  final String priority;
  final String? referenceId;
  final String? referenceType;
  final bool isRead;
  final DateTime? readAt;
  final DateTime createdAt;

  NotificationModel({
    required this.id,
    required this.userId,
    required this.title,
    required this.message,
    required this.eventType,
    required this.category,
    this.priority = 'Normal',
    this.referenceId,
    this.referenceType,
    required this.isRead,
    this.readAt,
    required this.createdAt,
  });

  NotificationModel copyWith({
    String? id,
    String? userId,
    String? title,
    String? message,
    String? eventType,
    String? category,
    String? priority,
    String? referenceId,
    String? referenceType,
    bool? isRead,
    DateTime? readAt,
    DateTime? createdAt,
  }) {
    return NotificationModel(
      id: id ?? this.id,
      userId: userId ?? this.userId,
      title: title ?? this.title,
      message: message ?? this.message,
      eventType: eventType ?? this.eventType,
      category: category ?? this.category,
      priority: priority ?? this.priority,
      referenceId: referenceId ?? this.referenceId,
      referenceType: referenceType ?? this.referenceType,
      isRead: isRead ?? this.isRead,
      readAt: readAt ?? this.readAt,
      createdAt: createdAt ?? this.createdAt,
    );
  }

  factory NotificationModel.fromJson(Map<String, dynamic> json) {
    return NotificationModel(
      id: json['id']?.toString() ?? '',
      userId: json['userId']?.toString() ?? '',
      title: json['title'] ?? '',
      message: json['message'] ?? '',
      eventType: json['eventType']?.toString() ?? 'General',
      category: json['category']?.toString() ?? 'General',
      priority: json['priority']?.toString() ?? 'Normal',
      referenceId: json['referenceId']?.toString(),
      referenceType: json['referenceType']?.toString(),
      isRead: json['isRead'] ?? false,
      readAt: json['readAt'] != null ? DateTime.parse(json['readAt']) : null,
      createdAt: json['createdAt'] != null
          ? DateTime.parse(json['createdAt'])
          : DateTime.now(),
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'userId': userId,
      'title': title,
      'message': message,
      'eventType': eventType,
      'category': category,
      'priority': priority,
      'referenceId': referenceId,
      'referenceType': referenceType,
      'isRead': isRead,
      'readAt': readAt?.toIso8601String(),
      'createdAt': createdAt.toIso8601String(),
    };
  }
}

class NotificationSummaryModel {
  final int totalCount;
  final int unreadCount;
  final int criticalCount;
  final int unreadBookingEvents;
  final int unreadMembershipEvents;
  final int unreadFacilityEvents;
  final List<NotificationModel> recentUnread;

  NotificationSummaryModel({
    required this.totalCount,
    required this.unreadCount,
    this.criticalCount = 0,
    this.unreadBookingEvents = 0,
    this.unreadMembershipEvents = 0,
    this.unreadFacilityEvents = 0,
    this.recentUnread = const [],
  });

  factory NotificationSummaryModel.fromJson(Map<String, dynamic> json) {
    var recents = <NotificationModel>[];
    if (json['recentUnread'] != null && json['recentUnread'] is List) {
      recents = (json['recentUnread'] as List)
          .map((n) => NotificationModel.fromJson(n))
          .toList();
    }

    return NotificationSummaryModel(
      totalCount: json['totalCount'] ?? 0,
      unreadCount: json['unreadCount'] ?? 0,
      criticalCount: json['criticalCount'] ?? 0,
      unreadBookingEvents: json['unreadBookingEvents'] ?? 0,
      unreadMembershipEvents: json['unreadMembershipEvents'] ?? 0,
      unreadFacilityEvents: json['unreadFacilityEvents'] ?? 0,
      recentUnread: recents,
    );
  }
}
