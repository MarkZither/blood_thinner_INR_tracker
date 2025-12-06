using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Mvc.Versioning
{
    // Minimal route constraint implementation for 'apiVersion' token.
    // This accepts any non-empty version token and delegates real validation
    // to the API versioning middleware. Having this class here avoids a
    // hard dependency on an internal constraint type from the versioning
    // package while allowing '{version:apiVersion}' routes to resolve.
    public class ApiVersionRouteConstraint : IRouteConstraint
    {
        public bool Match(HttpContext? httpContext, IRouter? route, string routeKey, RouteValueDictionary? values, RouteDirection routeDirection)
        {
            if (values == null) return false;
            if (!values.TryGetValue(routeKey, out var v)) return false;
            if (v == null) return false;
            var s = v.ToString();
            return !string.IsNullOrEmpty(s);
        }
    }
}
