class MembershipPlanModel {
  final String id;
  final String name;
  final String description;
  final double price;
  final int durationDays;
  final int maxClassesPerWeek;
  final bool hasTrainerAccess;
  final bool isActive;

  MembershipPlanModel({
    required this.id,
    required this.name,
    required this.description,
    required this.price,
    required this.durationDays,
    required this.maxClassesPerWeek,
    required this.hasTrainerAccess,
    required this.isActive,
  });

  factory MembershipPlanModel.fromJson(Map<String, dynamic> json) {
    return MembershipPlanModel(
      id: json['id'] ?? '',
      name: json['name'] ?? '',
      description: json['description'] ?? '',
      price: (json['price'] as num?)?.toDouble() ?? 0.0,
      durationDays: (json['durationDays'] as num?)?.toInt() ?? 30,
      maxClassesPerWeek: (json['maxClassesPerWeek'] as num?)?.toInt() ?? 5,
      hasTrainerAccess: json['hasTrainerAccess'] ?? false,
      isActive: json['isActive'] ?? true,
    );
  }
}

class MembershipModel {
  final String id;
  final String memberId;
  final String memberName;
  final String memberEmail;
  final String planId;
  final String planName;
  final DateTime startDate;
  final DateTime endDate;
  final String status;
  final bool autoRenew;
  final double pricePaid;

  MembershipModel({
    required this.id,
    required this.memberId,
    required this.memberName,
    required this.memberEmail,
    required this.planId,
    required this.planName,
    required this.startDate,
    required this.endDate,
    required this.status,
    required this.autoRenew,
    required this.pricePaid,
  });

  bool get isExpired => DateTime.now().toUtc().isAfter(endDate);
  int get daysRemaining => endDate.difference(DateTime.now().toUtc()).inDays;

  factory MembershipModel.fromJson(Map<String, dynamic> json) {
    return MembershipModel(
      id: json['id'] ?? '',
      memberId: json['memberId'] ?? '',
      memberName: json['memberName'] ?? '',
      memberEmail: json['memberEmail'] ?? '',
      planId: json['planId'] ?? '',
      planName: json['planName'] ?? '',
      startDate: json['startDate'] != null
          ? DateTime.parse(json['startDate'])
          : DateTime.now(),
      endDate: json['endDate'] != null
          ? DateTime.parse(json['endDate'])
          : DateTime.now(),
      status: json['status']?.toString() ?? 'Active',
      autoRenew: json['autoRenew'] ?? false,
      pricePaid: (json['pricePaid'] as num?)?.toDouble() ?? 0.0,
    );
  }
}

class GoalModel {
  final String id;
  final String memberId;
  final String title;
  final double targetValue;
  final double currentValue;
  final String unit;
  final DateTime targetDate;
  final String status;
  final int totalLogsCount;
  final List<ProgressRecordModel> recentLogs;

  GoalModel({
    required this.id,
    required this.memberId,
    required this.title,
    required this.targetValue,
    required this.currentValue,
    required this.unit,
    required this.targetDate,
    required this.status,
    required this.totalLogsCount,
    this.recentLogs = const [],
  });

  double get progressPercentage {
    if (targetValue <= 0) return 0.0;
    return (currentValue / targetValue * 100).clamp(0.0, 100.0);
  }

  bool get isAchieved =>
      status.toLowerCase() == 'achieved' || status == '1';

  factory GoalModel.fromJson(Map<String, dynamic> json) {
    var logs = <ProgressRecordModel>[];
    if (json['recentLogs'] != null && json['recentLogs'] is List) {
      logs = (json['recentLogs'] as List)
          .map((l) => ProgressRecordModel.fromJson(l))
          .toList();
    }

    return GoalModel(
      id: json['id'] ?? '',
      memberId: json['memberId'] ?? '',
      title: json['title'] ?? '',
      targetValue: (json['targetValue'] as num?)?.toDouble() ?? 0.0,
      currentValue: (json['currentValue'] as num?)?.toDouble() ?? 0.0,
      unit: json['unit'] ?? 'kg',
      targetDate: json['targetDate'] != null
          ? DateTime.parse(json['targetDate'])
          : DateTime.now(),
      status: json['status']?.toString() ?? 'InProgress',
      totalLogsCount: (json['totalLogsCount'] as num?)?.toInt() ?? 0,
      recentLogs: logs,
    );
  }
}

class ProgressRecordModel {
  final String id;
  final String goalId;
  final DateTime recordedDate;
  final double value;
  final String? notes;

  ProgressRecordModel({
    required this.id,
    required this.goalId,
    required this.recordedDate,
    required this.value,
    this.notes,
  });

  factory ProgressRecordModel.fromJson(Map<String, dynamic> json) {
    return ProgressRecordModel(
      id: json['id'] ?? '',
      goalId: json['goalId'] ?? '',
      recordedDate: json['recordedDate'] != null
          ? DateTime.parse(json['recordedDate'])
          : DateTime.now(),
      value: (json['value'] as num?)?.toDouble() ?? 0.0,
      notes: json['notes'],
    );
  }
}
