## Forwarded headers (X-Forwarded-*) and trusted proxies

When your application runs behind a TLS-terminating reverse proxy (e.g. Traefik, nginx, cloud load balancer, Kubernetes Ingress), the proxy terminates the TLS connection and forwards the original request information to the app using X-Forwarded-* headers. The app must trust the proxy and process these headers to correctly determine the original scheme and host for redirects, cookie settings, and URL generation.

Why this matters
- OAuth redirect URIs and cookie Secure/SameSite behavior depend on the external scheme/host. If the app doesn't honor forwarded headers, it may build HTTP URLs instead of HTTPS and OAuth flows will fail.
- Accepting X-Forwarded-* from untrusted clients is a security risk (header spoofing). Only accept forwarded headers from trusted proxies.

How to configure
1. Provide the proxy IPs or networks to the app via configuration. See `src/BloodThinnerTracker.Web/appsettings.Production.json` for an example:

```json
"ForwardedHeaders": {
  "KnownProxies": ["10.0.0.10"],
  "KnownNetworks": ["10.0.0.0/24"]
}
```

2. In production, populate `KnownProxies` with the IP address(es) of your reverse proxy or load balancer. For cloud providers, use the load-balancer or NAT gateway IPs. For Kubernetes, use the cluster service IP range or the specific node/proxy IPs depending on your setup.

3. The app will log a warning at startup if it runs in a non-Development environment and no proxies/networks are configured. This is intentional to avoid silent insecure defaults.

Obtaining proxy IPs
- Traefik (Docker): the host IP where Traefik is running, or the Traefik container IP when using user-defined networks. If Traefik runs as a service (e.g., in Docker Swarm), use the service VIP.
- Traefik (Kubernetes): use the IP(s) of the Kubernetes Service of type LoadBalancer or the external IP of your ingress controller.
- Cloud load balancers: check your cloud provider's console for the public IP(s) or NAT gateway IPs.

Notes and best practices
- Use CIDR (`KnownNetworks`) where possible to simplify configuration when your proxies sit in a known subnet.
- Prefer not to mount host network sockets or rely on host network mode; instead configure trusted proxy addresses.
- For local development the application continues to accept forwarded headers from local proxies for convenience.

If you need help determining the right values for your environment, provide your deployment details (Traefik Docker compose, Kubernetes ingress, or cloud provider), and we can suggest concrete values.
