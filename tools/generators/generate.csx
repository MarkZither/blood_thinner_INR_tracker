#!/usr/bin/env dotnet-script
#r "nuget: System.Text.Json, 8.0.0"

using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;

/*
 * Blood Thinner Tracker - Code Generator
 * Generates boilerplate code for medical entities, controllers, and services
 */

// Medical Disclaimer
Console.WriteLine("⚠️  MEDICAL CODE GENERATOR DISCLAIMER ⚠️");
Console.WriteLine("Generated code handles medical data and must be reviewed for compliance.");
Console.WriteLine("Always validate medical business rules and safety constraints.");
Console.WriteLine();

public class CodeGenerator
{
    private readonly string _projectRoot;
    private readonly string _templatesPath;
    
    public CodeGenerator()
    {
        _projectRoot = Directory.GetCurrentDirectory();
        _templatesPath = Path.Combine(_projectRoot, "tools", "generators", "templates");
        
        // Create templates directory if it doesn't exist
        Directory.CreateDirectory(_templatesPath);
    }
    
    // Generate Entity class
    public void GenerateEntity(string entityName, Dictionary<string, string> properties)
    {
        var template = @"using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BloodThinnerTracker.Shared.Models;

/// <summary>
/// Represents a {{EntityName}} entity in the blood thinner tracking system.
/// 
/// ⚠️ MEDICAL DATA DISCLAIMER:
/// This class handles medical information and must comply with healthcare data protection regulations.
/// Always validate medical data constraints and ensure proper encryption for sensitive information.
/// </summary>
[Table(""{{TableName}}"")]
public class {{EntityName}}
{
    /// <summary>
    /// Gets or sets the unique identifier for this {{EntityName}}.
    /// </summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

{{Properties}}

    /// <summary>
    /// Gets or sets when this record was created.
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets when this record was last updated.
    /// </summary>
    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the user ID who owns this record.
    /// </summary>
    [Required]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Soft delete flag for medical data retention compliance.
    /// </summary>
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// Gets or sets when this record was soft deleted.
    /// </summary>
    public DateTime? DeletedAt { get; set; }
}";

        var propertiesBuilder = new StringBuilder();
        foreach (var prop in properties)
        {
            propertiesBuilder.AppendLine(GenerateProperty(prop.Key, prop.Value));
        }

        var content = template
            .Replace("{{EntityName}}", entityName)
            .Replace("{{TableName}}", ToPlural(entityName))
            .Replace("{{Properties}}", propertiesBuilder.ToString());

        var filePath = Path.Combine(_projectRoot, "src", "BloodThinnerTracker.Shared", "Models", $"{entityName}.cs");
        Directory.CreateDirectory(Path.GetDirectoryName(filePath));
        File.WriteAllText(filePath, content);
        
        Console.WriteLine($"✅ Generated entity: {filePath}");
    }
    
    // Generate Controller
    public void GenerateController(string entityName)
    {
        var template = @"using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using BloodThinnerTracker.Shared.Models;
using BloodThinnerTracker.Api.Data;
using System.Security.Claims;

namespace BloodThinnerTracker.Api.Controllers;

/// <summary>
/// API controller for managing {{EntityName}} entities.
/// 
/// ⚠️ MEDICAL API DISCLAIMER:
/// This controller handles medical data and implements security measures required for healthcare applications.
/// All endpoints require authentication and implement proper data validation.
/// </summary>
[ApiController]
[Route(""api/[controller]"")]
[Authorize]
[Produces(""application/json"")]
public class {{EntityName}}Controller : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<{{EntityName}}Controller> _logger;

    public {{EntityName}}Controller(ApplicationDbContext context, ILogger<{{EntityName}}Controller> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets all {{EntityNameLower}} records for the authenticated user.
    /// </summary>
    /// <returns>List of {{EntityNameLower}} records</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<{{EntityName}}>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<{{EntityName}}>>> Get{{EntityNamePlural}}()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(""User ID not found in token"");
        }

        var {{entityNameLowerPlural}} = await _context.{{EntityNamePlural}}
            .Where(x => x.UserId == userId && !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        _logger.LogInformation(""Retrieved {Count} {{entityNameLower}} records for user {UserId}"", {{entityNameLowerPlural}}.Count, userId);
        
        return Ok({{entityNameLowerPlural}});
    }

    /// <summary>
    /// Gets a specific {{EntityNameLower}} record by ID.
    /// </summary>
    /// <param name=""id"">The {{EntityNameLower}} ID</param>
    /// <returns>The {{EntityNameLower}} record</returns>
    [HttpGet(""{id:guid}"")]
    [ProducesResponseType(typeof({{EntityName}}), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<{{EntityName}}>> Get{{EntityName}}(Guid id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        var {{entityNameLower}} = await _context.{{EntityNamePlural}}
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId && !x.IsDeleted);

        if ({{entityNameLower}} == null)
        {
            _logger.LogWarning(""{{EntityName}} {Id} not found for user {UserId}"", id, userId);
            return NotFound();
        }

        return Ok({{entityNameLower}});
    }

    /// <summary>
    /// Creates a new {{EntityNameLower}} record.
    /// </summary>
    /// <param name=""{{entityNameLower}}"">The {{EntityNameLower}} data</param>
    /// <returns>The created {{EntityNameLower}} record</returns>
    [HttpPost]
    [ProducesResponseType(typeof({{EntityName}}), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<{{EntityName}}>> Create{{EntityName}}({{EntityName}} {{entityNameLower}})
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(""User ID not found in token"");
        }

        // Ensure user can only create records for themselves
        {{entityNameLower}}.UserId = userId;
        {{entityNameLower}}.Id = Guid.NewGuid();
        {{entityNameLower}}.CreatedAt = DateTime.UtcNow;
        {{entityNameLower}}.UpdatedAt = DateTime.UtcNow;

        // TODO: Add medical data validation here
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        _context.{{EntityNamePlural}}.Add({{entityNameLower}});
        await _context.SaveChangesAsync();

        _logger.LogInformation(""Created {{EntityNameLower}} {Id} for user {UserId}"", {{entityNameLower}}.Id, userId);

        return CreatedAtAction(nameof(Get{{EntityName}}), new { id = {{entityNameLower}}.Id }, {{entityNameLower}});
    }

    /// <summary>
    /// Updates an existing {{EntityNameLower}} record.
    /// </summary>
    /// <param name=""id"">The {{EntityNameLower}} ID</param>
    /// <param name=""{{entityNameLower}}"">The updated {{EntityNameLower}} data</param>
    /// <returns>No content on success</returns>
    [HttpPut(""{id:guid}"")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Update{{EntityName}}(Guid id, {{EntityName}} {{entityNameLower}})
    {
        if (id != {{entityNameLower}}.Id)
        {
            return BadRequest(""ID mismatch"");
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        var existing = await _context.{{EntityNamePlural}}
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId && !x.IsDeleted);

        if (existing == null)
        {
            return NotFound();
        }

        // Preserve audit fields
        {{entityNameLower}}.UserId = existing.UserId;
        {{entityNameLower}}.CreatedAt = existing.CreatedAt;
        {{entityNameLower}}.UpdatedAt = DateTime.UtcNow;

        // TODO: Add medical data validation here
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        _context.Entry(existing).CurrentValues.SetValues({{entityNameLower}});
        await _context.SaveChangesAsync();

        _logger.LogInformation(""Updated {{EntityNameLower}} {Id} for user {UserId}"", id, userId);

        return NoContent();
    }

    /// <summary>
    /// Soft deletes a {{EntityNameLower}} record.
    /// </summary>
    /// <param name=""id"">The {{EntityNameLower}} ID</param>
    /// <returns>No content on success</returns>
    [HttpDelete(""{id:guid}"")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Delete{{EntityName}}(Guid id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        var {{entityNameLower}} = await _context.{{EntityNamePlural}}
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId && !x.IsDeleted);

        if ({{entityNameLower}} == null)
        {
            return NotFound();
        }

        // Soft delete for medical data retention compliance
        {{entityNameLower}}.IsDeleted = true;
        {{entityNameLower}}.DeletedAt = DateTime.UtcNow;
        {{entityNameLower}}.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation(""Soft deleted {{EntityNameLower}} {Id} for user {UserId}"", id, userId);

        return NoContent();
    }
}";

        var content = template
            .Replace("{{EntityName}}", entityName)
            .Replace("{{EntityNameLower}}", ToLowerFirst(entityName))
            .Replace("{{EntityNamePlural}}", ToPlural(entityName))
            .Replace("{{entityNameLower}}", ToLowerFirst(entityName))
            .Replace("{{entityNameLowerPlural}}", ToLowerFirst(ToPlural(entityName)));

        var filePath = Path.Combine(_projectRoot, "src", "BloodThinnerTracker.Api", "Controllers", $"{entityName}Controller.cs");
        Directory.CreateDirectory(Path.GetDirectoryName(filePath));
        File.WriteAllText(filePath, content);
        
        Console.WriteLine($"✅ Generated controller: {filePath}");
    }
    
    // Generate Service interface and implementation
    public void GenerateService(string entityName)
    {
        // Interface
        var interfaceTemplate = @"using BloodThinnerTracker.Shared.Models;

namespace BloodThinnerTracker.Api.Services;

/// <summary>
/// Service interface for {{EntityName}} business logic.
/// 
/// ⚠️ MEDICAL SERVICE DISCLAIMER:
/// This service handles medical data and implements business rules for healthcare applications.
/// All methods must validate medical constraints and ensure data integrity.
/// </summary>
public interface I{{EntityName}}Service
{
    /// <summary>
    /// Gets all {{EntityNameLower}} records for a user with medical validation.
    /// </summary>
    Task<IEnumerable<{{EntityName}}>> GetUser{{EntityNamePlural}}Async(string userId);
    
    /// <summary>
    /// Gets a specific {{EntityNameLower}} record with access control.
    /// </summary>
    Task<{{EntityName}}?> Get{{EntityName}}Async(Guid id, string userId);
    
    /// <summary>
    /// Creates a new {{EntityNameLower}} record with medical validation.
    /// </summary>
    Task<{{EntityName}}> Create{{EntityName}}Async({{EntityName}} {{entityNameLower}}, string userId);
    
    /// <summary>
    /// Updates an existing {{EntityNameLower}} record with medical validation.
    /// </summary>
    Task<{{EntityName}}?> Update{{EntityName}}Async({{EntityName}} {{entityNameLower}}, string userId);
    
    /// <summary>
    /// Soft deletes a {{EntityNameLower}} record for compliance.
    /// </summary>
    Task<bool> Delete{{EntityName}}Async(Guid id, string userId);
    
    /// <summary>
    /// Validates medical business rules for {{EntityNameLower}} data.
    /// </summary>
    Task<(bool IsValid, List<string> Errors)> ValidateMedical{{EntityName}}Async({{EntityName}} {{entityNameLower}});
}";

        var interfaceContent = interfaceTemplate
            .Replace("{{EntityName}}", entityName)
            .Replace("{{EntityNameLower}}", ToLowerFirst(entityName))
            .Replace("{{EntityNamePlural}}", ToPlural(entityName))
            .Replace("{{entityNameLower}}", ToLowerFirst(entityName));

        var interfaceFilePath = Path.Combine(_projectRoot, "src", "BloodThinnerTracker.Api", "Services", $"I{entityName}Service.cs");
        Directory.CreateDirectory(Path.GetDirectoryName(interfaceFilePath));
        File.WriteAllText(interfaceFilePath, interfaceContent);
        
        Console.WriteLine($"✅ Generated service interface: {interfaceFilePath}");

        // Implementation
        var implementationTemplate = @"using Microsoft.EntityFrameworkCore;
using BloodThinnerTracker.Shared.Models;
using BloodThinnerTracker.Api.Data;

namespace BloodThinnerTracker.Api.Services;

/// <summary>
/// Service implementation for {{EntityName}} business logic.
/// 
/// ⚠️ MEDICAL SERVICE IMPLEMENTATION:
/// This service implements medical business rules and data validation.
/// All operations include proper error handling and audit logging.
/// </summary>
public class {{EntityName}}Service : I{{EntityName}}Service
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<{{EntityName}}Service> _logger;

    public {{EntityName}}Service(ApplicationDbContext context, ILogger<{{EntityName}}Service> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<{{EntityName}}>> GetUser{{EntityNamePlural}}Async(string userId)
    {
        if (string.IsNullOrEmpty(userId))
            throw new ArgumentException(""User ID cannot be null or empty"", nameof(userId));

        var {{entityNameLowerPlural}} = await _context.{{EntityNamePlural}}
            .Where(x => x.UserId == userId && !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        _logger.LogInformation(""Retrieved {Count} {{entityNameLower}} records for user {UserId}"", {{entityNameLowerPlural}}.Count, userId);
        
        return {{entityNameLowerPlural}};
    }

    public async Task<{{EntityName}}?> Get{{EntityName}}Async(Guid id, string userId)
    {
        if (string.IsNullOrEmpty(userId))
            throw new ArgumentException(""User ID cannot be null or empty"", nameof(userId));

        var {{entityNameLower}} = await _context.{{EntityNamePlural}}
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId && !x.IsDeleted);

        if ({{entityNameLower}} == null)
        {
            _logger.LogWarning(""{{EntityName}} {Id} not found for user {UserId}"", id, userId);
        }

        return {{entityNameLower}};
    }

    public async Task<{{EntityName}}> Create{{EntityName}}Async({{EntityName}} {{entityNameLower}}, string userId)
    {
        if ({{entityNameLower}} == null)
            throw new ArgumentNullException(nameof({{entityNameLower}}));
        
        if (string.IsNullOrEmpty(userId))
            throw new ArgumentException(""User ID cannot be null or empty"", nameof(userId));

        // Validate medical business rules
        var (isValid, errors) = await ValidateMedical{{EntityName}}Async({{entityNameLower}});
        if (!isValid)
        {
            var errorMessage = string.Join("", "", errors);
            _logger.LogWarning(""Medical validation failed for {{EntityName}}: {Errors}"", errorMessage);
            throw new InvalidOperationException($""Medical validation failed: {errorMessage}"");
        }

        // Set audit fields
        {{entityNameLower}}.Id = Guid.NewGuid();
        {{entityNameLower}}.UserId = userId;
        {{entityNameLower}}.CreatedAt = DateTime.UtcNow;
        {{entityNameLower}}.UpdatedAt = DateTime.UtcNow;
        {{entityNameLower}}.IsDeleted = false;

        _context.{{EntityNamePlural}}.Add({{entityNameLower}});
        await _context.SaveChangesAsync();

        _logger.LogInformation(""Created {{EntityName}} {Id} for user {UserId}"", {{entityNameLower}}.Id, userId);

        return {{entityNameLower}};
    }

    public async Task<{{EntityName}}?> Update{{EntityName}}Async({{EntityName}} {{entityNameLower}}, string userId)
    {
        if ({{entityNameLower}} == null)
            throw new ArgumentNullException(nameof({{entityNameLower}}));
        
        if (string.IsNullOrEmpty(userId))
            throw new ArgumentException(""User ID cannot be null or empty"", nameof(userId));

        var existing = await _context.{{EntityNamePlural}}
            .FirstOrDefaultAsync(x => x.Id == {{entityNameLower}}.Id && x.UserId == userId && !x.IsDeleted);

        if (existing == null)
        {
            _logger.LogWarning(""{{EntityName}} {Id} not found for update by user {UserId}"", {{entityNameLower}}.Id, userId);
            return null;
        }

        // Validate medical business rules
        var (isValid, errors) = await ValidateMedical{{EntityName}}Async({{entityNameLower}});
        if (!isValid)
        {
            var errorMessage = string.Join("", "", errors);
            _logger.LogWarning(""Medical validation failed for {{EntityName}} update: {Errors}"", errorMessage);
            throw new InvalidOperationException($""Medical validation failed: {errorMessage}"");
        }

        // Preserve audit fields
        {{entityNameLower}}.UserId = existing.UserId;
        {{entityNameLower}}.CreatedAt = existing.CreatedAt;
        {{entityNameLower}}.UpdatedAt = DateTime.UtcNow;

        _context.Entry(existing).CurrentValues.SetValues({{entityNameLower}});
        await _context.SaveChangesAsync();

        _logger.LogInformation(""Updated {{EntityName}} {Id} for user {UserId}"", {{entityNameLower}}.Id, userId);

        return {{entityNameLower}};
    }

    public async Task<bool> Delete{{EntityName}}Async(Guid id, string userId)
    {
        if (string.IsNullOrEmpty(userId))
            throw new ArgumentException(""User ID cannot be null or empty"", nameof(userId));

        var {{entityNameLower}} = await _context.{{EntityNamePlural}}
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId && !x.IsDeleted);

        if ({{entityNameLower}} == null)
        {
            _logger.LogWarning(""{{EntityName}} {Id} not found for deletion by user {UserId}"", id, userId);
            return false;
        }

        // Soft delete for medical data retention compliance
        {{entityNameLower}}.IsDeleted = true;
        {{entityNameLower}}.DeletedAt = DateTime.UtcNow;
        {{entityNameLower}}.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation(""Soft deleted {{EntityName}} {Id} for user {UserId}"", id, userId);

        return true;
    }

    public async Task<(bool IsValid, List<string> Errors)> ValidateMedical{{EntityName}}Async({{EntityName}} {{entityNameLower}})
    {
        var errors = new List<string>();

        // TODO: Implement medical validation rules specific to {{EntityName}}
        // Examples:
        // - Date range validations
        // - Medical value constraints
        // - Cross-reference validations
        // - Safety checks

        // Basic validation
        if ({{entityNameLower}} == null)
        {
            errors.Add(""{{EntityName}} cannot be null"");
            return (false, errors);
        }

        // Add your medical business rules here
        
        return (errors.Count == 0, errors);
    }
}";

        var implementationContent = implementationTemplate
            .Replace("{{EntityName}}", entityName)
            .Replace("{{EntityNameLower}}", ToLowerFirst(entityName))
            .Replace("{{EntityNamePlural}}", ToPlural(entityName))
            .Replace("{{entityNameLower}}", ToLowerFirst(entityName))
            .Replace("{{entityNameLowerPlural}}", ToLowerFirst(ToPlural(entityName)));

        var implementationFilePath = Path.Combine(_projectRoot, "src", "BloodThinnerTracker.Api", "Services", $"{entityName}Service.cs");
        File.WriteAllText(implementationFilePath, implementationContent);
        
        Console.WriteLine($"✅ Generated service implementation: {implementationFilePath}");
    }

    private string GenerateProperty(string name, string type)
    {
        var template = @"    /// <summary>
    /// Gets or sets the {{PropertyDescription}}.
    /// </summary>
    {{Attributes}}
    public {{PropertyType}} {{PropertyName}} { get; set; }{{DefaultValue}}
";

        var attributes = new List<string>();
        var defaultValue = "";
        var description = name.ToLower();

        // Add appropriate attributes based on type
        switch (type.ToLower())
        {
            case "string":
                attributes.Add("[Required]");
                attributes.Add("[StringLength(255)]");
                defaultValue = " = string.Empty;";
                break;
            case "datetime":
                attributes.Add("[Required]");
                break;
            case "decimal":
                attributes.Add("[Required]");
                attributes.Add("[Range(0.01, 999999.99)]");
                break;
            case "int":
                attributes.Add("[Required]");
                attributes.Add("[Range(1, int.MaxValue)]");
                break;
            case "bool":
                attributes.Add("[Required]");
                break;
        }

        var attributeString = attributes.Count > 0 ? string.Join("\n    ", attributes) : "";

        return template
            .Replace("{{PropertyName}}", name)
            .Replace("{{PropertyType}}", type)
            .Replace("{{PropertyDescription}}", description)
            .Replace("{{Attributes}}", attributeString)
            .Replace("{{DefaultValue}}", defaultValue);
    }

    private string ToLowerFirst(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        return char.ToLower(input[0]) + input.Substring(1);
    }

    private string ToPlural(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        
        // Simple pluralization rules
        if (input.EndsWith("y", StringComparison.OrdinalIgnoreCase))
            return input.Substring(0, input.Length - 1) + "ies";
        if (input.EndsWith("s", StringComparison.OrdinalIgnoreCase) || 
            input.EndsWith("x", StringComparison.OrdinalIgnoreCase) || 
            input.EndsWith("ch", StringComparison.OrdinalIgnoreCase) || 
            input.EndsWith("sh", StringComparison.OrdinalIgnoreCase))
            return input + "es";
        
        return input + "s";
    }
}

// Main execution
if (Args.Length == 0)
{
    Console.WriteLine("Blood Thinner Tracker - Code Generator");
    Console.WriteLine();
    Console.WriteLine("Usage:");
    Console.WriteLine("  dotnet script generate.csx entity <EntityName> [properties]");
    Console.WriteLine("  dotnet script generate.csx controller <EntityName>");
    Console.WriteLine("  dotnet script generate.csx service <EntityName>");
    Console.WriteLine("  dotnet script generate.csx all <EntityName> [properties]");
    Console.WriteLine();
    Console.WriteLine("Properties format: PropertyName:Type,PropertyName:Type");
    Console.WriteLine();
    Console.WriteLine("Examples:");
    Console.WriteLine("  dotnet script generate.csx entity MedicationDose DosageAmount:decimal,TakenAt:DateTime,Notes:string");
    Console.WriteLine("  dotnet script generate.csx controller MedicationDose");
    Console.WriteLine("  dotnet script generate.csx all BloodPressure SystolicPressure:int,DiastolicPressure:int");
    Environment.Exit(0);
}

var generator = new CodeGenerator();
var command = Args[0].ToLower();
var entityName = Args.Length > 1 ? Args[1] : "";

if (string.IsNullOrEmpty(entityName))
{
    Console.WriteLine("❌ Entity name is required");
    Environment.Exit(1);
}

var properties = new Dictionary<string, string>();
if (Args.Length > 2)
{
    var propertiesArg = Args[2];
    foreach (var prop in propertiesArg.Split(','))
    {
        var parts = prop.Split(':');
        if (parts.Length == 2)
        {
            properties[parts[0].Trim()] = parts[1].Trim();
        }
    }
}

switch (command)
{
    case "entity":
        generator.GenerateEntity(entityName, properties);
        break;
    case "controller":
        generator.GenerateController(entityName);
        break;
    case "service":
        generator.GenerateService(entityName);
        break;
    case "all":
        generator.GenerateEntity(entityName, properties);
        generator.GenerateController(entityName);
        generator.GenerateService(entityName);
        Console.WriteLine($"✅ Generated complete CRUD stack for {entityName}");
        break;
    default:
        Console.WriteLine($"❌ Unknown command: {command}");
        Environment.Exit(1);
        break;
}

Console.WriteLine();
Console.WriteLine("🏥 NEXT STEPS:");
Console.WriteLine("1. Review generated code for medical compliance");
Console.WriteLine("2. Add medical validation rules to the service");
Console.WriteLine("3. Update DbContext to include the new entity");
Console.WriteLine("4. Create and run database migrations");
Console.WriteLine("5. Add unit tests for the generated code");
Console.WriteLine("6. Update API documentation");