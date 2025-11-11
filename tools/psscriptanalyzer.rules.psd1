@{
    # PSScriptAnalyzer settings (minimal)
    AnsiColor = $true
    IncludeRules = @(
        'PSAvoidUsingPlainTextForPassword',
        'PSAvoidUsingWriteHost',
        'PSUseApprovedVerbs'
    )
    ExcludeRules = @(
        # add rule names to exclude if necessary
    )
}
