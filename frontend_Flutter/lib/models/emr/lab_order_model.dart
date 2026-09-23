class LabReportModel {
  final int id;
  final int labOrderId;
  final String conductedByUserName;
  final DateTime reportDate;
  final String resultSummary;
  final String findings;
  final String referenceRange;
  final String doctorRemarks;

  LabReportModel({
    required this.id,
    required this.labOrderId,
    required this.conductedByUserName,
    required this.reportDate,
    required this.resultSummary,
    required this.findings,
    required this.referenceRange,
    required this.doctorRemarks,
  });

  factory LabReportModel.fromJson(Map<String, dynamic> json) {
    return LabReportModel(
      id: json['id'] ?? 0,
      labOrderId: json['labOrderId'] ?? 0,
      conductedByUserName: json['conductedByUserName'] ?? '',
      reportDate: json['reportDate'] != null
          ? DateTime.parse(json['reportDate'])
          : DateTime.now(),
      resultSummary: json['resultSummary'] ?? '',
      findings: json['findings'] ?? '',
      referenceRange: json['referenceRange'] ?? '',
      doctorRemarks: json['doctorRemarks'] ?? '',
    );
  }
}

class LabOrderModel {
  final int id;
  final String orderNumber;
  final int patientId;
  final String doctorName;
  final String testName;
  final String category;
  final String priority;
  final String status;
  final DateTime orderedAt;
  final LabReportModel? report;

  LabOrderModel({
    required this.id,
    required this.orderNumber,
    required this.patientId,
    required this.doctorName,
    required this.testName,
    required this.category,
    required this.priority,
    required this.status,
    required this.orderedAt,
    this.report,
  });

  factory LabOrderModel.fromJson(Map<String, dynamic> json) {
    return LabOrderModel(
      id: json['id'] ?? 0,
      orderNumber: json['orderNumber'] ?? '',
      patientId: json['patientId'] ?? 0,
      doctorName: json['doctorName'] ?? '',
      testName: json['testName'] ?? '',
      category: json['category'] ?? '',
      priority: json['priority'] ?? '',
      status: json['status'] ?? '',
      orderedAt: json['orderedAt'] != null
          ? DateTime.parse(json['orderedAt'])
          : DateTime.now(),
      report: json['report'] != null
          ? LabReportModel.fromJson(json['report'])
          : null,
    );
  }
}
