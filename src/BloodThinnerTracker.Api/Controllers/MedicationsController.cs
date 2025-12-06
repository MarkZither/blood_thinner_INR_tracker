// MEDICATIONS CONTROLLER MOVED TO Controllers/V1
// This file was retained as a move marker to preserve history. The controller implementation
// now lives in `src/BloodThinnerTracker.Api/Controllers/V1/MedicationsController.cs`.
// Delete this file if you prefer a physical move instead of a marker.


            if (!string.IsNullOrWhiteSpace(request.DosageUnit))
                medication.DosageUnit = request.DosageUnit.Trim();

            medication.Form = request.Form?.Trim() ?? medication.Form;
            medication.Color = request.Color?.Trim();
            medication.Shape = request.Shape?.Trim();
            medication.Imprint = request.Imprint?.Trim();

            if (request.IsBloodThinner.HasValue)
                medication.IsBloodThinner = request.IsBloodThinner.Value;

            if (request.IsActive.HasValue)
                medication.IsActive = request.IsActive.Value;

            if (request.StartDate.HasValue)
                medication.StartDate = request.StartDate.Value;

            medication.EndDate = request.EndDate;
            medication.PrescribedBy = request.PrescribedBy?.Trim();
            medication.Pharmacy = request.Pharmacy?.Trim();
            medication.Instructions = request.Instructions?.Trim();
            medication.SideEffects = request.SideEffects?.Trim();
            medication.Contraindications = request.Contraindications?.Trim();
            medication.StorageInstructions = request.StorageInstructions?.Trim();

            if (request.MaxDailyDose.HasValue)
                medication.MaxDailyDose = request.MaxDailyDose.Value;

            if (request.MinHoursBetweenDoses.HasValue)
                medication.MinHoursBetweenDoses = Math.Max(1, request.MinHoursBetweenDoses.Value);

            if (request.RequiresINRMonitoring.HasValue)
                medication.RequiresINRMonitoring = request.RequiresINRMonitoring.Value;

            medication.INRTargetMin = request.INRTargetMin;
            medication.INRTargetMax = request.INRTargetMax;
            medication.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var response = new MedicationResponse
            {
                PublicId = medication.PublicId,
                Name = medication.Name,
                BrandName = medication.BrandName,
                GenericName = medication.GenericName,
                Dosage = medication.Dosage,
                DosageUnit = medication.DosageUnit,
                Type = medication.Type,
                Frequency = medication.Frequency,
                Form = medication.Form,
                Color = medication.Color,
                Shape = medication.Shape,
                Imprint = medication.Imprint,
                IsBloodThinner = medication.IsBloodThinner,
                IsActive = medication.IsActive,
                StartDate = medication.StartDate,
                EndDate = medication.EndDate,
                PrescribedBy = medication.PrescribedBy,
                Pharmacy = medication.Pharmacy,
                Instructions = medication.Instructions,
                SideEffects = medication.SideEffects,
                Contraindications = medication.Contraindications,
                StorageInstructions = medication.StorageInstructions,
                MaxDailyDose = medication.MaxDailyDose,
                MinHoursBetweenDoses = medication.MinHoursBetweenDoses,
                RequiresINRMonitoring = medication.RequiresINRMonitoring,
                INRTargetMin = medication.INRTargetMin,
                INRTargetMax = medication.INRTargetMax,
                CreatedAt = medication.CreatedAt,
                UpdatedAt = medication.UpdatedAt
            };

            _logger.LogInformation("Medication updated successfully: {MedicationPublicId} for user {UserPublicId}", publicId, userPublicId);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating medication {MedicationPublicId}", publicId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                "An error occurred while updating the medication");
        }
    }

    /// <summary>
    /// Deactivates a medication (soft delete).
    /// </summary>
    /// <param name="publicId">Medication public ID.</param>
    /// <param name="request">Deactivation request with reason.</param>
    /// <returns>Confirmation of medication deactivation.</returns>
    /// <response code="200">Medication deactivated successfully.</response>
    /// <response code="400">Invalid deactivation request.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="404">Medication not found.</response>
    [HttpPost("{id}/deactivate")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> DeactivateMedication([FromRoute(Name = "id")] Guid publicId, [FromBody] DeactivateMedicationRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for medication deactivation: {Errors}",
                    string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return BadRequest(ModelState);
            }

            var userPublicId = GetCurrentUserPublicId();
            if (userPublicId == null)
            {
                _logger.LogWarning("Attempted to deactivate medication with invalid user ID");
                return Unauthorized("Invalid user authentication");
            }

            var medication = await _context.Medications
                .Include(m => m.User)
                .FirstOrDefaultAsync(m => m.PublicId == publicId && m.User.PublicId == userPublicId.Value && !m.IsDeleted);

            if (medication == null)
            {
                _logger.LogWarning("Medication not found for deactivation: {MedicationPublicId} for user {UserPublicId}", publicId, userPublicId);
                return NotFound("Medication not found");
            }

            // Deactivate the medication
            medication.IsActive = false;
            medication.EndDate = DateTime.UtcNow.Date;
            medication.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Medication deactivated: {MedicationPublicId} for user {UserPublicId}, Reason: {Reason}",
                publicId, userPublicId, request.Reason);

            return Ok(new ApiResponse
            {
                Success = true,
                Message = "Medication has been deactivated successfully. Historical data has been preserved.",
                Data = new { DeactivatedAt = medication.UpdatedAt, EndDate = medication.EndDate }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating medication {MedicationPublicId}", publicId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                "An error occurred while deactivating the medication");
        }
    }

    /// <summary>
    /// Gets blood thinner medications for the current user.
    /// </summary>
    /// <returns>List of blood thinner medications.</returns>
    /// <response code="200">Blood thinner medications retrieved successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    [HttpGet("blood-thinners")]
    [ProducesResponseType(typeof(List<MedicationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<MedicationResponse>>> GetBloodThinners()
    {
        try
        {
            var userPublicId = GetCurrentUserPublicId();
            if (userPublicId == null)
            {
                _logger.LogWarning("Attempted to get blood thinners with invalid user ID");
                return Unauthorized("Invalid user authentication");
            }

            var bloodThinners = await _context.Medications
                .Include(m => m.User)
                .Where(m => m.User.PublicId == userPublicId.Value && !m.IsDeleted && m.IsBloodThinner && m.IsActive)
                .OrderBy(m => m.Name)
                .Select(m => new MedicationResponse
                {
                    PublicId = m.PublicId,
                    Name = m.Name,
                    BrandName = m.BrandName,
                    GenericName = m.GenericName,
                    Dosage = m.Dosage,
                    DosageUnit = m.DosageUnit,
                    Type = m.Type,
                    Frequency = m.Frequency,
                    Form = m.Form ?? string.Empty,
                    Color = m.Color,
                    Shape = m.Shape,
                    Imprint = m.Imprint,
                    IsBloodThinner = m.IsBloodThinner,
                    IsActive = m.IsActive,
                    StartDate = m.StartDate,
                    EndDate = m.EndDate,
                    PrescribedBy = m.PrescribedBy,
                    Pharmacy = m.Pharmacy,
                    Instructions = m.Instructions,
                    SideEffects = m.SideEffects,
                    Contraindications = m.Contraindications,
                    StorageInstructions = m.StorageInstructions,
                    MaxDailyDose = m.MaxDailyDose,
                    MinHoursBetweenDoses = m.MinHoursBetweenDoses,
                    RequiresINRMonitoring = m.RequiresINRMonitoring,
                    INRTargetMin = m.INRTargetMin,
                    INRTargetMax = m.INRTargetMax,
                    CreatedAt = m.CreatedAt,
                    UpdatedAt = m.UpdatedAt
                })
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} blood thinner medications for user {UserPublicId}", bloodThinners.Count, userPublicId);
            return Ok(bloodThinners);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving blood thinner medications");
            return StatusCode(StatusCodes.Status500InternalServerError,
                "An error occurred while retrieving blood thinner medications");
        }
    }

    /// <summary>
    /// Validates medication safety parameters for creation.
    /// </summary>
    /// <param name="request">Medication creation request.</param>
    /// <returns>Validation result with any safety concerns.</returns>
    private static ValidationResult ValidateMedicationSafety(CreateMedicationRequest request)
    {
        var errors = new List<string>();

        // Blood thinner specific validations
        if (request.IsBloodThinner)
        {
            if (request.MinHoursBetweenDoses < 12)
            {
                errors.Add("Blood thinners require minimum 12 hours between doses for safety");
            }

            // Warfarin (VitKAntagonist) specific validations
            if (request.Type == MedicationType.VitKAntagonist)
            {
                if (request.MaxDailyDose > 20)
                {
                    errors.Add("Warfarin dosage above 20mg requires special attention. Consult healthcare provider.");
                }

                // Warfarin MUST have INR monitoring
                if (request.RequiresINRMonitoring == false)
                {
                    errors.Add("Warfarin (Vitamin K Antagonist) requires INR monitoring. This cannot be disabled.");
                }

                // Warfarin should have INR targets set
                if (!request.INRTargetMin.HasValue || !request.INRTargetMax.HasValue)
                {
                    errors.Add("Warfarin requires INR target range to be set (typical: 2.0-3.0)");
                }
            }

            // INR range validation (when specified)
            if (request.INRTargetMin.HasValue && request.INRTargetMax.HasValue)
            {
                if (request.INRTargetMin.Value < 0.5m || request.INRTargetMax.Value > 8.0m)
                {
                    errors.Add("INR target range must be between 0.5 and 8.0");
                }

                if (request.INRTargetMin.Value >= request.INRTargetMax.Value)
                {
                    errors.Add("INR target minimum must be less than maximum");
                }
            }
        }

        // General medication validations
        if (request.Dosage <= 0)
        {
            errors.Add("Medication dosage must be greater than 0");
        }

        if (request.MaxDailyDose <= 0)
        {
            errors.Add("Maximum daily dose must be greater than 0");
        }

        if (request.MinHoursBetweenDoses < 1)
        {
            errors.Add("Minimum hours between doses must be at least 1 hour");
        }

        return new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors
        };
    }

    /// <summary>
    /// Validates medication safety parameters for updates.
    /// </summary>
    /// <param name="request">Medication update request.</param>
    /// <param name="existingMedication">Existing medication data.</param>
    /// <returns>Validation result with any safety concerns.</returns>
    private static ValidationResult ValidateMedicationUpdateSafety(UpdateMedicationRequest request, Medication existingMedication)
    {
        var errors = new List<string>();

        // Check if this is a blood thinner (existing or being changed to one)
        var isBloodThinner = request.IsBloodThinner ?? existingMedication.IsBloodThinner;
        var medicationType = existingMedication.Type; // Type cannot be changed via update

        if (isBloodThinner)
        {
            var minHours = request.MinHoursBetweenDoses ?? existingMedication.MinHoursBetweenDoses;
            if (minHours < 12)
            {
                errors.Add("Blood thinners require minimum 12 hours between doses for safety");
            }

            // Warfarin (VitKAntagonist) specific validations
            if (medicationType == MedicationType.VitKAntagonist)
            {
                var maxDose = request.MaxDailyDose ?? existingMedication.MaxDailyDose;
                if (maxDose > 20)
                {
                    errors.Add("Warfarin dosage above 20mg requires special attention. Consult healthcare provider.");
                }

                // Warfarin MUST have INR monitoring
                var requiresINR = request.RequiresINRMonitoring ?? existingMedication.RequiresINRMonitoring;
                if (!requiresINR)
                {
                    errors.Add("Warfarin (Vitamin K Antagonist) requires INR monitoring. This cannot be disabled.");
                }
            }

            // INR range validation (when specified)
            var inrMin = request.INRTargetMin ?? existingMedication.INRTargetMin;
            var inrMax = request.INRTargetMax ?? existingMedication.INRTargetMax;

            if (inrMin.HasValue && inrMax.HasValue)
            {
                if (inrMin.Value < 0.5m || inrMax.Value > 8.0m)
                {
                    errors.Add("INR target range must be between 0.5 and 8.0");
                }

                if (inrMin.Value >= inrMax.Value)
                {
                    errors.Add("INR target minimum must be less than maximum");
                }
            }
        }

        // General validations
        if (request.Dosage.HasValue && request.Dosage.Value <= 0)
        {
            errors.Add("Medication dosage must be greater than 0");
        }

        if (request.MaxDailyDose.HasValue && request.MaxDailyDose.Value <= 0)
        {
            errors.Add("Maximum daily dose must be greater than 0");
        }

        if (request.MinHoursBetweenDoses.HasValue && request.MinHoursBetweenDoses.Value < 1)
        {
            errors.Add("Minimum hours between doses must be at least 1 hour");
        }

        return new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors
        };
    }

    /// <summary>
    /// Gets the current user's public ID (GUID) from JWT claims.
    /// ⚠️ SECURITY: JWT claims contain PublicId (GUID), never internal database Id.
    /// </summary>
    /// <returns>Current user's public GUID or null if not authenticated.</returns>
    private Guid? GetCurrentUserPublicId()
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                        User.FindFirst("sub")?.Value ??
                        User.FindFirst("userId")?.Value;

        if (string.IsNullOrEmpty(userIdStr))
            return null;

        return Guid.TryParse(userIdStr, out var guid) ? guid : null;
    }

}

