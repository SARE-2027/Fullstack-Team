namespace SARE.Domain.Enums;

// حالة التحقق مستقلة عن فتح الجلسة وإغلاقها؛ Pending لا يعني عدد منتجات معلومًا.
public enum BasketState
{
    Verified,
    Pending,
    NeedsStaff,
    Auditing
}
