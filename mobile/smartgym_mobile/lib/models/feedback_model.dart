class FeedbackModel {
  final String id;
  final String memberName;
  final String subject;
  final String content;
  final int rating;
  final String statusName;
  final String? adminResponse;
  final DateTime createdAt;

  const FeedbackModel({
    required this.id,
    required this.memberName,
    required this.subject,
    required this.content,
    required this.rating,
    required this.statusName,
    this.adminResponse,
    required this.createdAt,
  });

  factory FeedbackModel.fromJson(Map<String, dynamic> json) {
    return FeedbackModel(
      id: json['id']?.toString() ?? '',
      memberName: json['memberName'] ?? 'Member',
      subject: json['subject'] ?? '',
      content: json['content'] ?? '',
      rating: json['rating'] is int ? json['rating'] : 5,
      statusName: json['statusName'] ?? json['status']?.toString() ?? 'Submitted',
      adminResponse: json['adminResponse'],
      createdAt: json['createdAt'] != null
          ? DateTime.parse(json['createdAt'])
          : DateTime.now(),
    );
  }
}
