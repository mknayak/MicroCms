namespace MicroCms.Core.Validation
{
    public class ValidationException : ArgumentException
    {
        public string ErrorCode { get; set; }
        public string FieldName { get; set; }

        public ValidationException(string fieldName, string errorCode) : this(fieldName,errorCode, "Argument Invalid")
        {

        }

        public ValidationException(string fieldName, string errorCode, string message):base(message)
        {
            this.ErrorCode = errorCode;
            this.FieldName = fieldName;
        }
    }
}
