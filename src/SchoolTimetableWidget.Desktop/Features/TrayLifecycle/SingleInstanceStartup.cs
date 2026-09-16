namespace SchoolTimetableWidget.Desktop.Features.TrayLifecycle;

internal interface IInstanceOwnership
{
    bool IsPrimary { get; }
    void SignalActivation();
}

internal static class SingleInstanceStartup
{
    // The callback includes profile loading, window creation and tray creation. Secondary never enters it.
    public static bool Run(IInstanceOwnership instance, Action initializePrimary)
    {
        if (!instance.IsPrimary) { instance.SignalActivation(); return false; }
        initializePrimary();
        return true;
    }
}
