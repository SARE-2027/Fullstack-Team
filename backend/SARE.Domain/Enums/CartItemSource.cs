namespace SARE.Domain.Enums;

public enum CartItemSource
{
    Vision,
    Scanner,
    // قرارات الحسم على السيرفر والسجلات القديمة؛ الجهاز يرسل Vision أو Scanner فقط.
    Manual
}
