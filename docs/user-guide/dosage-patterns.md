# Dosage Pattern Feature Guide

## Overview

The **Dosage Pattern** feature enables you to manage variable medication schedules that repeat cyclically. Instead of taking the same dose every day, you can define patterns like "4mg, 4mg, 3mg" that automatically repeat, making it easier to track complex blood thinner dosing schedules.

## When to Use Patterns

### ✅ Use Patterns When:
- Your doctor prescribes different dosages on different days (e.g., "Take 4mg Monday-Friday, 3mg Saturday-Sunday")
- Your dosage alternates (e.g., "4mg one day, 3mg the next")
- You have a complex weekly or multi-week cycle
- Your INR results require frequent dosage adjustments with repeating patterns

### ❌ Use Fixed Dosage When:
- You take the same amount every day
- Your dosage rarely changes
- You prefer simple daily tracking

---

## Creating Your First Pattern

### Step 1: Navigate to Medications

From the main menu, click **Medications** to view your medication list.

### Step 2: Select or Create Medication

- **Existing medication**: Click the medication name to open details
- **New medication**: Click **Add Medication** button

### Step 3: Open Pattern Entry

Click the **Define Pattern** button (or **Change Pattern** if one already exists).

### Step 4: Enter Your Pattern

**Pattern Input Box**: Enter your dosages separated by commas.

**Examples**:
- Weekly pattern: `4, 4, 4, 4, 4, 3, 3` (Mon-Sun)
- Alternating: `4, 3, 4, 3, 4, 3`
- Complex cycle: `4.5, 4.5, 4, 4, 3.5, 3.5`

**Tips**:
- Use commas to separate values
- Decimals are supported (e.g., 4.5)
- No spaces needed (they're automatically trimmed)
- Pattern can be 1-365 days long

### Step 5: Choose Entry Mode

**Two modes available** (your default mode is set in settings):

#### Date-Based Mode
- Select the **effective date** when pattern starts
- System automatically calculates which day of the pattern applies today
- Best for: Planning future changes

#### Day-Number Mode
- Enter which **day number** you're currently on (e.g., "Day 3")
- System back-calculates the start date
- Best for: Continuing an existing pattern from your pillbox

### Step 6: Review Pattern Preview

The **Pattern Preview** shows your dosages as colored chips:
- Each chip represents one day's dosage
- Current day (if applicable) is highlighted
- Verify the pattern looks correct before saving

### Step 7: Save Pattern

Click **Save Pattern** to activate your new dosage schedule.

---

## Understanding Pattern Display

### Pattern Card

After creating a pattern, you'll see a **Pattern Card** showing:

```
Active Pattern (6 days)
[4mg] [4mg] [3mg] [4mg] [3mg] [3mg]
        ↑ You are here (Day 2)

Average: 3.5mg per day
Started: Jan 15, 2025
```

**Indicators**:
- **Blue chips**: Pattern dosages
- **Arrow**: Your current position in the cycle
- **Average**: Mean dosage per day
- **Started**: When pattern became effective

---

## Logging Doses with Patterns

### Auto-Population

When you log a dose:

1. Click **Log Dose** for your medication
2. The **dosage field is pre-filled** with today's expected amount
3. Review and adjust if needed (e.g., if you accidentally took different amount)
4. Click **Save**

**Example**:
```
Log Dose for Warfarin

Expected dosage today: 4mg (Day 1 of your pattern)

Dosage: [4.0]mg  ← Pre-filled
Taken: Today 8:30 AM
Notes: [Optional]

[Save]  [Cancel]
```

### Variance Tracking

If you log a dose different from expected:

**Variance Indicator** appears:
```
⚠️ Variance: -1.0mg (-25%)
Expected: 4mg
Actual: 3mg
```

**Color Codes**:
- 🟢 **Green**: No variance (on target)
- 🟡 **Yellow**: Minor variance (±5-20%)
- 🔴 **Red**: Significant variance (>±20%)

**Why This Matters**:
- Helps identify dosing errors
- Tracks adherence patterns
- Provides data for your healthcare provider

---

## Viewing Your Schedule

### Schedule View

To see your upcoming dosages:

1. Open medication details
2. Click **View Schedule**
3. Choose time range (14, 21, or 28 days)

**Schedule Table Shows**:
```
| Date       | Day      | Dosage | Pattern Day |
|------------|----------|--------|-------------|
| Jan 15     | Monday   | 4.0mg  | Day 1       |
| Jan 16     | Tuesday  | 4.0mg  | Day 2       |
| Jan 17     | Wednesday| 3.0mg  | Day 3       |
| Jan 18     | Thursday | 4.0mg  | Day 4       |
| ...
```

**Summary Statistics**:
- **Total Dosage**: Sum for the period (e.g., 98mg over 28 days)
- **Average Daily**: Mean dosage (e.g., 3.5mg/day)
- **Min/Max**: Dosage range (e.g., 3-4mg)
- **Pattern Cycles**: How many complete cycles (e.g., 4.67 cycles)

**Pattern Changes**:
If a new pattern starts during the schedule period, you'll see:
```
⚠️ Pattern changes on Jan 22
  Old: 3-day cycle (4, 3, 3)
  New: 6-day cycle (4, 4, 3, 4, 3, 3)
```

---

## Modifying Patterns

### When to Change Patterns

- INR test results indicate dosage adjustment needed
- Doctor prescribes a new pattern
- Side effects require dosage changes

### How to Modify

1. **Open medication** → Click **Change Pattern**
2. **Enter new pattern** (e.g., change `4, 3, 3` to `4, 4, 3`)
3. **Set effective date**:
   - **Tomorrow**: Most common choice
   - **Today**: Immediate change
   - **Past date**: Back-date if you already started the new pattern

### Backdating Confirmation

If you set the effective date **more than 7 days in the past**, you'll see:

```
⚠️ Confirm Backdated Pattern

The pattern start date is 10 days in the past (Jan 5, 2025).
This will affect historical medication logs.
Are you sure you want to continue?

[Yes, Continue]  [Cancel]
```

**Important**: Backdating changes how variance is calculated for past logs. Only backdate if you actually started this pattern in the past.

### Pattern History

View your pattern change timeline:

1. Open medication details
2. Click **Pattern History**
3. See chronological list:

```
Active Pattern (Jan 15 - Present)
[4mg] [4mg] [3mg] [4mg] [3mg] [3mg]
Average: 3.5mg

Previous Pattern (Dec 1 - Jan 14)
[4mg] [3mg] [3mg]
Average: 3.33mg

Previous Pattern (Nov 1 - Nov 30)
[5mg] [5mg] [4mg]
Average: 4.67mg
```

---

## Validation and Warnings

The system provides helpful guidance:

### Single-Value Pattern Warning
```
⚠️ Single-Value Pattern Detected

You entered only one dosage value (4mg).
Did you mean to use a fixed daily dose instead of a repeating pattern?

A pattern with one value will repeat the same dose every day.

[Yes, Continue with Pattern]  [No, Cancel]
```
**Recommendation**: Use fixed dosage for single values (simpler).

### Long Pattern Warning
```
⚠️ Long Pattern Detected

Your pattern is 25 days long, which is unusually long.
Most dosage patterns are 7-20 days.
Please verify this is correct before continuing.

[Continue with Pattern]  [Edit Pattern]
```
**Recommendation**: Double-check for input errors (extra commas, typos).

### High Dosage Warning
```
⚠️ High Dosage Warning

Your pattern contains 25mg of Warfarin, which exceeds typical maximum (20mg).
Please verify this dosage with your healthcare provider before continuing.

[I Understand, Continue]  [Edit Dosage]
```
**Recommendation**: Confirm with doctor before saving high dosages.

### Invalid Format Errors
```
❌ Invalid dosage value: 'abc'
Please use numeric values only.

❌ Dosage 1500 is out of range (0.1-1000mg)

❌ Pattern cannot exceed 365 days
You entered 400 values. Maximum is 365.
```

---

## Tips and Best Practices

### 📝 Pattern Entry Tips

1. **Use your pill organizer**: Count the dosages in your weekly pillbox and enter them in order
2. **Start on Monday**: Align patterns with weekly schedules for easier tracking
3. **Double-check commas**: Extra commas create empty values
4. **Verify the preview**: Always check the chip display before saving

### 📅 Effective Date Tips

1. **Default to tomorrow**: Changes usually start the next day
2. **Today for immediate**: Only if you're starting the new pattern right now
3. **Avoid backdating**: Unless you've already been following the new pattern

### 📊 Adherence Tips

1. **Log daily**: Record doses as soon as you take them
2. **Review variance**: Check weekly for dosing errors
3. **Share with doctor**: Show variance reports at appointments
4. **Set reminders**: Use phone alarms to maintain consistency

### 🔄 Pattern Management Tips

1. **Keep it simple**: Shorter patterns are easier to follow
2. **Update after INR tests**: Create new pattern when doctor adjusts dosage
3. **Add notes**: Document why pattern changed (e.g., "INR 2.8, doctor reduced dose")
4. **Review history**: Look for patterns in dosage changes over time

---

## Common Scenarios

### Scenario 1: Weekly Alternating Pattern

**Situation**: Doctor prescribes 4mg Mon-Fri, 3mg Sat-Sun

**Solution**:
```
Pattern: 4, 4, 4, 4, 4, 3, 3
Start: Next Monday
```

### Scenario 2: Every-Other-Day Alternating

**Situation**: Alternate between 4mg and 3mg daily

**Solution**:
```
Pattern: 4, 3, 4, 3, 4, 3
Start: Tomorrow
```

### Scenario 3: Complex Cycle

**Situation**: Doctor prescribes a 6-day repeating cycle

**Solution**:
```
Pattern: 4.5, 4.5, 4, 4, 3.5, 3.5
Start: Tomorrow
```

### Scenario 4: Mid-Cycle Adjustment

**Situation**: You're on Day 3 of a pattern when doctor changes it

**Solution**:
1. Use **Day-Number Mode**
2. Enter new pattern
3. Set current day number: 3
4. System calculates start date automatically

### Scenario 5: Correcting a Mistake

**Situation**: You entered wrong pattern and already logged doses

**Solution**:
1. Create correct pattern
2. Set effective date to when you should have started it
3. Confirm backdating warning
4. System recalculates variance for affected logs

---

## Troubleshooting

### "No active pattern" message

**Cause**: Medication doesn't have a current pattern  
**Solution**: Create a pattern or use fixed daily dosage

### Pattern preview doesn't match my input

**Cause**: Extra commas, spaces, or typos  
**Solution**: Review input carefully, remove extra punctuation

### Expected dosage is wrong when logging

**Cause**: Pattern effective date may be incorrect  
**Solution**: Check pattern start date in Pattern History

### Variance appears when I took correct dose

**Cause**: Pattern may have changed recently  
**Solution**: Verify which pattern was active on that date

### Can't save pattern (validation error)

**Cause**: Dosage out of range or invalid format  
**Solution**: Check error message, correct values to 0.1-1000mg range

---

## Feature Compatibility

### Non-Daily Medications

If your medication is **"Every other day"** or **weekly**:
- Pattern applies to **scheduled days only**
- Non-scheduled days show "No dose" in schedule
- Example: Pattern [4, 3, 3] for every-other-day means:
  - Day 1 (scheduled): 4mg
  - Day 2 (skip): No dose
  - Day 3 (scheduled): 3mg

### Mobile App

Pattern features are fully supported in the mobile app:
- Create/edit patterns
- View schedule
- Log doses with auto-population
- All features synchronized with web app

---

## Privacy and Data

- **Your patterns are private**: Only you and authorized healthcare providers can see them
- **Cloud sync**: Patterns sync across your devices
- **Export capability**: Download pattern history for doctor appointments
- **No sharing without consent**: Patterns are never shared with third parties

---

## Getting Help

### Need Assistance?

- **In-app help**: Click the ℹ️ icon on any screen
- **Documentation**: Visit our support center
- **Video tutorials**: Watch step-by-step guides
- **Contact support**: Email support@bloodthinnertracker.com

### Medical Questions?

⚠️ **Always consult your healthcare provider for medical advice.**

This app is a tracking tool, not a substitute for medical guidance. If you have questions about:
- Dosage amounts
- Pattern complexity
- INR target ranges
- Side effects or symptoms

**Contact your doctor, pharmacist, or anticoagulation clinic immediately.**

---

## Medical Disclaimer

⚠️ **Important Medical Disclaimer**

This dosage pattern feature is provided for informational and record-keeping purposes only. It is not intended to replace professional medical advice, diagnosis, or treatment.

- Always follow your healthcare provider's instructions
- Never adjust your medication without consulting your doctor
- Seek immediate medical attention if you experience unusual bleeding or bruising
- Keep all scheduled INR appointments
- Report any missed doses or dosing errors to your healthcare provider

---

**Questions? Feedback? Contact us at support@bloodthinnertracker.com**
