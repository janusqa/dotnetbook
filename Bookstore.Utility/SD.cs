namespace Bookstore.Utility
{
    public static class SD
    {
        // Role Constants
        public const string Role_Customer = "Customer";
        public const string Role_Company = "Company";
        public const string Role_Admin = "Admin";
        public const string Role_Employee = "Employee";

        // Order Status Constants
        public const string OrderStatusPending = "Pending";
        public const string OrderStatusApproved = "Approved";
        public const string OrderStatusInProcess = "Processing";
        public const string OrderStatusShipped = "Shipped";
        public const string OrderStatusCancelled = "Cancelled";

        // Order Payment Status Constants
        public const string PaymentStatusPending = "Pending";
        public const string PaymentStatusApproved = "Approved";
        public const string PaymentStatusApprovedDelayedPayment = "ApprovedForDelayedPayment";
        public const string PaymentStatusRejected = "Rejected";
        public const string PaymentStatusRefunded = "Refunded";
        public const string PaymentStatusCancelled = "Cancelled";

        // sessions management
        public const string SessionCart = "SessionShoppingCart";

    }
}