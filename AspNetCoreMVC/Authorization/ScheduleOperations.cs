using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace AspNetCoreMVC.Authorization
{
    public static class ScheduleOperations
    {
        public static OperationAuthorizationRequirement Create =   
          new OperationAuthorizationRequirement {Name=Constants.CreateOperationName};
        public static OperationAuthorizationRequirement Read = 
          new OperationAuthorizationRequirement {Name=Constants.ReadOperationName};  
        public static OperationAuthorizationRequirement Update = 
          new OperationAuthorizationRequirement {Name=Constants.UpdateOperationName}; 
        public static OperationAuthorizationRequirement Delete = 
          new OperationAuthorizationRequirement {Name=Constants.DeleteOperationName};
        public static OperationAuthorizationRequirement Approve = 
          new OperationAuthorizationRequirement {Name=Constants.ApproveOperationName};
        public static OperationAuthorizationRequirement Reject = 
          new OperationAuthorizationRequirement {Name=Constants.RejectOperationName};

        // Custom operations for schedule management
        public static OperationAuthorizationRequirement AssignOwner =
            new OperationAuthorizationRequirement {Name=Constants.AssignOwner};
        public static OperationAuthorizationRequirement AssignStatus =
            new OperationAuthorizationRequirement {Name=Constants.AssignStatus};

    }

    public class Constants
    {
        public static readonly string CreateOperationName = "Create";
        public static readonly string ReadOperationName = "Read";
        public static readonly string UpdateOperationName = "Update";
        public static readonly string DeleteOperationName = "Delete";
        public static readonly string ApproveOperationName = "Approve";
        public static readonly string RejectOperationName = "Reject";

        public static readonly string ScheduleAdministratorsRole = "ScheduleAdministrators";
        public static readonly string ScheduleManagersRole = "ScheduleManagers";

        // Custom operations for schedule management
        public static readonly string AssignOwner = "AssignOwner";
        public static readonly string AssignStatus = "AssignStatus";
    }
}