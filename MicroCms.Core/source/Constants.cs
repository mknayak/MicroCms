namespace MicroCms.Core
{
    public static class Constants
    {
        public static class Ids
        {

            internal const string SystemFolderTemplateId = "{94F73E73-39DE-414A-A725-BDA48CDA38BE}";
            public const string FolderTemplateId = "{F9BB3FAD-C3F4-472C-8015-C6B57EDBDFDB}";
            public const string ContenRootId = "{63D2FCF3-635C-47C9-B1DF-56C419F0E7BB}";
            public const string TemplateRootId = "{4E85013D-E297-4FA3-BCF1-BD3D65D6BAAE}";
            public const string CoreTemplateFolderId = "{8F917465-C6AE-4D20-A97D-5AC0ED8D0123}";
            public const string DataRootId = "{A0CD0AC6-ACAE-418C-8E2F-77C2E41939AC}";
        }
        public const string DefaultCreatedBy = "System";

        public static class Error
        {
            public const string NotNullValidtion = "VE100";
            public const string NotNullValidtionMessage = "'{0}' can not be null.";
            public const string TypeValidtion = "VE101";
            public const string TypeValidtionMessage = "'{0}' is not of type '{1}'.";
            public const string RequiredValidtion = "VES101";
            public const string RequiredValidtionMessage = "'{0}' is required.";
            public const string MinLengthValidtion = "VES102";
            public const string MinLengthValidtionMessage = "'{0}' should be of minimum '{1}' length.";
            public const string MaxLengthValidtion = "VES103";
            public const string MaxLengthValidtionMessage = "'{0}' should be of maximum '{1}' length.";
            public const string PatternValidtion = "VES104";
            public const string PatternValidtionMessage = "'{0}' doesnot match pattern '{1}'.";
            public const string StartsWithValidtion = "VES105";
            public const string StartsWithValidtionMessage = "'{0}' doesnot start with pattern '{1}'.";
            public const string EndsWithValidtion = "VES106";
            public const string EndsWithValidtionMessage = "'{0}' doesnot end with pattern '{1}'.";
            public const string MoreThanhValidtion = "VEI101";
            public const string MoreThanValidtionMessage = "'{0}' should be more than '{1}'.";
            public const string LessThanValidtion = "VEI102";
            public const string LessThanValidtionMessage = "'{0}' should be less than '{1}'.";
            public const string RangeValidtion = "VEI103";
            public const string RangeValidtionMessage = "'{0}' should be between '{1}' - '{2}'.";
        }
    }
}
