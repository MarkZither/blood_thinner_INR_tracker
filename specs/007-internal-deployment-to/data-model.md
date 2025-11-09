# data-model.md

This feature does not introduce new persistent data models. Deployment artifacts and logs are stored on host filesystem; the spec describes `Artifact` and `DeploymentConfig` entities for operational metadata.

- Artifact: { version, path, timestamp }
- DeploymentConfig: { host, ports, serviceName, rollbackPath }
