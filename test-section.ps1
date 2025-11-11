$apiConfig = @'
{
    "Logging": {
        "LogLevel": {
            "Default": "Information",
            "Microsoft.AspNetCore": "Warning",
            "Microsoft.EntityFrameworkCore": "Warning"
        }
    },
    "AllowedHosts": "*",
    "ConnectionStrings": {
        "DefaultConnection": "Data Source=/var/lib/bloodtracker/bloodtracker.db;Cache=Shared;"
    },
    "Database": {
        "Provider": "SQLite"
    },
    "Urls": "http://0.0.0.0:5234",
    "Security": {
        "RequireHttps": false,
        "EnableCors": true,
        "AllowedOrigins": [
            "http://localhost:5235",
            "http://raspberrypi:5235",
            "http://raspberrypi.local:5235"
        ]
    },
    "MedicalApplication": {
        "Name": "Blood Thinner Medication and INR Tracker",
        "Version": "1.0.0",
        "ComplianceLevel": "InternalUseOnly",
        "EnableAuditLogging": true
    }
}
'@


# Escape single quotes in JSON for shell
