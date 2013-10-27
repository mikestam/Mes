namespace Mes.Framework
{
    using GitHub.Validation;
    using ReactiveUI;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;

    public class ReactiveValidatableObject : ReactiveObject, IDataErrorInfo
    {
        private readonly Dictionary<string, bool> enabledProperties;
        private readonly Dictionary<string, bool> invalidProperties;
        private readonly IServiceProvider serviceProvider;
        private static readonly ConcurrentDictionary<Type, Dictionary<string, ValidatedProperty>> typeValidatorsMap = new ConcurrentDictionary<Type, Dictionary<string, ValidatedProperty>>();
        private readonly Dictionary<string, ValidatedProperty> validatedProperties;
        private bool validationEnabled;

        public ReactiveValidatableObject(IServiceProvider serviceProvider)
        {
            Func<IObservedChange<object, object>, bool> predicate = null;
            Action<IObservedChange<object, object>> onNext = null;
            this.invalidProperties = new Dictionary<string, bool>();
            this.enabledProperties = new Dictionary<string, bool>();
            this.validationEnabled = true;
            this.validatedProperties = typeValidatorsMap.GetOrAdd(base.GetType(), new Func<Type, Dictionary<string, ValidatedProperty>>(ReactiveValidatableObject.GetValidatedProperties));
            this.serviceProvider = serviceProvider;
            if (predicate == null)
            {
                predicate = x => this.validatedProperties.ContainsKey(x.PropertyName);
            }
            if (onNext == null)
            {
                onNext = x => this.enabledProperties[x.PropertyName] = true;
            }
            base.Changed.Where<IObservedChange<object, object>>(predicate).Subscribe<IObservedChange<object, object>>(onNext);
        }

        private IEnumerable<string> EnableValidationForUnvalidatedProperties()
        {
            return (from key in this.validatedProperties.Keys
                where !this.enabledProperties.ContainsKey(key)
                select key).Do<string>(delegate (string propertyName) {
                this.enabledProperties[propertyName] = true;
            });
        }

        private string GetErrorMessage(string propertyName)
        {
            ValidatedProperty property;
            if (this.validatedProperties.TryGetValue(propertyName, out property))
            {
                ValidationResult firstValidationError = property.GetFirstValidationError(this, this.serviceProvider);
                if (firstValidationError != ValidationResult.Success)
                {
                    return firstValidationError.ErrorMessage;
                }
            }
            return null;
        }

        private static Dictionary<string, ValidatedProperty> GetValidatedProperties(Type type)
        {
            return (from property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                let validated = new ValidatedProperty(property)
                where validated.Validators.Any<ValidationAttribute>()
                select validated).ToDictionary<ValidatedProperty, string, ValidatedProperty>(p => p.Property.Name, p => p);
        }

        public void ResetValidation()
        {
            try
            {
                this.validationEnabled = false;
                this.TriggerValidationForAllProperties();
                this.invalidProperties.Clear();
                this.enabledProperties.Clear();
                foreach (ValidatedProperty property in this.validatedProperties.Values)
                {
                    property.Reset();
                }
                this.RaisePropertyChanged<ReactiveValidatableObject, bool>(x => x.IsValid);
            }
            finally
            {
                this.validationEnabled = true;
            }
        }

        public void SetErrorMessage(string propertyName, string errorMessage)
        {
            ValidatedProperty property;
            Ensure.ArgumentNotNullOrEmptyString(propertyName, "propertyName");
            Ensure.ArgumentNotNullOrEmptyString(errorMessage, "errorMessage");
            if (this.validatedProperties.TryGetValue(propertyName, out property))
            {
                property.AddValidator(new SetErrorValidator(errorMessage, property.Property.GetValue(this, null)));
                this.TriggerValidationForProperty(propertyName);
            }
        }

        private void TriggerValidationForAllProperties()
        {
            this.validatedProperties.Keys.ForEach<string>(new Action<string>(this.TriggerValidationForProperty));
        }

        private void TriggerValidationForProperty(string propertyName)
        {
            this.RaisePropertyChanged<ReactiveValidatableObject>(propertyName);
        }

        public bool Validate()
        {
            bool isValid = this.IsValid;
            this.EnableValidationForUnvalidatedProperties().ForEach<string>(new Action<string>(this.TriggerValidationForProperty));
            if (this.IsValid != isValid)
            {
                this.RaisePropertyChanged<ReactiveValidatableObject, bool>(x => x.IsValid);
            }
            return this.IsValid;
        }

        public string Error
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool IsValid
        {
            get
            {
                return !this.invalidProperties.Any<KeyValuePair<string, bool>>();
            }
        }

        public string this[string propertyName]
        {
            get
            {
                if (!this.validationEnabled || !this.enabledProperties.ContainsKey(propertyName))
                {
                    return null;
                }
                string errorMessage = this.GetErrorMessage(propertyName);
                bool flag = errorMessage == null;
                if (flag && this.invalidProperties.ContainsKey(propertyName))
                {
                    this.invalidProperties.Remove(propertyName);
                    this.RaisePropertyChanged<ReactiveValidatableObject, bool>(x => x.IsValid);
                    return errorMessage;
                }
                if (!flag)
                {
                    this.invalidProperties[propertyName] = true;
                    this.RaisePropertyChanged<ReactiveValidatableObject, bool>(x => x.IsValid);
                }
                return errorMessage;
            }
        }

        private sealed class SetErrorValidator : ValidationAttribute
        {
            private readonly string errorMessage;
            private readonly object originalValue;

            public SetErrorValidator(string errorMessage, object originalValue)
            {
                this.errorMessage = errorMessage;
                this.originalValue = originalValue;
            }

            protected override ValidationResult IsValid(object value, ValidationContext validationContext)
            {
                if (this.originalValue.Equals(value))
                {
                    return new ValidationResult(this.errorMessage);
                }
                return ValidationResult.Success;
            }
        }

        private class ValidatedProperty
        {
            private readonly IList<ValidationAttribute> validators;

            public ValidatedProperty(PropertyInfo property)
            {
                this.Property = property;
                this.validators = property.GetCustomAttributes(typeof(ValidationAttribute), true).Cast<ValidationAttribute>().ToList<ValidationAttribute>();
                this.Validators = from v in this.validators
                    where !(v is ValidateIfAttribute)
                    select v;
                this.ConditionalValidation = this.validators.FirstOrDefault<ValidationAttribute>(v => (v is ValidateIfAttribute)) as ValidateIfAttribute;
            }

            public void AddValidator(ValidationAttribute validator)
            {
                this.validators.Add(validator);
            }

            public ValidationResult GetFirstValidationError(object instance, IServiceProvider serviceProvider)
            {
                ValidationContext validationContext = new ValidationContext(instance, serviceProvider, null) {
                    MemberName = this.Property.Name
                };
                if ((this.ConditionalValidation != null) && !this.ConditionalValidation.IsValidationRequired(validationContext))
                {
                    return ValidationResult.Success;
                }
                object value = this.Property.GetValue(instance, null);
                return (from validator in this.Validators
                    let r = validator.GetValidationResult(value, validationContext)
                    where r != null
                    select r).FirstOrDefault<ValidationResult>();
            }

            public void Reset()
            {
                (from x in this.validators
                    where x is ReactiveValidatableObject.SetErrorValidator
                    select x).ToList<ValidationAttribute>().ForEach(x => this.validators.Remove(x));
            }

            private ValidateIfAttribute ConditionalValidation { get; set; }

            public PropertyInfo Property { get; private set; }

            public IEnumerable<ValidationAttribute> Validators { get; private set; }
        }
    }
}

