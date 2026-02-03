namespace CommunityShareStack.Models
{
    public enum ItemType
    {
        Book = 0,
        Other = 1
    }

    public enum ItemCondition
    {
        New = 0,
        LikeNew = 1,
        Good = 2,
        Fair = 3,
        Poor = 4
    }

    public enum LoanRequestStatus
    {
        Requested = 0,
        Approved = 1,
        Rejected = 2,
        Cancelled = 3
    }

    public enum LoanStatus
    {
        CheckedOut = 0,
        Returned = 1,
        Overdue = 2
    }
}
