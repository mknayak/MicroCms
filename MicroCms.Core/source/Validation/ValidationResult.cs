namespace MicroCms.Core.Validation
{
    public class ValidationResult
    {
        public ValidationResult(object data)
        {
            this.Data = data;
            this.FieldName = "Field";
        }
        public object Data { get; set; }
        public string FieldName { get; set; }
    }
    public class ValidationResult<T> : ValidationResult
    {
        public ValidationResult(T field) : base(field)
        {
            this.Field = field;
        }
        public virtual T Field { get; set; }
    }
}
