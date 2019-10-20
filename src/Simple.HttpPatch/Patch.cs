using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;

namespace Simple.HttpPatch
{
    public sealed class Patch<TModel> : DynamicObject where TModel : class
    {
        private readonly IDictionary<PropertyInfo, object> _changedProperties = new Dictionary<PropertyInfo, object>();
        private readonly string[] _excludedProperties = new[] { "ID" };

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            PropertyInfo propertyInfo = typeof(TModel).GetProperty(binder.Name, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            bool isIgnoredProp = propertyInfo?.GetCustomAttribute<PatchIgnoreAttribute>() != null;
            bool isIgnoredIfNull = propertyInfo?.GetCustomAttribute<PatchIgnoreNullAttribute>() != null;
            bool containsValueOrIsNull = (isIgnoredIfNull && value != null) || !isIgnoredIfNull;

            if (propertyInfo != null &&
                !isIgnoredProp &&
                containsValueOrIsNull)
            {
                _changedProperties.Add(propertyInfo, value);
            }

            return base.TrySetMember(binder, value);
        }

        public void Apply(TModel delta)
        {
            if (delta == null)
            {
                throw new ArgumentNullException(nameof(delta));
            }

            foreach (var property in _changedProperties)
            {
                if (_excludedProperties.Contains(property.Key.Name.ToUpper()))
                {
                    continue;
                }

                object value = ChangeType(property.Value, property.Key.PropertyType);

                property.Key.SetValue(delta, value);
            }
        }

        private object ChangeType(object value, Type type)
        {
            if (type == null)
            {
                return null;
            }
            
            if (type == typeof(Guid))
            {
                return Guid.Parse((string)value);
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                if (value == null)
                {
                    return null;
                }

                type = Nullable.GetUnderlyingType(type);
            }

            return Convert.ChangeType(value, type);
        }
    }
}
