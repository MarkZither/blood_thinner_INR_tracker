## EMPTY CATCH POLICY

Never leave an empty `catch { }` block in the codebase.

Why:
- Silent failures hide real problems and make debugging extremely difficult.
- Empty catches swallow exceptions that may indicate integration or environment issues.
- Logging exceptions preserves diagnostic information (stack trace, message, context).

What to do instead:
- If the exception is expected and safely ignorable, add a brief comment explaining why and log at `Debug`/`Trace` level with the exception object.
- If you are intentionally suppressing non-actionable exceptions, prefer filtering with specific exception types and document the reason.
- When in doubt, log the exception and rethrow or return a safe fallback.

Examples:
```csharp
// Good: log and continue (expected transient error)
catch (HttpRequestException ex)
{
    logger.LogWarning(ex, "Failed to call downstream service (transient)");
    // fallback behavior
}

// Better: when completely unexpected, log and rethrow to let global middleware handle it
catch (Exception ex)
{
    logger.LogError(ex, "Unexpected error while registering routes");
    throw;
}
```

Keep this file as the single-source guideline for teammates and tooling.
