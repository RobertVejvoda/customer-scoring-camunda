namespace ScoringAPI.Camunda;

public static class References {

    public static class Headers
    {
        public const string XZeebeProcessInstanceKey = "X-Zeebe-Process-Instance-Key";
    }
    
    public static class Dapr
    {
        
        public static class Processes
        {
            public const string CustomerScoring = "customer_scoring";
        }

        public static class Bindings
        {
            public const string ZeebeCommand = "zeebe-command";
            public const string Storage = "storage";
            public const string Smtp = "smtp";
        }

        public static class Operations
        {
            public static class Zeebe
            {
                public const string CreateInstance = "create-instance";
                public const string PublishMessage = "publish-message";
            }

            public static class Storage
            {
                public const string Create = "create";
                public const string List = "list";
                public const string Get = "get";
            }
            
            public static class Smtp
            {
                public const string Create = "create";
            }
        }

        public static class MessageNames
        {
            public const string OnUploadDocument = "upload-document";
        }
    }
}