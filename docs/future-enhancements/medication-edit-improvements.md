# Medication Edit Screen Improvements

**Status**: Planned  
**Priority**: Medium  
**Category**: User Experience / Data Model

## Current Behavior

The medication edit screen (`MedicationEdit.razor`) currently allows full editing of all medication fields including:
- Medication name (Generic/Brand)
- Medication type
- Dosage and units
- Frequency
- Schedule
- Notes

## Issue

Medications in the system are intended to be selected from a hardcoded/predefined list of blood thinner medications (e.g., Warfarin, Coumadin, Heparin, Apixaban, etc.). Users should not be able to arbitrarily edit the medication name or brand since these come from a curated medical database.

## Proposed Improvement

### Phase 1: Make Names Read-Only
- Make the Generic Name and Brand Name fields **read-only** in the edit screen
- Only allow editing of:
  - Dosage amount
  - Dosage unit
  - Frequency
  - Schedule times
  - Notes and instructions

### Phase 2: Implement Medication Master List
- Create a master list of FDA-approved blood thinner medications
- Implement medication selection during "Add Medication" from predefined list
- Store medication template data (name, brand, type, typical dosages, warnings)
- Allow user customization only for dose, schedule, and personal notes

### Phase 3: Enhanced Features
- Add medication interaction warnings
- Implement dosage validation based on medication type
- Add visual pill identification (color, shape, imprint)
- Support multiple brand names per generic medication

## Implementation Notes

- This change requires updates to:
  - `MedicationEdit.razor` - Make name fields read-only
  - `MedicationAdd.razor` - Change to selection-based instead of free-text
  - Database schema - May need a separate `MedicationCatalog` table
  - API endpoints - Add medication catalog lookup endpoints

## Related Issues

- Originated from PR #7 review comment: https://github.com/MarkZither/blood_thinner_INR_tracker/pull/7#discussion_r2485617923
- User: @MarkZither noted this as a design limitation to be addressed later

## Medical Safety Considerations

- Predefined medication list ensures accurate drug information
- Reduces user error in medication tracking
- Enables better drug interaction checking
- Supports proper INR monitoring requirements per medication type

## Timeline

- **Not scheduled for immediate implementation**
- Track as technical debt / enhancement
- Review during next major UI/UX improvement cycle
