class FacilityIssueModel {
  final String id;
  final String title;
  final String description;
  final String location;
  final String status;
  final String? equipmentName;
  final DateTime reportedAt;
  final double? estimatedRepairCost;

  const FacilityIssueModel({
    required this.id,
    required this.title,
    required this.description,
    required this.location,
    required this.status,
    this.equipmentName,
    required this.reportedAt,
    this.estimatedRepairCost,
  });

  factory FacilityIssueModel.fromJson(Map<String, dynamic> json) {
    return FacilityIssueModel(
      id: json['id']?.toString() ?? '',
      title: json['title'] ?? '',
      description: json['description'] ?? '',
      location: json['location'] ?? '',
      status: json['status'] ?? 'Reported',
      equipmentName: json['equipmentName'],
      reportedAt: json['reportedAt'] != null
          ? DateTime.parse(json['reportedAt'])
          : DateTime.now(),
      estimatedRepairCost: json['estimatedRepairCost'] != null
          ? (json['estimatedRepairCost'] as num).toDouble()
          : null,
    );
  }
}
