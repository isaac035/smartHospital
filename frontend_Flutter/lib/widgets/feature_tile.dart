import 'dart:async';

import 'package:flutter/material.dart';

import '../core/theme/app_theme.dart';

class IconBadge extends StatelessWidget {
  const IconBadge({
    super.key,
    required this.icon,
    this.tint = AppTheme.serviceAppointmentsTint,
    this.foreground = AppTheme.serviceAppointmentsForeground,
    this.size = 48,
  });

  final IconData icon;
  final Color tint;
  final Color foreground;
  final double size;

  @override
  Widget build(BuildContext context) => Container(
    width: size,
    height: size,
    decoration: BoxDecoration(
      color: tint,
      borderRadius: BorderRadius.circular(AppTheme.radiusSmall + 4),
    ),
    alignment: Alignment.center,
    child: Icon(icon, size: 26, color: foreground),
  );
}

class FeatureTile extends StatefulWidget {
  const FeatureTile({
    super.key,
    required this.icon,
    required this.title,
    required this.subtitle,
    required this.semanticDestination,
    required this.tint,
    required this.foreground,
    required this.onTap,
    this.horizontal = false,
    this.staggerIndex = 0,
  });

  final IconData icon;
  final String title;
  final String subtitle;
  final String semanticDestination;
  final Color tint;
  final Color foreground;
  final VoidCallback onTap;
  final bool horizontal;
  final int staggerIndex;

  @override
  State<FeatureTile> createState() => _FeatureTileState();
}

class _FeatureTileState extends State<FeatureTile> {
  bool _entered = false;
  bool _pressed = false;
  bool _hovered = false;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (!mounted || MediaQuery.of(context).disableAnimations) {
        if (mounted) setState(() => _entered = true);
        return;
      }
      Timer(Duration(milliseconds: 40 * widget.staggerIndex), () {
        if (mounted) setState(() => _entered = true);
      });
    });
  }

  @override
  Widget build(BuildContext context) {
    final reduceMotion = MediaQuery.of(context).disableAnimations;
    final radius = BorderRadius.circular(AppTheme.radiusMedium + 2);
    final content = widget.horizontal
        ? Row(
            children: [
              IconBadge(
                icon: widget.icon,
                tint: widget.tint,
                foreground: widget.foreground,
              ),
              const SizedBox(width: 16),
              Expanded(child: _titleAndSubtitle(context)),
              const SizedBox(width: 8),
              Icon(
                Icons.arrow_forward_rounded,
                color: AppTheme.textMuted,
                size: 20,
              ),
            ],
          )
        : Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  IconBadge(
                    icon: widget.icon,
                    tint: widget.tint,
                    foreground: widget.foreground,
                  ),
                  Icon(
                    Icons.arrow_outward_rounded,
                    color: AppTheme.textMuted,
                    size: 19,
                  ),
                ],
              ),
              const Spacer(),
              _titleAndSubtitle(context),
            ],
          );

    return Semantics(
      button: true,
      label: '${widget.title}, opens ${widget.semanticDestination}',
      child: MouseRegion(
        cursor: SystemMouseCursors.click,
        onEnter: (_) => setState(() => _hovered = true),
        onExit: (_) => setState(() => _hovered = false),
        child: AnimatedOpacity(
          duration: reduceMotion
              ? Duration.zero
              : const Duration(milliseconds: 250),
          opacity: _entered || reduceMotion ? 1 : 0,
          child: AnimatedSlide(
            duration: reduceMotion
                ? Duration.zero
                : const Duration(milliseconds: 250),
            offset: _entered || reduceMotion
                ? Offset.zero
                : const Offset(0, .06),
            child: AnimatedScale(
              duration: reduceMotion
                  ? Duration.zero
                  : const Duration(milliseconds: 110),
              scale: _pressed ? .98 : (_hovered ? 1.015 : 1),
              child: AnimatedContainer(
                duration: reduceMotion
                    ? Duration.zero
                    : const Duration(milliseconds: 180),
                decoration: BoxDecoration(
                  color: AppTheme.surfaceColor,
                  borderRadius: radius,
                  border: Border.all(color: AppTheme.borderColor),
                  boxShadow: [
                    BoxShadow(
                      color: AppTheme.tileShadowColor.withValues(
                        alpha: _hovered ? .12 : .055,
                      ),
                      blurRadius: _hovered ? 18 : 12,
                      offset: Offset(0, _hovered ? 7 : 4),
                    ),
                  ],
                ),
                child: Material(
                  color: AppTheme.transparentColor,
                  borderRadius: radius,
                  child: InkWell(
                    onTap: widget.onTap,
                    onHighlightChanged: (value) =>
                        setState(() => _pressed = value),
                    borderRadius: radius,
                    child: Padding(
                      padding: EdgeInsets.all(widget.horizontal ? 18 : 16),
                      child: content,
                    ),
                  ),
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }

  Widget _titleAndSubtitle(BuildContext context) => Column(
    crossAxisAlignment: CrossAxisAlignment.start,
    mainAxisSize: MainAxisSize.min,
    children: [
      Text(
        widget.title,
        maxLines: 1,
        overflow: TextOverflow.ellipsis,
        style: Theme.of(
          context,
        ).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w600),
      ),
      const SizedBox(height: 3),
      Text(
        widget.subtitle,
        maxLines: 1,
        overflow: TextOverflow.ellipsis,
        style: Theme.of(
          context,
        ).textTheme.bodySmall?.copyWith(color: AppTheme.textSecondary),
      ),
    ],
  );
}
