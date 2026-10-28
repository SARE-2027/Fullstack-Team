namespace SARE.Api.Services;

// مفتاح تسجيل العربات الجديدة في الـRAM فقط، مشترك بين كل اتصالات الـHub.
// مقفول افتراضيًا، والأدمن يفتحه وقت تجهيز العربات. أي restart للسيرفر يرجّعه مقفول.
public sealed class CartRegistrationGate
{
    private volatile bool _isOpen;

    public bool IsOpen => _isOpen;

    public void Set(bool open) => _isOpen = open;
}

