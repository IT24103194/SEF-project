class ClassScheduleModel {
  final String id;
  final String className;
  final String trainerName;
  final String room;
  final DateTime startTime;
  final int durationMinutes;
  final int capacity;
  final int bookedCount;

  const ClassScheduleModel({
    required this.id,
    required this.className,
    required this.trainerName,
    required this.room,
    required this.startTime,
    required this.durationMinutes,
    required this.capacity,
    required this.bookedCount,
  });

  bool get isFull => bookedCount >= capacity;
  int get availableSpots => capacity - bookedCount;

  factory ClassScheduleModel.fromJson(Map<String, dynamic> json) {
    return ClassScheduleModel(
      id: json['id']?.toString() ?? '',
      className: json['className'] ?? '',
      trainerName: json['trainerName'] ?? '',
      room: json['room'] ?? '',
      startTime: json['startTime'] != null
          ? DateTime.parse(json['startTime'])
          : DateTime.now(),
      durationMinutes: json['durationMinutes'] ?? 60,
      capacity: json['capacity'] ?? 20,
      bookedCount: json['bookedCount'] ?? 0,
    );
  }
}
