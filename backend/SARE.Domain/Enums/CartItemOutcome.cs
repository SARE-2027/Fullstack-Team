namespace SARE.Domain.Enums;

public enum CartItemOutcome
{
    Unknown,
    Accepted,
    // لقراءة نتائج الكشف التاريخية؛ الطلبات الحالية لا تنتج تصحيحًا تلقائيًا.
    Corrected,
    Rejected
}
