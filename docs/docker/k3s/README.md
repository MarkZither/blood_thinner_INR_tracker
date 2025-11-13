# k3s / Kubernetes examples — Azure Key Vault integration

This directory contains example manifests and a short walkthrough for securely surfacing Azure Key Vault secrets into Kubernetes (k3s) workloads.

Recommended production pattern (summary):

- Use External Secrets Operator (ESO) with the Azure Key Vault provider (or Secrets Store CSI Driver) to sync Key Vault secrets into Kubernetes Secrets.
- Keep a minimal bootstrap secret that contains Azure service principal credentials outside the cluster (encrypted in Git) using Bitnami SealedSecrets or another GitOps-safe mechanism.
- Grant the service principal only the required Key Vault permissions (get/list for secrets) and avoid wide permissions.
- Use RBAC and namespaces to scope which workloads can access the synced Kubernetes Secrets.

Files in this folder:
- `clustersecretstore-example.yaml` — Cluster-level ExternalSecrets operator store pointing at Azure Key Vault. It references a Kubernetes secret for credentials.
- `externalsecret-example.yaml` — Example ExternalSecret that maps a Key Vault secret into a Kubernetes Secret in your namespace.
- `sealedsecret-bootstrap-example.yaml` — Example Bitnami SealedSecret containing the Azure service principal credentials used by the ClusterSecretStore.

Security notes:

- The Azure service principal credential (client secret) is sensitive. Do NOT commit it in plaintext. Use SealedSecrets or your chosen secret-encryption mechanism for git-stored manifests.
- Prefer managed identities when available. For k3s you may need to use a service principal with limited permissions.
- Rotate credentials regularly and rely on Key Vault's rotation features when possible.

Next steps: follow the commands in the manifest comments to create the service principal, grant Key Vault access, create and seal the bootstrap secret, and apply the manifests.
