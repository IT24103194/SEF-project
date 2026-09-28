class ClassScheduleModel {
  final String id;
  final String classId;
  final String className;
  final String categoryName;
  final String intensityLevel;
  final String trainerId;
  final String trainerName;
  final String room;
  final DateTime startTime;
  final DateTime endTime;
  final int durationMinutes;
  final int capacity;
  final int bookedCount;
  final int status; // 1: Scheduled, 2: Completed, 3: Cancelled

  const ClassScheduleModel({
    required this.id,
    required this.classId,
    required this.className,
    required this.categoryName,
    required this.intensityLevel,
    required this.trainerId,
    required this.trainerName,
    required this.room,
    required this.startTime,
    required this.endTime,
    required this.durationMinutes,
    required this.capacity,
    required this.bookedCount,
    required this.status,
  });

  bool get isFull => bookedCount >= capacity;
  int get availableSpots => (capacity - bookedCount).clamp(0, capacity);
  bool get isCancelled => status == 3;

  factory ClassScheduleModel.fromJson(Map<String, dynamic> json) {
    return ClassScheduleModel(
      id: json['id']?.toString() ?? '',
      classId: json['classId']?.toString() ?? '',
      className: json['className'] ?? '',
      categoryName: json['categoryName'] ?? '',
      intensityLevel: json['intensityLevel'] ?? 'Medium',
      trainerId: json['trainerId']?.toString() ?? '',
      trainerName: json['trainerName'] ?? '',
      room: json['room'] ?? '',
      startTime: json['startTime'] != null
          ? DateTime.parse(json['startTime'])
          : DateTime.now(),
      endTime: json['endTime'] != null
          ? DateTime.parse(json['endTime'])
          : DateTime.now().add(const Duration(hours: 1)),
      durationMinutes: json['durationMinutes'] ?? 60,
      capacity: json['capacity'] ?? 20,
      bookedCount: json['bookedCount'] ?? 0,
      status: json['status'] ?? 1,
    );
  }
}

class ClassBookingModel {
  final String id;
  final String scheduleId;
  final String className;
  final String trainerName;
  final String room;
  final DateTime classStartTime;
  final DateTime classEndTime;
  final String memberId;
  final String memberName;
  final String memberEmail;
  final DateTime bookingTime;
  final int status; // 1: Confirmed, 2: Cancelled, 3: Waitlisted
  final DateTime? cancelledAt;
  final String? cancellationReason;
  final String? attendanceStatus;

  const ClassBookingModel({
    required this.id,
    required this.scheduleId,
    required this.className,
    required this.trainerName,
    required this.room,
    required this.classStartTime,
    required this.classEndTime,
    required this.memberId,
    required this.memberName,
    required this.memberEmail,
    required this.bookingTime,
    required this.status,
    this.cancelledAt,
    this.cancellationReason,
    this.attendanceStatus,
  });

  bool get isConfirmed => status == 1;
  bool get isCancelled => status == 2;

  factory ClassBookingModel.fromJson(Map<String, dynamic> json) {
    String? attStatus;
    if (json['attendance'] != null && json['attendance'] is Map) {
      final code = json['attendance']['status'];
      if (code == 1) attStatus = 'Attended';
      if (code == 2) attStatus = 'Absent';
      if (code == 3) attStatus = 'Excused';
    }

    return ClassBookingModel(
      id: json['id']?.toString() ?? '',
      scheduleId: json['scheduleId']?.toString() ?? '',
      className: json['className'] ?? '',
      trainerName: json['trainerName'] ?? '',
      room: json['room'] ?? '',
      classStartTime: json['classStartTime'] != null
          ? DateTime.parse(json['classStartTime'])
          : DateTime.now(),
      classEndTime: json['classEndTime'] != null
          ? DateTime.parse(json['classEndTime'])
          : DateTime.now(),
      memberId: json['memberId']?.toString() ?? '',
      memberName: json['memberName'] ?? '',
      memberEmail: json['memberEmail'] ?? '',
      bookingTime: json['bookingTime'] != null
          ? DateTime.parse(json['bookingTime'])
          : DateTime.now(),
      status: json['status'] ?? 1,
      cancelledAt: json['cancelledAt'] != null
          ? DateTime.parse(json['cancelledAt'])
          : null,
      cancellationReason: json['cancellationReason'],
      attendanceStatus: attStatus,
    );
  }
}
