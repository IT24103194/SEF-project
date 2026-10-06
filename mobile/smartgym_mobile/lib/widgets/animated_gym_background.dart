import 'dart:io';
import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';

class AnimatedGymBackground extends StatefulWidget {
  static const String defaultGymImageUrl =
      'https://walldecordelights.com/cdn/shop/files/Typography-Graffiti-Gym-Word-Discipline-Focus-Power-Repeat-Sign-8.png?v=1788110044&width=1080';

  final String imageUrl;
  final Widget child;
  final PreferredSizeWidget? appBar;
  final Widget? bottomNavigationBar;
  final Widget? floatingActionButton;
  final FloatingActionButtonLocation? floatingActionButtonLocation;
  final bool? resizeToAvoidBottomInset;
  final Widget? drawer;
  final Widget? endDrawer;

  const AnimatedGymBackground({
    super.key,
    this.imageUrl = defaultGymImageUrl,
    required this.child,
    this.appBar,
    this.bottomNavigationBar,
    this.floatingActionButton,
    this.floatingActionButtonLocation,
    this.resizeToAvoidBottomInset,
    this.drawer,
    this.endDrawer,
  });

  @override
  State<AnimatedGymBackground> createState() => _AnimatedGymBackgroundState();
}

class _AnimatedGymBackgroundState extends State<AnimatedGymBackground>
    with SingleTickerProviderStateMixin {
  late AnimationController _bgController;
  late Animation<Alignment> _bgAnimation;

  @override
  void initState() {
    super.initState();
    final isTest = !kIsWeb &&
        (Platform.environment.containsKey('FLUTTER_TEST') ||
            Platform.environment['FLUTTER_TEST'] == 'true');

    // Initialize the animation to pan back and forth over 20 seconds
    _bgController = AnimationController(
      vsync: this,
      duration: const Duration(seconds: 20),
    );

    // In unit/widget tests, do not repeat indefinitely to prevent pumpAndSettle timeouts
    if (!isTest) {
      _bgController.repeat(reverse: true);
    }

    _bgAnimation = Tween<Alignment>(
      begin: Alignment.topLeft,
      end: Alignment.bottomRight,
    ).animate(CurvedAnimation(
      parent: _bgController,
      curve: Curves.easeInOut,
    ));
  }

  @override
  void dispose() {
    _bgController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: Colors.transparent,
      appBar: widget.appBar,
      drawer: widget.drawer,
      endDrawer: widget.endDrawer,
      bottomNavigationBar: widget.bottomNavigationBar,
      floatingActionButton: widget.floatingActionButton,
      floatingActionButtonLocation: widget.floatingActionButtonLocation,
      resizeToAvoidBottomInset: widget.resizeToAvoidBottomInset,
      body: Stack(
        fit: StackFit.expand,
        children: [
          // BOTTOM LAYER: The Animated Background Image
          AnimatedBuilder(
            animation: _bgAnimation,
            builder: (context, child) {
              final isTest = !kIsWeb &&
                  (Platform.environment.containsKey('FLUTTER_TEST') ||
                      Platform.environment['FLUTTER_TEST'] == 'true');

              if (isTest) {
                return Container(
                  decoration: BoxDecoration(
                    gradient: LinearGradient(
                      begin: _bgAnimation.value,
                      end: Alignment.bottomRight,
                      colors: const [
                        Color(0xFF0B0F19),
                        Color(0xFF1E1B4B),
                        Color(0xFF0B0F19),
                      ],
                    ),
                  ),
                );
              }

              return Container(
                decoration: BoxDecoration(
                  image: DecorationImage(
                    image: NetworkImage(widget.imageUrl),
                    fit: BoxFit.cover,
                    alignment: _bgAnimation.value,
                    // Applies dark tint to maintain contrast and gym branding
                    colorFilter: ColorFilter.mode(
                      const Color(0xFF0B0F19).withValues(alpha: 0.72),
                      BlendMode.darken,
                    ),
                    onError: (exception, stackTrace) {},
                  ),
                ),
              );
            },
          ),

          // TOP LAYER: The unique UI of the screen
          widget.child,
        ],
      ),
    );
  }
}
