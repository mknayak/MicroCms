namespace MicroCms.Core.Validation
{
    public class Validate
    {
        [Obsolete]
        public static ValidationResult<T> Field<T>(T input)
        {
            return new ValidationResult<T>(input);
        }
        public static ValidationResult<T> Field<T>(T input, string fieldName)
        {
            return new ValidationResult<T>(input) { FieldName = fieldName };
        }
    }
}