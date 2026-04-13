# Security Policy

## Medical Application Notice

This application is used for tracking blood thinner medication and INR test results.
Security vulnerabilities could directly impact patient safety. Please report issues promptly.

## Supported Versions

| Version | Supported |
| ------- | --------- |
| latest  | ✅ Yes    |

## Reporting a Vulnerability

**Please do not report security vulnerabilities through public GitHub issues.**

Use [GitHub Private Security Advisories](https://github.com/MarkZither/blood_thinner_INR_tracker/security/advisories/new)
to report a vulnerability privately.

Please include:

- A description of the vulnerability and its potential impact
- Steps to reproduce or proof-of-concept code
- Affected versions
- Any suggested mitigations

You can expect an acknowledgement within **48 hours** and a resolution timeline within **7 days**
for critical issues affecting patient data or safety.

## Security Considerations

This project handles sensitive medical data. The following areas are considered critical:

- Authentication and authorisation (OAuth 2.0 / JWT)
- Health data encryption at rest and in transit
- INR and medication dosage data integrity
- Cross-device sync security (SignalR)
- API input validation and injection prevention
