using System;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace BloodThinnerTracker.Web.Tests.TestHelpers
{
    public static class BunitRenderHelpers
    {
        /// <summary>
        /// Render a component by its Type using bUnit's generic Render&lt;T&gt;() method via reflection.
        /// We intentionally pass a single null argument to select the overload that accepts
        /// an Action&lt;ComponentParameterCollectionBuilder&lt;T&gt;&gt;? parameter. In C# 13 this can be
        /// expressed concisely as the collection expression [null]. Keeping the reflection
        /// logic here centralizes the detail so tests remain clean.
        /// </summary>
        public static IRenderedComponent<IComponent> RenderComponentByType(this BunitContext ctx, Type componentType)
        {
            var method = typeof(BunitContext)
                .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(m => m.Name == "Render" && m.IsGenericMethod && m.GetGenericArguments().Length == 1);

            if (method == null)
                throw new InvalidOperationException("Render<T>() reflection helper could not find the method.");

            var generic = method.MakeGenericMethod(componentType);

            // Use C# 13 collection expression [null] to pass a single null argument array.
            var rendered = generic.Invoke(ctx, [null]);

            if (rendered == null)
                throw new InvalidOperationException($"Failed to render component of type {componentType.FullName}.");

            return (IRenderedComponent<IComponent>)rendered;
        }
    }
}
