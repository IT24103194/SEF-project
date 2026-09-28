import 'dart:typed_data';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:image_picker/image_picker.dart';
import '../models/facility_issue_model.dart';
import '../services/facility_api_service.dart';
import '../widgets/app_button.dart';

class FacilityReportScreen extends ConsumerStatefulWidget {
  const FacilityReportScreen({super.key});

  @override
  ConsumerState<FacilityReportScreen> createState() => _FacilityReportScreenState();
}

class _FacilityReportScreenState extends ConsumerState<FacilityReportScreen> {
  final _titleController = TextEditingController();
  final _descriptionController = TextEditingController();
  final _formKey = GlobalKey<FormState>();

  final ImagePicker _picker = ImagePicker();
  XFile? _selectedImage;
  Uint8List? _selectedImageBytes;

  List<LocationModel> _locations = [];
  List<EquipmentModel> _equipmentList = [];
  String? _selectedLocationId;
  String? _selectedEquipmentId;
  int _selectedSeverity = 2; // Medium

  bool _isLoadingLocations = true;
  bool _isSubmitting = false;

  @override
  void initState() {
    super.initState();
    _loadLocations();
  }

  @override
  void dispose() {
    _titleController.dispose();
    _descriptionController.dispose();
    super.dispose();
  }

  Future<void> _loadLocations() async {
    final api = ref.read(facilityApiServiceProvider);
    try {
      final locs = await api.fetchLocations();
      final eq = await api.fetchEquipment();
      if (mounted) {
        setState(() {
          _locations = locs;
          _equipmentList = eq;
          if (locs.isNotEmpty) {
            _selectedLocationId = locs.first.id;
          }
          _isLoadingLocations = false;
        });
      }
    } catch (e) {
      if (mounted) {
        setState(() => _isLoadingLocations = false);
      }
    }
  }

  Future<void> _pickImage(ImageSource source) async {
    try {
      final XFile? photo = await _picker.pickImage(
        source: source,
        maxWidth: 1920,
        maxHeight: 1080,
        imageQuality: 85,
      );
      if (photo != null) {
        final bytes = await photo.readAsBytes();
        setState(() {
          _selectedImage = photo;
          _selectedImageBytes = bytes;
        });
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Could not access camera/gallery: $e'), backgroundColor: Colors.red),
        );
      }
    }
  }

  void _clearImage() {
    setState(() {
      _selectedImage = null;
      _selectedImageBytes = null;
    });
  }

  Future<void> _submitReport() async {
    if (!_formKey.currentState!.validate()) return;
    if (_selectedLocationId == null) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Please select a facility location.'), backgroundColor: Colors.red),
      );
      return;
    }

    setState(() => _isSubmitting = true);
    final api = ref.read(facilityApiServiceProvider);

    try {
      final success = await api.submitIssueWithImage(
        locationId: _selectedLocationId!,
        equipmentId: _selectedEquipmentId,
        title: _titleController.text.trim(),
        description: _descriptionController.text.trim(),
        severity: _selectedSeverity,
        imageBytes: _selectedImageBytes,
        fileName: _selectedImage?.name ?? 'photo.jpg',
      );

      if (mounted) {
        setState(() => _isSubmitting = false);
        if (success) {
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(
              content: Text('Issue reported successfully. Staff and AI Triage notified!'),
              backgroundColor: Color(0xFF10B981),
            ),
          );
          Navigator.of(context).pop();
        } else {
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(
              content: Text('Failed to submit report. Please try again.'),
              backgroundColor: Color(0xFFF43F5E),
            ),
          );
        }
      }
    } catch (e) {
      if (mounted) {
        setState(() => _isSubmitting = false);
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Error: $e'), backgroundColor: const Color(0xFFF43F5E)),
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final filteredEquipment = _selectedLocationId == null
        ? _equipmentList
        : _equipmentList.where((e) => e.locationId == _selectedLocationId).toList();

    return Scaffold(
      backgroundColor: const Color(0xFF0B0F19),
      appBar: AppBar(
        backgroundColor: const Color(0xFF111827),
        elevation: 0,
        title: const Text('Report Equipment Issue'),
      ),
      body: _isLoadingLocations
          ? const Center(child: CircularProgressIndicator(color: Color(0xFF6366F1)))
          : SingleChildScrollView(
              padding: const EdgeInsets.all(20),
              child: Form(
                key: _formKey,
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    // Information Banner
                    Container(
                      padding: const EdgeInsets.all(14),
                      decoration: BoxDecoration(
                        color: const Color(0xFF6366F1).withValues(alpha: 0.12),
                        borderRadius: BorderRadius.circular(12),
                        border: Border.all(color: const Color(0xFF6366F1).withValues(alpha: 0.3)),
                      ),
                      child: const Row(
                        children: [
                          Icon(Icons.auto_awesome, color: Color(0xFF6366F1), size: 22),
                          SizedBox(width: 12),
                          Expanded(
                            child: Text(
                              'Take or upload a photo of the damaged machine for automated diagnostics and rapid dispatch.',
                              style: TextStyle(color: Color(0xFFCBD5E1), fontSize: 13),
                            ),
                          ),
                        ],
                      ),
                    ),
                    const SizedBox(height: 20),

                    // Photo Picker & Preview
                    Container(
                      padding: const EdgeInsets.all(16),
                      decoration: BoxDecoration(
                        color: const Color(0xFF111827),
                        borderRadius: BorderRadius.circular(12),
                        border: Border.all(color: Colors.white.withValues(alpha: 0.08)),
                      ),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          const Text(
                            'Equipment Photo',
                            style: TextStyle(color: Colors.white, fontSize: 14, fontWeight: FontWeight.bold),
                          ),
                          const SizedBox(height: 8),

                          if (_selectedImageBytes != null) ...[
                            // Image Preview
                            Stack(
                              children: [
                                Container(
                                  height: 180,
                                  width: double.infinity,
                                  decoration: BoxDecoration(
                                    borderRadius: BorderRadius.circular(10),
                                    border: Border.all(color: Colors.white24),
                                  ),
                                  clipBehavior: Clip.antiAlias,
                                  child: Image.memory(_selectedImageBytes!, fit: BoxFit.cover),
                                ),
                                Positioned(
                                  top: 8,
                                  right: 8,
                                  child: CircleAvatar(
                                    backgroundColor: Colors.black87,
                                    radius: 16,
                                    child: IconButton(
                                      icon: const Icon(Icons.close, size: 16, color: Colors.white),
                                      onPressed: _clearImage,
                                    ),
                                  ),
                                ),
                              ],
                            ),
                            const SizedBox(height: 12),
                          ],

                          // Capture / Gallery Action Buttons
                          Row(
                            children: [
                              Expanded(
                                child: OutlinedButton.icon(
                                  style: OutlinedButton.styleFrom(
                                    foregroundColor: Colors.white,
                                    side: const BorderSide(color: Color(0xFF6366F1)),
                                    padding: const EdgeInsets.symmetric(vertical: 12),
                                    shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
                                  ),
                                  icon: const Icon(Icons.camera_alt, color: Color(0xFF6366F1)),
                                  label: const Text('Take Photo'),
                                  onPressed: () => _pickImage(ImageSource.camera),
                                ),
                              ),
                              const SizedBox(width: 12),
                              Expanded(
                                child: OutlinedButton.icon(
                                  style: OutlinedButton.styleFrom(
                                    foregroundColor: Colors.white,
                                    side: const BorderSide(color: Colors.white24),
                                    padding: const EdgeInsets.symmetric(vertical: 12),
                                    shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
                                  ),
                                  icon: const Icon(Icons.photo_library, color: Colors.white70),
                                  label: const Text('Gallery'),
                                  onPressed: () => _pickImage(ImageSource.gallery),
                                ),
                              ),
                            ],
                          ),
                        ],
                      ),
                    ),
                    const SizedBox(height: 16),

                    // Issue Title
                    TextFormField(
                      controller: _titleController,
                      style: const TextStyle(color: Colors.white),
                      decoration: InputDecoration(
                        labelText: 'Issue Title (e.g. Treadmill Belt Slipping) *',
                        labelStyle: const TextStyle(color: Color(0xFF94A3B8)),
                        filled: true,
                        fillColor: const Color(0xFF111827),
                        border: OutlineInputBorder(borderRadius: BorderRadius.circular(10)),
                      ),
                      validator: (v) => v == null || v.trim().isEmpty ? 'Required' : null,
                    ),
                    const SizedBox(height: 16),

                    // Location Dropdown
                    DropdownButtonFormField<String>(
                      initialValue: _selectedLocationId,
                      dropdownColor: const Color(0xFF111827),
                      style: const TextStyle(color: Colors.white),
                      decoration: InputDecoration(
                        labelText: 'Facility Location / Zone *',
                        labelStyle: const TextStyle(color: Color(0xFF94A3B8)),
                        filled: true,
                        fillColor: const Color(0xFF111827),
                        border: OutlineInputBorder(borderRadius: BorderRadius.circular(10)),
                      ),
                      items: _locations.map((loc) {
                        return DropdownMenuItem<String>(
                          value: loc.id,
                          child: Text('${loc.name} (${loc.floor})', overflow: TextOverflow.ellipsis),
                        );
                      }).toList(),
                      onChanged: (val) {
                        setState(() {
                          _selectedLocationId = val;
                          _selectedEquipmentId = null;
                        });
                      },
                    ),
                    const SizedBox(height: 16),

                    // Equipment Dropdown (Optional)
                    DropdownButtonFormField<String>(
                      initialValue: _selectedEquipmentId,
                      dropdownColor: const Color(0xFF111827),
                      style: const TextStyle(color: Colors.white),
                      decoration: InputDecoration(
                        labelText: 'Specific Equipment Machine (Optional)',
                        labelStyle: const TextStyle(color: Color(0xFF94A3B8)),
                        filled: true,
                        fillColor: const Color(0xFF111827),
                        border: OutlineInputBorder(borderRadius: BorderRadius.circular(10)),
                      ),
                      items: [
                        const DropdownMenuItem<String>(
                          value: null,
                          child: Text('General Facility / Non-Equipment', style: TextStyle(color: Colors.white60)),
                        ),
                        ...filteredEquipment.map((eq) {
                          return DropdownMenuItem<String>(
                            value: eq.id,
                            child: Text('${eq.name} (${eq.serialNumber})', overflow: TextOverflow.ellipsis),
                          );
                        }),
                      ],
                      onChanged: (val) {
                        setState(() => _selectedEquipmentId = val);
                      },
                    ),
                    const SizedBox(height: 16),

                    // Severity Selector
                    DropdownButtonFormField<int>(
                      initialValue: _selectedSeverity,
                      dropdownColor: const Color(0xFF111827),
                      style: const TextStyle(color: Colors.white),
                      decoration: InputDecoration(
                        labelText: 'Severity Level',
                        labelStyle: const TextStyle(color: Color(0xFF94A3B8)),
                        filled: true,
                        fillColor: const Color(0xFF111827),
                        border: OutlineInputBorder(borderRadius: BorderRadius.circular(10)),
                      ),
                      items: const [
                        DropdownMenuItem(value: 1, child: Text('Low — Minor cosmetic or non-critical issue')),
                        DropdownMenuItem(value: 2, child: Text('Medium — Machine usable with caution')),
                        DropdownMenuItem(value: 3, child: Text('High — Machine unusable / halted')),
                        DropdownMenuItem(value: 4, child: Text('Critical — Safety hazard / power danger')),
                      ],
                      onChanged: (val) {
                        if (val != null) setState(() => _selectedSeverity = val);
                      },
                    ),
                    const SizedBox(height: 16),

                    // Problem Description
                    TextFormField(
                      controller: _descriptionController,
                      maxLines: 4,
                      style: const TextStyle(color: Colors.white),
                      decoration: InputDecoration(
                        labelText: 'Detailed Problem Description *',
                        labelStyle: const TextStyle(color: Color(0xFF94A3B8)),
                        filled: true,
                        fillColor: const Color(0xFF111827),
                        border: OutlineInputBorder(borderRadius: BorderRadius.circular(10)),
                      ),
                      validator: (v) => v == null || v.trim().isEmpty ? 'Required' : null,
                    ),
                    const SizedBox(height: 24),

                    // Submit Button
                    AppButton(
                      label: 'Submit Equipment Report',
                      isLoading: _isSubmitting,
                      onPressed: _submitReport,
                    ),
                  ],
                ),
              ),
            ),
    );
  }
}
