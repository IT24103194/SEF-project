class LocationModel {
  final String id;
  final String name;
  final String floor;
  final String description;

  const LocationModel({
    required this.id,
    required this.name,
    required this.floor,
    required this.description,
  });

  factory LocationModel.fromJson(Map<String, dynamic> json) {
    return LocationModel(
      id: json['id']?.toString() ?? '',
      name: json['name'] ?? '',
      floor: json['floor'] ?? '',
      description: json['description'] ?? '',
    );
  }
}

class EquipmentModel {
  final String id;
  final String locationId;
  final String locationName;
  final String serialNumber;
  final String name;
  final String model;
  final String manufacturer;
  final String statusName;

  const EquipmentModel({
    required this.id,
    required this.locationId,
    required this.locationName,
    required this.serialNumber,
    required this.name,
    required this.model,
    required this.manufacturer,
    required this.statusName,
  });

  factory EquipmentModel.fromJson(Map<String, dynamic> json) {
    return EquipmentModel(
      id: json['id']?.toString() ?? '',
      locationId: json['locationId']?.toString() ?? '',
      locationName: json['locationName'] ?? '',
      serialNumber: json['serialNumber'] ?? '',
      name: json['name'] ?? '',
      model: json['model'] ?? '',
      manufacturer: json['manufacturer'] ?? '',
      statusName: json['statusName'] ?? 'Operational',
    );
  }
}

class IssueImageModel {
  final String id;
  final String issueId;
  final String imageUrl;
  final String? originalFileName;
  final DateTime uploadedAt;

  const IssueImageModel({
    required this.id,
    required this.issueId,
    required this.imageUrl,
    this.originalFileName,
    required this.uploadedAt,
  });

  factory IssueImageModel.fromJson(Map<String, dynamic> json) {
    return IssueImageModel(
      id: json['id']?.toString() ?? '',
      issueId: json['issueId']?.toString() ?? '',
      imageUrl: json['imageUrl'] ?? '',
      originalFileName: json['originalFileName'],
      uploadedAt: json['uploadedAt'] != null
          ? DateTime.parse(json['uploadedAt'])
          : DateTime.now(),
    );
  }
}

class IssueHistoryModel {
  final String id;
  final String action;
  final String? fromStatus;
  final String? toStatus;
  final String? performedByUserName;
  final String? notes;
  final DateTime timestamp;

  const IssueHistoryModel({
    required this.id,
    required this.action,
    this.fromStatus,
    this.toStatus,
    this.performedByUserName,
    this.notes,
    required this.timestamp,
  });

  factory IssueHistoryModel.fromJson(Map<String, dynamic> json) {
    return IssueHistoryModel(
      id: json['id']?.toString() ?? '',
      action: json['action'] ?? '',
      fromStatus: json['fromStatus'],
      toStatus: json['toStatus'],
      performedByUserName: json['performedByUserName'],
      notes: json['notes'],
      timestamp: json['timestamp'] != null
          ? DateTime.parse(json['timestamp'])
          : DateTime.now(),
    );
  }
}

class FacilityIssueModel {
  final String id;
  final String reportedByMemberId;
  final String reporterName;
  final String? equipmentId;
  final String? equipmentName;
  final String? equipmentSerialNumber;
  final String locationId;
  final String locationName;
  final String locationFloor;
  final String title;
  final String description;
  final String? sanitizedDescription;
  final String? resolutionNotes;
  final String moderationStatus;
  final String? moderationReason;
  final String severityName;
  final String statusName;
  final DateTime reportedAt;
  final DateTime? resolvedAt;
  final List<IssueImageModel> images;

  const FacilityIssueModel({
    required this.id,
    required this.reportedByMemberId,
    required this.reporterName,
    this.equipmentId,
    this.equipmentName,
    this.equipmentSerialNumber,
    required this.locationId,
    required this.locationName,
    required this.locationFloor,
    required this.title,
    required this.description,
    this.sanitizedDescription,
    this.resolutionNotes,
    required this.moderationStatus,
    this.moderationReason,
    required this.severityName,
    required this.statusName,
    required this.reportedAt,
    this.resolvedAt,
    this.images = const [],
  });

  factory FacilityIssueModel.fromJson(Map<String, dynamic> json) {
    var rawImages = json['images'] as List<dynamic>? ??
        json['attachments'] as List<dynamic>? ??
        [];
    return FacilityIssueModel(
      id: json['id']?.toString() ?? '',
      reportedByMemberId: json['reportedByMemberId']?.toString() ?? '',
      reporterName: json['reporterName'] ?? 'Member',
      equipmentId: json['equipmentId']?.toString(),
      equipmentName: json['equipmentName'],
      equipmentSerialNumber: json['equipmentSerialNumber'],
      locationId: json['locationId']?.toString() ?? '',
      locationName: json['locationName'] ?? '',
      locationFloor: json['locationFloor'] ?? '',
      title: json['title'] ?? '',
      description: json['description'] ?? '',
      sanitizedDescription: json['sanitizedDescription'],
      resolutionNotes: json['resolutionNotes'],
      moderationStatus: json['moderationStatus'] ?? 'Clean',
      moderationReason: json['moderationReason'],
      severityName: json['severityName'] ?? json['severity']?.toString() ?? 'Medium',
      statusName: json['statusName'] ?? json['status']?.toString() ?? 'SUBMITTED',
      reportedAt: json['reportedAt'] != null
          ? DateTime.parse(json['reportedAt'])
          : DateTime.now(),
      resolvedAt: json['resolvedAt'] != null
          ? DateTime.parse(json['resolvedAt'])
          : null,
      images: rawImages.map((img) => IssueImageModel.fromJson(img)).toList(),
    );
  }
}
