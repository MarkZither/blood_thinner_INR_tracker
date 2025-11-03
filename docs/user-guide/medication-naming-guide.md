# Medication Naming Guide

## Overview

The Blood Thinner INR Tracker uses a flexible medication naming system that respects how users actually refer to their medications in real life, while maintaining medical accuracy.

## The Problem

Different medications are known by different names in everyday use:
- Some medications are commonly called by their **generic name** (e.g., "Warfarin")
- Others are better known by their **brand name** (e.g., "Sintrom" rather than "Acenocoumarol")
- Users may have their own **personal names** for medications

## The Solution

Our system stores three types of names for each medication:

### 1. Display Name (Required)
**Field:** `Name`
- This is **how YOU know the medication**
- It appears in your medication list and throughout the app
- You can enter whatever name you use: brand name, generic name, or your own custom name
- Examples: "Sintrom", "Warfarin", "My blood thinner"

### 2. Generic Name (Optional)
**Field:** `GenericName`
- The medical/scientific name of the medication
- Important for medical records and healthcare provider communication
- Auto-filled when you select a medication from our list
- Examples: "Acenocoumarol", "Warfarin Sodium", "Apixaban"

### 3. Brand Name(s) (Optional)
**Field:** `BrandName`
- The commercial/brand name(s) of the medication
- Auto-filled when you select a medication from our list
- Can include multiple brands separated by commas
- Examples: "Sintrom", "Coumadin, Jantoven", "Eliquis"

## How It Works

### Adding a New Medication

1. **Search for your medication** using the autocomplete search box
   - The dropdown shows: **Generic Name (Brand Names) - Drug Class**
   - Example: "Acenocoumarol (Sintrom) - Vitamin K Antagonist"

2. **Choose what to call it** in the "How You Know This Medication" field
   - The system suggests a user-friendly name:
     - For well-known generics like "Warfarin" → suggests "Warfarin"
     - For medications better known by brand like "Sintrom" → suggests "Sintrom"
   - You can change this to anything you prefer

3. **Review the auto-filled information**
   - Generic Name and Brand Name(s) are auto-filled from our database
   - These fields are disabled (read-only) when you select from our list
   - This ensures medical accuracy while letting you use your preferred name

### Editing an Existing Medication

When editing a medication, all three name fields are available:
- **How You Know This Medication**: Change this anytime to update the display name
- **Generic Name**: Optional field for medical accuracy
- **Brand Name(s)**: Optional field for reference

## Smart Name Suggestions

When you select a medication from our list, the system intelligently suggests a display name:

### Well-Known Generics
These medications are commonly referred to by their generic name:
- Warfarin → Suggests "Warfarin"
- Aspirin → Suggests "Aspirin"
- Heparin → Suggests "Heparin"
- Clopidogrel → Suggests "Clopidogrel"

### Brand-First Medications
These medications are better known by their brand name:
- Acenocoumarol → Suggests "Sintrom"
- Dabigatran Etexilate → Suggests "Pradaxa"
- Apixaban → Suggests "Eliquis"

### Multiple Brand Names
When a medication has multiple well-known brands:
- Defaults to generic name for consistency
- Example: "Warfarin Sodium" has brands "Coumadin, Jantoven" → Suggests "Warfarin Sodium"

## Examples

### Example 1: Acenocoumarol (Sintrom)
```
User selects: "Acenocoumarol (Sintrom) - Vitamin K Antagonist"
System suggests:
  - Display Name: "Sintrom" (because it's better known by brand)
  - Generic Name: "Acenocoumarol" (auto-filled)
  - Brand Name: "Sintrom" (auto-filled)
User can change Display Name to: "Sintrom", "Acenocoumarol", or "My blood thinner"
```

### Example 2: Warfarin
```
User selects: "Warfarin (Coumadin) - Vitamin K Antagonist"
System suggests:
  - Display Name: "Warfarin" (well-known generic)
  - Generic Name: "Warfarin" (auto-filled)
  - Brand Name: "Coumadin" (auto-filled)
User can change Display Name to: "Warfarin", "Coumadin", or any custom name
```

### Example 3: Custom Medication
```
User does NOT select from list
User enters:
  - Display Name: "My doctor's blood thinner"
  - Generic Name: (optional - can leave blank or fill in)
  - Brand Name: (optional - can leave blank or fill in)
```

## Benefits

1. **User-Friendly**: You see the name you actually use
2. **Medically Accurate**: Generic and brand names are preserved for healthcare communication
3. **Flexible**: Works for common medications, rare medications, and custom entries
4. **Consistent**: Same approach in both Add and Edit screens
5. **Smart**: Suggests the most commonly-used name, but lets you override it

## Best Practices

1. **Use the autocomplete search** when possible - it ensures accurate medical information
2. **Keep the suggested display name** if it makes sense - it's based on common usage
3. **Customize the display name** to match how you actually talk about your medication
4. **Don't worry about generic vs brand** - the system handles this for you
5. **Add notes** in the Notes field for any additional context about the medication
