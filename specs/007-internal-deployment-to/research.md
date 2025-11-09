# research.md

## Decisions

- Decision: Use Tailscale (Tailnet + magic DNS) for VPN and secure access.
  - Rationale: KISS for internal deployments; Tailscale provides easy key management, NAT traversal, and magic DNS so operators can rely on single hostname per host.
  - Alternatives considered: WireGuard manual configuration (more setup), traditional VPN appliances (complex for small internal setups).

- Decision: K.I.S.S. operator-driven publish & install using scripts.
  - Rationale: Low operational complexity; leverages existing .NET publish artifacts and basic system service features.

- Decision: Observability baseline is local health endpoint and rotating logs; metrics to be added later.

## Remaining unknowns (deferred to plan/tasks)

- None critical — clarifications collected during /speckit.clarify. Implementation tasks will create scripted checks and guidance.
