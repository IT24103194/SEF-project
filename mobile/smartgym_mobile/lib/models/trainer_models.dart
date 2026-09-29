class ScheduleAttendeeModel {
  final String bookingId;
  final String memberId;
  final String memberName;
  final String memberEmail;
  final String status;
  final DateTime bookedAt;
  final String? attendanceStatus;
  final DateTime? attendedAt;

  const ScheduleAttendeeModel({
    required this.bookingId,
    required this.memberId,
    required this.memberName,
    required this.memberEmail,
    required this.status,
    required this.bookedAt,
    this.attendanceStatus,
    this.attendedAt,
  });

  bool get isAttended => attendanceStatus?.toUpperCase() == 'ATTENDED';
  bool get isNoShow => attendanceStatus?.toUpperCase() == 'NOSHOW';

  factory ScheduleAttendeeModel.fromJson(Map<String, dynamic> json) {
    return ScheduleAttendeeModel(
      bookingId: json['id']?.toString() ?? json['bookingId']?.toString() ?? '',
      memberId: json['memberId']?.toString() ?? '',
      memberName: json['memberName'] ?? 'Member',
      memberEmail: json['memberEmail'] ?? '',
      status: json['status']?.toString() ?? 'Confirmed',
      bookedAt: json['bookedAt'] != null
          ? DateTime.parse(json['bookedAt'])
          : DateTime.now(),
      attendanceStatus: json['attendanceStatus']?.toString(),
      attendedAt: json['attendedAt'] != null
          ? DateTime.parse(json['attendedAt'])
          : null,
    );
  }
}

class MemberGoalOverviewModel {
  final String id;
  final String title;
  final double targetValue;
  final double currentValue;
  final String unit;
  final String status;
  final DateTime targetDate;

  const MemberGoalOverviewModel({
    required this.id,
    required this.title,
    required this.targetValue,
    required this.currentValue,
    required this.unit,
    required this.status,
    required this.targetDate,
  });

  factory MemberGoalOverviewModel.fromJson(Map<String, dynamic> json) {
    return MemberGoalOverviewModel(
      id: json['id']?.toString() ?? '',
      title: json['title'] ?? '',
      targetValue: (json['targetValue'] is num) ? (json['targetValue'] as num).toDouble() : 0.0,
      currentValue: (json['currentValue'] is num) ? (json['currentValue'] as num).toDouble() : 0.0,
      unit: json['unit'] ?? '',
      status: json['status']?.toString() ?? 'InProgress',
      targetDate: json['targetDate'] != null
          ? DateTime.parse(json['targetDate'])
          : DateTime.now(),
    );
  }
}
