import 'package:flutter/widgets.dart';

/// After `formKey.currentState!.validate()` fails, scrolls to and focuses the first field
/// showing an error, so the user lands on what needs fixing.
void focusFirstInvalidField(GlobalKey<FormState> formKey) {
  final formContext = formKey.currentContext;
  if (formContext == null) return;

  Element? invalid;
  void findInvalid(Element element) {
    if (invalid != null) return;
    if (element is StatefulElement && element.state is FormFieldState && (element.state as FormFieldState).hasError) {
      invalid = element;
      return;
    }
    element.visitChildren(findInvalid);
  }

  formContext.visitChildElements(findInvalid);
  final target = invalid;
  if (target == null) return;

  Scrollable.ensureVisible(target, duration: const Duration(milliseconds: 250), alignment: 0.2);
  void focusEditable(Element element) {
    if (element is StatefulElement && element.state is EditableTextState) {
      (element.state as EditableTextState).requestKeyboard();
      return;
    }
    element.visitChildren(focusEditable);
  }

  target.visitChildren(focusEditable);
}
