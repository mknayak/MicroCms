using System.Text.RegularExpressions;

namespace MicroCms.Core.Validation
{
    public static class ValidationExtension
    {
        #region string validation
        public static ValidationResult<string> MinLength(this ValidationResult<string> validationResult, int minLength, string? errorMessage = null)
        {
            if (validationResult.Field.Length < minLength)
            {
                errorMessage = errorMessage ?? string.Format(Constants.Error.MinLengthValidtionMessage, validationResult.FieldName, minLength);
                throw new ValidationException(validationResult.FieldName, Constants.Error.MinLengthValidtion, errorMessage);
            }



            return validationResult;
        }

        public static ValidationResult<string> MaxLength(this ValidationResult<string> validationResult, int maxLength, string? errorMessage = null)
        {
            if (validationResult.Field.Length > maxLength)
            {
                errorMessage = errorMessage ?? string.Format(Constants.Error.MaxLengthValidtionMessage, validationResult.FieldName, maxLength);
                throw new ValidationException(validationResult.FieldName, Constants.Error.MaxLengthValidtion, errorMessage);
            }

            return validationResult;
        }

        public static ValidationResult<string> MatchesPattern(this ValidationResult<string> validationResult, string pattern, string? errorMessage = null)
        {
            if (!Regex.IsMatch(validationResult.Field, pattern))
            {
                errorMessage = errorMessage ?? string.Format(Constants.Error.PatternValidtionMessage, validationResult.FieldName, pattern);
                throw new ValidationException(validationResult.FieldName, Constants.Error.PatternValidtion, errorMessage);
            }

            return validationResult;
        }

        public static ValidationResult<string> StartsWith(this ValidationResult<string> validationResult, string pattern, string? errorMessage = null)
        {
            if (validationResult.Field.StartsWith(pattern))
            {
                errorMessage = errorMessage ?? string.Format(Constants.Error.StartsWithValidtionMessage, validationResult.FieldName, pattern);
                throw new ValidationException(validationResult.FieldName, Constants.Error.StartsWithValidtion, errorMessage);
            }

            return validationResult;
        }

        public static ValidationResult<string> EndsWith(this ValidationResult<string> validationResult, string pattern, string? errorMessage = null)
        {
            if (validationResult.Field.EndsWith(pattern))
            {
                errorMessage = errorMessage ?? string.Format(Constants.Error.EndsWithValidtionMessage, validationResult.FieldName, pattern);
                throw new ValidationException(validationResult.FieldName, Constants.Error.EndsWithValidtion, errorMessage);
            }

            return validationResult;
        }
        public static ValidationResult<string> IsNotNullOrEmpty(this ValidationResult<string> validationResult, string? errorMessage = null)
        {
            if (string.IsNullOrWhiteSpace(validationResult.Field))
            {
                errorMessage = errorMessage ?? string.Format(Constants.Error.NotNullValidtionMessage, validationResult.FieldName);
                throw new ValidationException(validationResult.FieldName, Constants.Error.NotNullValidtion, errorMessage);
            }

            return validationResult;
        }

        #endregion




        public static ValidationResult<int> MoreThan(this ValidationResult<int> validationResult, int value, string? errorMessage = null)
        {
            if (validationResult.Field < value)
            {
                errorMessage = errorMessage ?? string.Format(Constants.Error.MoreThanValidtionMessage, validationResult.FieldName, value);
                throw new ValidationException(validationResult.FieldName, Constants.Error.MoreThanhValidtion, errorMessage);
            }

            return validationResult;
        }

        public static ValidationResult<int> LessThan(this ValidationResult<int> validationResult, int value, string? errorMessage = null)
        {
            if (validationResult.Field > value)
            {
                errorMessage = errorMessage ?? string.Format(Constants.Error.LessThanValidtionMessage, validationResult.FieldName, value);
                throw new ValidationException(validationResult.FieldName, Constants.Error.LessThanValidtion, errorMessage);
            }

            return validationResult;
        }

        public static ValidationResult<int> Range(this ValidationResult<int> validationResult, int from, int to, string? errorMessage = null)
        {
            if (validationResult.Field >= from && validationResult.Field <= to)
                return validationResult;

            errorMessage = errorMessage ?? string.Format(Constants.Error.RangeValidtionMessage, validationResult.FieldName, from, to);
            throw new ValidationException(validationResult.FieldName, Constants.Error.RangeValidtion, errorMessage);

        }

        public static ValidationResult IsNotNull(this ValidationResult validationResult, string? errorMessage = null)
        {
            if (null == validationResult.Data)
            {
                errorMessage = errorMessage ?? string.Format(Constants.Error.NotNullValidtionMessage, validationResult.FieldName);
                throw new ValidationException(validationResult.FieldName, Constants.Error.NotNullValidtion, errorMessage);
            }

            return validationResult;
        }
        public static ValidationResult<T> OfType<T>(this ValidationResult validationResult, string? errorMessage = null)
        {
            var tdata = (T)Convert.ChangeType(validationResult.Data, typeof(T));

            if (tdata != null)
                return new ValidationResult<T>((T)Convert.ChangeType(validationResult.Data, typeof(T))) { FieldName = validationResult.FieldName };


            errorMessage = errorMessage ?? string.Format(Constants.Error.TypeValidtionMessage, validationResult.FieldName, typeof(T).Name);
            throw new ValidationException(validationResult.FieldName, Constants.Error.TypeValidtion, errorMessage);

        }
    }
}
