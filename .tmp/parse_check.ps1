# Improved parse checker: ParseFile returns parse errors via out parameter; capture them and report.
$path = 'c:\Source\github\blood_thinner_INR_tracker\tools\deploy-to-pi.ps1'
$tokens = $null
$errors = $null
[void][System.Management.Automation.Language.Parser]::ParseFile($path, [ref]$tokens, [ref]$errors)
if ($errors -and $errors.Count -gt 0) {
    Write-Output 'PARSE_ERROR'
    foreach ($err in $errors) {
        # Each error has Message and Extent for position info
        Write-Output $err.Message
        if ($err.Extent) {
            Write-Output ("At: {0}:{1}" -f $path, $err.Extent.StartLineNumber)
            # show a short snippet if available
            $snippet = $err.Extent.Text -replace "\r",""
            if ($snippet) { Write-Output "  -> $snippet" }
        }
    }
    exit 1
} else {
    Write-Output 'PARSE_OK'
}
