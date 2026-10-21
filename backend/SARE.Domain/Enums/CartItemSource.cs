namespace SARE.Domain.Enums;

public enum CartItemSource
{
    Vision,
    Scanner,
    // محفوظ لسجلات الفاتورة القديمة؛ التقارير الجديدة تقبل Vision أو Scanner فقط.
    Manual
}
